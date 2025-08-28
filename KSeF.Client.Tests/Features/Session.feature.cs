using KSeF.Client.Api.Services;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features;

[CollectionDefinition("Session.feature")]
[Trait("Category", "Features")]
[Trait("Features", "sessions.feature")]
public class InteractiveSessionTests : TestBase
{
    private const string OwnerContextNip = "6351111111";
    private const string SecondContextNip = "6451111144";


    [Theory]
    [InlineData("FA (2)")]
    [InlineData("FA (3)")]
    [Trait("Scenario", "Pytam o status aktualnej sesji interaktywnej")]
    public async Task GivenActiveInteractiveSession_WhenCheckingStatus_ThenReturnsValidStatus(string systemCode)
    {
        var authResult = await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService, OwnerContextNip);

        var cryptographyService = new CryptographyService(ksefClient);
        var encryptionData = cryptographyService.GetEncryptionData();

        var openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
           .Create()
           .WithFormCode(systemCode: systemCode, schemaVersion: "1-0E", value: "FA")
           .WithEncryption(
               encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
               initializationVector: encryptionData.EncryptionInfo.InitializationVector)
           .Build();

        var openOnlineSessionResponse = await ksefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, authResult.AccessToken.Token);
        Assert.NotNull(openOnlineSessionResponse);
        Assert.NotNull(openOnlineSessionResponse.ReferenceNumber);

        var sessionStatusResponse = await ksefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);
        
        Assert.NotNull(sessionStatusResponse);
        Assert.NotNull(sessionStatusResponse.Status);
        Assert.True(sessionStatusResponse.Status.Code == 100);
    }

    [Theory]
    [InlineData("FA (2)")]
    [InlineData("FA (3)")]
    [Trait("Scenario", "Pytam o status innej sesji interaktywnej z mojego kontekstu")]
    public async Task GivenSessionFromSameContext_WhenCheckingStatus_ThenReturnsValidStatus(string systemCode)
    {
        var authResult = await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService, OwnerContextNip);

        var cryptographyService = new CryptographyService(ksefClient);
        var encryptionData = cryptographyService.GetEncryptionData();

        var openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
               .Create()
               .WithFormCode(systemCode: systemCode, schemaVersion: "1-0E", value: "FA")
               .WithEncryption(
                   encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                   initializationVector: encryptionData.EncryptionInfo.InitializationVector)
               .Build();

        var openOnlineSessionResponse = await ksefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, authResult.AccessToken.Token);
        Assert.NotNull(openOnlineSessionResponse.ReferenceNumber);

        var sessionStatusResponse = await ksefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);
        Assert.NotNull(sessionStatusResponse);
        Assert.True(sessionStatusResponse.Status.Code == 100);

        var openSecondOnlineSessionResponse = await ksefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, authResult.AccessToken.Token);
        Assert.NotNull(openSecondOnlineSessionResponse.ReferenceNumber);
        
        var secondSessionStatusResponse = await ksefClient.GetSessionStatusAsync(openSecondOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);
        
        Assert.NotNull(secondSessionStatusResponse);
        Assert.True(secondSessionStatusResponse.Status.Code == 100);
    }

    [Theory]
    [InlineData("FA (2)")]
    [InlineData("FA (3)")]
    [Trait("Scenario", "Pytam o status innej sesji interaktywnej z innego kontekstu")]
    public async Task GivenSessionFromDifferentContext_WhenCheckingStatus_ThenReturnsAuthorizationError(string systemCode)
    {
        var authResult = await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService, OwnerContextNip);

        var cryptographyService = new CryptographyService(ksefClient);
        var encryptionData = cryptographyService.GetEncryptionData();

        var openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
               .Create()
               .WithFormCode(systemCode: systemCode, schemaVersion: "1-0E", value: "FA")
               .WithEncryption(
                   encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                   initializationVector: encryptionData.EncryptionInfo.InitializationVector)
               .Build();

        var openOnlineSessionResponse = await ksefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, authResult.AccessToken.Token);
        Assert.NotNull(openOnlineSessionResponse);
        Assert.NotNull(openOnlineSessionResponse.ReferenceNumber);

        var sessionStatusResponse = await ksefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);
        Assert.True(sessionStatusResponse.Status.Code == 100);

        authResult = await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService, SecondContextNip); //new context
        var openSecondOnlineSessionResponse = await ksefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, authResult.AccessToken.Token);

        var secondSessionStatusResponse = await ksefClient.GetSessionStatusAsync(openSecondOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);
        Assert.NotNull(secondSessionStatusResponse);
        Assert.NotNull(secondSessionStatusResponse.Status);
        Assert.True(secondSessionStatusResponse.Status.Code == 100);

        var callFromSecondContextResponse = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    ksefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token));

        Assert.NotNull(callFromSecondContextResponse);
    }

    [Theory]
    [InlineData("FA (2)")]
    [InlineData("FA (3)")]
    [Trait("Scenario", "Zamykam sesję interaktywną")]
    public async Task GivenInteractiveSession_WhenClosingSession_ThenSessionIsClosed(string systemCode)
    {
        var authResult = await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService, OwnerContextNip);

        var cryptographyService = new CryptographyService(ksefClient);
        var encryptionData = cryptographyService.GetEncryptionData();

        var openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
               .Create()
               .WithFormCode(systemCode: systemCode, schemaVersion: "1-0E", value: "FA")
               .WithEncryption(
                   encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                   initializationVector: encryptionData.EncryptionInfo.InitializationVector)
               .Build();

        var openOnlineSessionResponse = await ksefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, authResult.AccessToken.Token);
        var sessionStatusResponse = await ksefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);
        Assert.NotNull(sessionStatusResponse);
        Assert.NotNull(sessionStatusResponse.Status);
        Assert.True(sessionStatusResponse.Status.Code == 100);

        await ksefClient.CloseOnlineSessionAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);
        var closedSessionStatusResponse = await ksefClient.GetSessionStatusAsync(openOnlineSessionResponse.ReferenceNumber, authResult.AccessToken.Token);

        Assert.NotNull(closedSessionStatusResponse);
        Assert.NotNull(closedSessionStatusResponse.Status);
        Assert.False(closedSessionStatusResponse.Status.Code == 100);
        Assert.True(closedSessionStatusResponse.Status.Code == 440);
    }
}