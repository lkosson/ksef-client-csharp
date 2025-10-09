using System.Text;
using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.DI;
using Microsoft.Extensions.DependencyInjection;

var sc = new ServiceCollection();
sc.AddKSeFClient(opts => { opts.BaseUrl = KsefEnviromentsUris.TEST; opts.CustomHeaders = []; });

sc.AddCryptographyClient(options => { options.WarmupOnStart = WarmupMode.Blocking; });

using var sp = sc.BuildServiceProvider();
var ksefClient = sp.GetRequiredService<IKSeFClient>();

async Task<(TokenInfo accessToken, TokenInfo refreshToken)> AuthenticateUsingSignature(string nip)
{
	var signatureService = sp.GetRequiredService<ISignatureService>();
	var challenge = await ksefClient.GetAuthChallengeAsync();
	var authTokenRequest = AuthTokenRequestBuilder
		.Create()
		.WithChallenge(challenge.Challenge)
		.WithContext(KSeF.Client.Core.Models.Authorization.ContextIdentifierType.Nip, nip)
		.WithIdentifierType(SubjectIdentifierTypeEnum.CertificateSubject)
		.Build();
	var certificate = SelfSignedCertificateForSignatureBuilder
				.Create()
				.WithGivenName("Jan")
				.WithSurname("Kowalski")
				.WithSerialNumber("TINPL-1111111111")
				.WithCommonName("JK")
				.Build();
	var unsignedXml = AuthTokenRequestSerializer.SerializeToXmlString(authTokenRequest);
	var signedXml = signatureService.Sign(unsignedXml, certificate);
	var authOperationInfo = await ksefClient.SubmitXadesAuthRequestAsync(signedXml, verifyCertificateChain: false);

retry:
	var status = await ksefClient.GetAuthStatusAsync(authOperationInfo.ReferenceNumber, authOperationInfo.AuthenticationToken.Token);
	if (status.Status.Code != 200)
	{
		await Task.Delay(TimeSpan.FromSeconds(1));
		goto retry;
	}

	var tokens = await ksefClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);

	return (tokens.AccessToken, tokens.RefreshToken);
}

async Task<(TokenInfo accessToken, TokenInfo refreshToken)> AuthenticateUsingToken(string nip, string ksefToken)
{
	var cryptographyService = sp.GetRequiredService<ICryptographyService>();
	await cryptographyService.WarmupAsync();
	var challenge = await ksefClient.GetAuthChallengeAsync();
	var timestamp = challenge.Timestamp.ToUnixTimeMilliseconds();
	var plaintextRequest = ksefToken + "|" + timestamp;
	var plaintextRequestBytes = Encoding.UTF8.GetBytes(plaintextRequest);
	var encryptedRequestBytes = cryptographyService.EncryptKsefTokenWithRSAUsingPublicKey(plaintextRequestBytes);
	var encryptedRequest = Convert.ToBase64String(encryptedRequestBytes);
	var request = new AuthKsefTokenRequest
	{
		Challenge = challenge.Challenge,
		ContextIdentifier = new AuthContextIdentifier
		{
			Type = KSeF.Client.Core.Models.Authorization.ContextIdentifierType.Nip,
			Value = nip
		},
		EncryptedToken = encryptedRequest,
		AuthorizationPolicy = null
	};

	var signature = await ksefClient.SubmitKsefTokenAuthRequestAsync(request, CancellationToken.None);

retry:
	var status = await ksefClient.GetAuthStatusAsync(signature.ReferenceNumber, signature.AuthenticationToken.Token);
	if (status.Status.Code == 100)
	{
		await Task.Delay(TimeSpan.FromSeconds(1));
		goto retry;
	}
	var tokens = await ksefClient.GetAccessTokenAsync(signature.AuthenticationToken.Token);
	return (tokens.AccessToken, tokens.RefreshToken);
}

async Task<string> GenerateToken(string accessToken)
{
	var request = new KsefTokenRequest { Permissions = [ KsefTokenPermissionType.InvoiceRead, KsefTokenPermissionType.InvoiceWrite ], Description = "ProFak" };
	var token = await ksefClient.GenerateKsefTokenAsync(request, accessToken);

wait:
	var status = await ksefClient.GetKsefTokenAsync(token.ReferenceNumber, accessToken);
	if (status.Status != AuthenticationKsefTokenStatus.Active)
	{
		await Task.Delay(TimeSpan.FromSeconds(1));
		goto wait;
	}

	return token.Token;
}

async Task GetInvoices(string accessToken)
{
	var query = new InvoiceQueryFilters
	{
		DateRange = new DateRange
		{
			From = DateTime.Now.Date.AddDays(-30),
			To = DateTime.Now.Date.AddDays(1),
			DateType = DateType.PermanentStorage
		},
		SubjectType = SubjectType.Subject1
	};

	var invoicesMetadata = await ksefClient.QueryInvoiceMetadataAsync(query, accessToken);
	var nr = invoicesMetadata.Invoices.First().KsefNumber;

	var invoice = await ksefClient.GetInvoiceAsync(nr, accessToken);
}

/*
var tokenInfo = await AuthenticateUsingSignature("1111111111");
var accessToken = tokenInfo.accessToken.Token;
Console.WriteLine($"Access token: {accessToken}");

var ksefToken = await GenerateToken(accessToken);
Console.WriteLine($"KSeF token: {ksefToken}");
*/
/*
var tokenInfo = await AuthenticateUsingToken("1111111111", ksefToken);
var accessToken = tokenInfo.accessToken.Token;
Console.WriteLine($"Access token: {accessToken}");
*/

await GetInvoices(accessToken);

Console.WriteLine("Ok");