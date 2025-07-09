using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeFClient;
using KSeFClient.Api.Builders.Auth;
using KSeFClient.Api.Services;
using KSeFClient.Core.Interfaces;
using KSeFClient.Core.Models;
using KSeFClient.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Threading;

namespace WebApplication.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{

    private readonly IAuthCoordinator authCoordinator;
    private readonly IKSeFClient ksefClient;


    private readonly string _contextIdentifier;
    private readonly string? xMLDirectory;

    public AuthController(IAuthCoordinator authCoordinator, IConfiguration configuration, IKSeFClient ksefClient)
    {
        this.authCoordinator = authCoordinator;
        this.ksefClient = ksefClient;

        _contextIdentifier = configuration["Tools:contextIdentifier"]!;
        xMLDirectory = configuration["Tools:XMLDirectory"];
    }

    [HttpPost("auth-by-coordinator-with-PZ")]
    public async Task<ActionResult<AuthOperationStatusResponse>> AuthWithPZAsync(string contextIdentifier, CancellationToken cancellationToken)
    {
        // Inicjalizacja przykłdowego identyfikatora - w tym przypadku NIP.

        return await authCoordinator.AuthAsync(
                                                    ContextIdentifierType.Nip,
                                                    !string.IsNullOrWhiteSpace(contextIdentifier) ? contextIdentifier : _contextIdentifier,
                                                    SubjectIdentifierTypeEnum.CertificateSubject,
                                                    xmlSigner: (xml) => { return XadeSDummy.SignWithPZ(xml, xMLDirectory); },
                                                    ipAddressPolicy: null,
                                                    cancellationToken);
    }

    [HttpPost("auth-step-by-step")]
    public async Task<ActionResult<AuthOperationStatusResponse>> AuthStepByStepAsync(string contextIdentifier, CancellationToken cancellationToken)
    {

        return await ksefClient
            .AuthSessionStepByStep(
            SubjectIdentifierTypeEnum.CertificateSubject,
            string.IsNullOrWhiteSpace(contextIdentifier) ? contextIdentifier : _contextIdentifier,
            (xml) => { return XadeSDummy.SignWithPZ(xml, xMLDirectory); },
            ipAddressPolicy: null,
            cancellationToken);
    }

    [HttpGet("refresh-token")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return await ksefClient.RefreshAccessTokenAsync(
            refreshToken,
            cancellationToken);
    }
    [HttpGet("auth-with-ksef-certificate")]
    public async Task<AuthOperationStatusResponse> AuthWithKsefCert(string certInBase64, string contextIdentifier, string privateKey, [FromServices] ISignatureService signatureService, CancellationToken cancellationToken)
    {
        var cert = Convert.FromBase64String(certInBase64);
        var x509 = new X509Certificate2(cert);
        var privateKeyBytes = Convert.FromBase64String(privateKey);

        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

        var challengeResponse = await ksefClient
           .GetAuthChallengeAsync(cancellationToken);

        var challenge = challengeResponse.Challenge;

        var authTokenRequest =
            AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challenge)
            .WithContext(ContextIdentifierType.Nip, contextIdentifier)
            .WithIdentifierType(SubjectIdentifierTypeEnum.CertificateSubject);


        AuthTokenRequest authorizeRequest = authTokenRequest.Build();

        var unsignedXml = AuthTokenRequestSerializer.SerializeToXmlString(authorizeRequest);

        var signedXml = await signatureService.SignAsync(unsignedXml, x509.CopyWithPrivateKey(rsa));

        var authSubmission = await ksefClient
           .SubmitXadesAuthRequestAsync(signedXml, false, cancellationToken);

        AuthStatus authStatus;
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMinutes(2);

        do
        {
            authStatus = await ksefClient.GetAuthStatusAsync(authSubmission.ReferenceNumber, authSubmission.AuthenticationToken.Token, cancellationToken);

            Console.WriteLine(
                $"Polling: StatusCode={authStatus.Status.Code}, " +
                $"Description='{authStatus.Status.Description}', " +
                $"Elapsed={DateTime.UtcNow - startTime:mm\\:ss}");

            if (authStatus.Status.Code != 200 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        while (authStatus.Status.Code != 200
            && !cancellationToken.IsCancellationRequested
            && (DateTime.UtcNow - startTime) < timeout);

        if (authStatus.Status.Code != 200)
        {
            Console.WriteLine("Timeout: Brak tokena po 2 minutach.");
            throw new Exception("Timeout Uwierzytelniania: Brak tokena po 2 minutach.");
        }
        var accessTokenResponse = await ksefClient.GetAccessTokenAsync(authSubmission.AuthenticationToken.Token, cancellationToken);

        // 7) Zwróć token            
        return accessTokenResponse;
    }
}

public static class AuthSessionStepByStepHelper
{
    public static async Task<AuthOperationStatusResponse>
        AuthSessionStepByStep(this IKSeFClient ksefClient, SubjectIdentifierTypeEnum authIdentifierType, string contextIdentifier, Func<string, Task<string>> xmlSigner, IpAddressPolicy? ipAddressPolicy = null, CancellationToken cancellationToken = default)
    {

        // Wykonanie auth challenge.
        var challengeResponse = await ksefClient
            .GetAuthChallengeAsync();

        Console.WriteLine(challengeResponse.Challenge);

        // Wymagany jest podpis cyfrowy w formacie XAdES-BES.
        var authTokenRequest =
            AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challengeResponse.Challenge)
            .WithContext(ContextIdentifierType.Nip, contextIdentifier)
            .WithIdentifierType(authIdentifierType)
            .WithIpAddressPolicy(ipAddressPolicy ?? new IpAddressPolicy { /* ... */ })      // optional
            .Build();

        var unsignedXml = AuthTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        // TODO Trzeba podpisac XML przed wysłaniem
        var signedXml = await xmlSigner.Invoke(unsignedXml);

        // Przesłanie podpisanego XML do systemu KSeF
        var authOperationInfo = await ksefClient.
            SubmitXadesAuthRequestAsync(signedXml, false, cancellationToken);


        // Uzyskanie accessToken w celu uwierzytelniania 
        var accessTokenResult = await ksefClient
            .GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token, cancellationToken);

        return accessTokenResult;
    }
}