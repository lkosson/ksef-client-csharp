using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Tests.Utils;
using KSeFClient.Api.Services;
using KSeFClient.Core.Interfaces;
using KSeFClient.Http;

namespace KSeF.Client.Tests;

public class Authorization : TestBase
{
    [Fact]
    public async Task RefreshToken_Receive_ShouldReturnTokenDifferentThanInitialToken()
    {
        // Arrange
        var authInfo = await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService);
        var refreshTokenResult = await ksefClient.RefreshAccessTokenAsync(authInfo.RefreshToken.Token);
        
        // Act
       

        Assert.Multiple(() =>
        {
            Assert.NotNull(refreshTokenResult);
            Assert.NotEqual(authInfo.RefreshToken.Token, refreshTokenResult.AccessToken.Token);
        });
    }

    [Fact]
    /// <summary>
    /// Authenticates using certificate and context NIP, encrypted session, role: owner.
    /// </summary>
    public async Task AuthAsync_FullIntegrationFlow_ReturnsAccessToken()
    {

        var authResult = await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService);

        // Assert
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.AccessToken);

        // (opcjonalnie: Assert na format tokena, Claims, czas ważności itp.)
    }

    [Fact]
    /// <summary>
    /// Authenticates using certificate and context NIP, encrypted session, role: owner.
    /// </summary>
    public async Task AuthAsync_FullIntegrationFlowWithKSeFTokenRSA_ReturnsAccessToken()
    {
        // Arrange
        // authenticate
        var authInfo = await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService);
        //need to first auth as owner to get the KSeF token
        var permissions = new KsefTokenPermissionType[]
        {
            KsefTokenPermissionType.InvoiceWrite,
            KsefTokenPermissionType.InvoiceRead
        };
        
        await Task.Delay(sleepTime);
        var ownerToken = await ksefClient.GenerateKsefTokenAsync(new KsefTokenRequest() { Description = $"Wystawianie i przeglądanie faktur", Permissions = permissions }, authInfo.AccessToken.Token);

        await Task.Delay(sleepTime);
        var tokenStatus = await ksefClient.GetKsefTokenAsync(ownerToken.ReferenceNumber, authInfo.AccessToken.Token);

        await Task.Delay(sleepTime);
        var authCoordinator = new AuthCoordinator(ksefClient) as IAuthCoordinator;
        var cryptographyService = new CryptographyService(ksefClient) as ICryptographyService;
        await Task.Delay(sleepTime);

        var contextType = ContextIdentifierType.Nip;
        var contextValue = tokenStatus.ContextIdentifier.Value;

        IpAddressPolicy? ipPolicy = null;
             

        // Act
        var result = await authCoordinator.AuthKsefTokenAsync(
            contextType,
            contextValue,
            ownerToken.Token,
            cryptographyService,
            EncryptionMethodEnum.Rsa,
            ipPolicy,
            default
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);

        // (opcjonalnie: Assert na format tokena, Claims, czas ważności itp.)
    }


    //[Fact]
    /// <summary>
    /// [ECDSA is not supported yet]
    /// Authenticates using certificate and context NIP, encrypted session, role: owner.
    /// </summary>
    public async Task AuthAsync_FullIntegrationFlowWithKSeFTokenECDsa_ReturnsAccessToken()
    {

        // Arrange
        // authenticate
        var authInfo = await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService);
        //need to first auth as owner to get the KSeF token
        var permissions = new KsefTokenPermissionType[]
        {
            KsefTokenPermissionType.InvoiceWrite,
            KsefTokenPermissionType.InvoiceRead
        };

        await Task.Delay(sleepTime);
        var ownerToken = await ksefClient.GenerateKsefTokenAsync(new KsefTokenRequest() { Description = $"Wystawianie i przeglądanie faktur", Permissions = permissions }, authInfo.AccessToken.Token);

        await Task.Delay(sleepTime);
        var tokenStatus = await ksefClient.GetKsefTokenAsync(ownerToken.ReferenceNumber, authInfo.AccessToken.Token);

        await Task.Delay(sleepTime);
        var authCoordinator = new AuthCoordinator(this.ksefClient) as IAuthCoordinator;
        var restClient = new RestClient(httpClientBase) as IRestClient;
        var cryptographyService = new CryptographyService(ksefClient) as ICryptographyService;
        await Task.Delay(sleepTime);

        var contextType = ContextIdentifierType.Nip;
        var contextValue = tokenStatus.ContextIdentifier.Value;

        IpAddressPolicy? ipPolicy = null;


        // Act
        var result = await authCoordinator.AuthKsefTokenAsync(
            contextType,
            contextValue,
            ownerToken.Token,
            cryptographyService,
            EncryptionMethodEnum.ECDsa,
            ipPolicy,
            default
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);

        // (opcjonalnie: Assert na format tokena, Claims, czas ważności itp.)
    }
}
