using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Authorization;
using KSeFClient;
using KSeFClient.Api.Builders.Auth;
using KSeFClient.Core.Models;

namespace KSeF.Client.Tests.Utils;
internal static class AuthenticationUtils
{
    internal static async Task<AuthOperationStatusResponse> AuthenticateAsync(IKSeFClient ksefClient,
    ISignatureService signatureService,
        string nip,
        ContextIdentifierType contextIdentifierType = ContextIdentifierType.Nip)
    {
        var challengeResponse = await ksefClient
            .GetAuthChallengeAsync();


        var authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(challengeResponse.Challenge)
           .WithContext(contextIdentifierType, nip)
           .WithIdentifierType(SubjectIdentifierTypeEnum.CertificateSubject) // or Fingerprint
           .WithIpAddressPolicy(new IpAddressPolicy { /* ... */ })
           .Build();

        var unsignedXml = AuthTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        var certificate = SelfSignedCertificateForSignatureBuilder
            .Create()
            .WithGivenName("A")
            .WithSurname("R")
            .WithSerialNumber("TINPL-" + nip) // Alternatywnie: TINPL-1234567890 , PNOPL-9
            .WithCommonName("A R")
            .Build();

        var signedXml = await signatureService.SignAsync(unsignedXml, certificate);

        var authOperationInfo = await ksefClient
          .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);


        AuthStatus accessTokenResponse;
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMinutes(2);

        do
        {
            accessTokenResponse = await ksefClient
                .GetAuthStatusAsync(authOperationInfo.ReferenceNumber, authOperationInfo.AuthenticationToken.Token);

            Console.WriteLine(
                $"Polling: StatusCode={accessTokenResponse.Status.Code}, " +
                $"Description='{accessTokenResponse.Status.Description}', " +
                $"Elapsed={DateTime.UtcNow - startTime:mm\\:ss}");

            if (accessTokenResponse.Status.Code != 200)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
        while (accessTokenResponse.Status.Code == 100
               && (DateTime.UtcNow - startTime) < timeout);


        if (accessTokenResponse.Status.Code != 200)
        {
            var msg = $"Authentication failed. Status code: {accessTokenResponse?.Status.Code}, description: {accessTokenResponse?.Status.Description}.";

            throw new Exception(msg);
        }

        return await ksefClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);
    }

    internal static async Task<AuthOperationStatusResponse> AuthenticateAsync(IKSeFClient ksefClient,
    ISignatureService signatureService,
        ContextIdentifierType contextIdentifierType = ContextIdentifierType.Nip)
    {
        var random = MiscellaneousUtils.GetRandomNip();
        var nip = random;

        var challengeResponse = await ksefClient
            .GetAuthChallengeAsync();


        var authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(challengeResponse.Challenge)
           .WithContext(contextIdentifierType, nip)
           .WithIdentifierType(SubjectIdentifierTypeEnum.CertificateSubject) // or Fingerprint
           .WithIpAddressPolicy(new IpAddressPolicy { /* ... */ })
           .Build();

        var unsignedXml = AuthTokenRequestSerializer.SerializeToXmlString(authTokenRequest);

        var certificate = SelfSignedCertificateForSignatureBuilder
            .Create()
            .WithGivenName("A")
            .WithSurname("R")
            .WithSerialNumber("TINPL-" + nip) // Alternatywnie: TINPL-1234567890 , PNOPL-9
            .WithCommonName("A R")
            .Build();

        var signedXml = await signatureService.SignAsync(unsignedXml, certificate);

        var authOperationInfo = await ksefClient
          .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);


        AuthStatus accessTokenResponse;
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMinutes(2);

        do
        {
            accessTokenResponse = await ksefClient
                .GetAuthStatusAsync(authOperationInfo.ReferenceNumber, authOperationInfo.AuthenticationToken.Token);

            Console.WriteLine(
                $"Polling: StatusCode={accessTokenResponse.Status.Code}, " +
                $"Description='{accessTokenResponse.Status.Description}', " +
                $"Elapsed={DateTime.UtcNow - startTime:mm\\:ss}");

            if (accessTokenResponse.Status.Code != 200)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
        while (accessTokenResponse.Status.Code == 100
               && (DateTime.UtcNow - startTime) < timeout);


        if (accessTokenResponse.Status.Code != 200)
        {
            var msg = $"Authentication failed. Status code: {accessTokenResponse?.Status.Code}, description: {accessTokenResponse?.Status.Description}.";

            throw new Exception(msg);
        }

        var authResult = await ksefClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);
        return authResult;
    }
}
