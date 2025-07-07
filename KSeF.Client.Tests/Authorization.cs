using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Authorization;
using KSeFClient.Api.Services;
using KSeFClient.Core.Interfaces;
using KSeFClient.Http;

namespace KSeF.Client.Tests;

public class Authorization : TestBase
{
    [Fact]
    public async Task RefreshTokenTest()
    {
        // Arrange
        var initialToken = RefreshToken;
        
        var refreshTokenResult = await kSeFClient.RefreshAccessTokenAsync(initialToken);
        
        // Act
       

        Assert.Multiple(() =>
        {
            Assert.NotNull(refreshTokenResult);
            Assert.NotEqual(initialToken, refreshTokenResult.AccessToken.Token);
        });
    }

    [Fact]
    /// <summary>
    /// Authenticates using certificate and context NIP, encrypted session, role: owner.
    /// </summary>
    public async Task AuthAsync_FullIntegrationFlow_ReturnsAccessToken()
    {

        var authResult = await AuthenticateAsync();

        // Assert
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.AccessToken);

        // (opcjonalnie: Assert na format tokena, Claims, czas ważności itp.)
    }

    [Fact]
    /// <summary>
    /// Authenticates using certificate and context NIP, encrypted session, role: owner.
    /// </summary>
    public async Task AuthAsync_FullIntegrationFlowWithKSeFToken_ReturnsAccessToken()
    {
      
        var random = randomGenerator.Next(100000000, 999999999);
        var randomString = 7 + random.ToString();

        // Arrange
        //need to first auth as owner to get the KSeF token
        var owner = this.AuthenticateAsync();
        var permissions = new KsefTokenPermissionType[]
        {
            KsefTokenPermissionType.InvoiceWrite,
            KsefTokenPermissionType.InvoiceRead
        };
        
        await Task.Delay(sleepTime);
        var ownerToken = await kSeFClient.GenerateKsefTokenAsync(new KsefTokenRequest() { Description = $"NIP {randomString}", Permissions = permissions },AccessToken);

        await Task.Delay(sleepTime);
        var tokenStatus = await kSeFClient.GetKsefTokenAsync(ownerToken.ReferenceNumber, AccessToken);

        await Task.Delay(sleepTime);
        var authCoordinator = new AuthCoordinator(this.kSeFClient) as IAuthCoordinator;
        var restClient = new RestClient(new HttpClient { BaseAddress = new Uri(env) }) as IRestClient;
            var cryptographyService = new CryptographyService(kSeFClient, restClient) as ICryptographyService;
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
            ipPolicy,
            default
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);

        // (opcjonalnie: Assert na format tokena, Claims, czas ważności itp.)
    }
}
