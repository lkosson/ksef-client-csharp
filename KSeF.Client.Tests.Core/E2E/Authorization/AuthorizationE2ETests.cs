using KSeF.Client.Core.Models.Token;

namespace KSeF.Client.Tests.Core.E2E.Authorization;

public class AuthorizationE2ETests : TestBase
{
    private const string OwnerRole = "owner";

    /// <summary>
    /// Uwierzytelnia klienta KSeF i sprawdza, czy zwrócony token dostępu jest poprawny
    /// </summary>
    [Fact]
    public async Task AuthAsync_FullIntegrationFlow_ReturnsAccessToken()
    {
        // Arrange & Act
        Client.Core.Models.Authorization.AuthOperationStatusResponse authResult = 
            await Utils.AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService);

        // Assert
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.AccessToken);

        PersonToken personToken = TokenService.MapFromJwt(authResult.AccessToken!.Token!);
        Assert.NotNull(personToken);
        Assert.Contains(OwnerRole, personToken.Roles, StringComparer.OrdinalIgnoreCase);
    }
}

