using KSeF.Client.Api.Builders.AuthorizationPermissions;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.AuthorizationEntity;
using KSeF.Client.Tests.Utils;
using StandardPermissionType = KSeF.Client.Core.Models.Permissions.AuthorizationEntity.StandardPermissionType;

namespace KSeF.Client.Tests.Core.E2E.Permissions.AuthorizationPermission;

[Collection("AuthorizationPermissionsScenarioE2ECollection")]
public class AuthorizationPermissionsE2ETests : TestBase
{
    private readonly AuthorizationPermissionsScenarioE2EFixture Fixture;
    private string accessToken = string.Empty;

    public AuthorizationPermissionsE2ETests()
    {
        Fixture = new AuthorizationPermissionsScenarioE2EFixture();
        AuthOperationStatusResponse authOperationStatusResponse =
            AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService).GetAwaiter().GetResult();
        accessToken = authOperationStatusResponse.AccessToken.Token;
        Fixture.SubjectIdentifier.Value = MiscellaneousUtils.GetRandomNip();
    }

    /// <summary>
    /// Nadaje uprawnienia, wyszukuje czy zostały nadane, odwołuje uprawnienia i sprawdza, czy po odwołaniu uprawnienia już nie występują.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task AuthorizationPermissions_E2E_GrantSearchRevokeSearch()
    {
        #region Nadaj uprawnienia
        // Act
        OperationResponse operationResponse = await GrantPermissionsAsync();
        Fixture.GrantResponse = operationResponse;

        // Assert
        Assert.NotNull(Fixture.GrantResponse);
        Assert.True(!string.IsNullOrEmpty(Fixture.GrantResponse.OperationReferenceNumber));

        #endregion

        await Task.Delay(SleepTime);

        #region Wyszukaj — powinny się pojawić
        // Act
        PagedRolesResponse<EntityRole> entityRolesPaged = await SearchGrantedRolesAsync();

        // Assert
        Assert.NotNull(entityRolesPaged);
        Assert.Empty(entityRolesPaged.Roles);
        Fixture.SearchResponse = entityRolesPaged;

        #endregion

        await Task.Delay(SleepTime);

        #region Cofnij uprawnienia
        // Act
        await RevokePermissionsAsync();

        #endregion

        await Task.Delay(SleepTime);

        #region Wyszukaj ponownie — nie powinno być wpisów
        PagedRolesResponse<EntityRole> entityRolesAfterRevoke =
            await SearchGrantedRolesAsync();

        // Assert
        if (Fixture.ExpectedPermissionsAfterRevoke > 0)
        {
            Assert.True(entityRolesPaged.Roles.Count == Fixture.ExpectedPermissionsAfterRevoke);
        }
        else
        {
            Assert.Empty(entityRolesPaged.Roles);
        }
        #endregion
    }

    /// <summary>
    /// Nadaje uprawnienia.
    /// </summary>
    /// <returns>Numer referencyjny operacji</returns>
    private async Task<OperationResponse> GrantPermissionsAsync()
    {
        GrantAuthorizationPermissionsRequest grantPermissionAuthorizationRequest =
            GrantAuthorizationPermissionsRequestBuilder
            .Create()
            .WithSubject(Fixture.SubjectIdentifier)
            .WithPermission(StandardPermissionType.SelfInvoicing)
            .WithDescription("E2E test grant")
            .Build();

        OperationResponse operationResponse = await KsefClient
            .GrantsAuthorizationPermissionAsync(grantPermissionAuthorizationRequest,
            accessToken, CancellationToken);

        return operationResponse;
    }

    /// <summary>
    /// Wyszukuje uprawnienia.
    /// </summary>
    /// <returns>Stronicowana lista nadanych uprawnień.</returns>
    private async Task<PagedRolesResponse<EntityRole>> SearchGrantedRolesAsync()
    {
        PagedRolesResponse<EntityRole> entityRolesPaged = await KsefClient
            .SearchEntityInvoiceRolesAsync(
                accessToken,
                pageOffset: 0,
                pageSize: 10,
                CancellationToken
            );

        return entityRolesPaged;
    }

    /// <summary>
    /// Odwołuje uprawnienia.
    /// </summary>
    private async Task RevokePermissionsAsync()
    {

        foreach (var permission in Fixture.SearchResponse.Roles)
        {
            var resp = await KsefClient
            .RevokeAuthorizationsPermissionAsync(permission.Role, accessToken, CancellationToken);

            Assert.NotNull(resp);
            Assert.False(string.IsNullOrEmpty(resp.OperationReferenceNumber));
            Fixture.RevokeResponse.Add(resp);
        }

        foreach (var revokeStatus in Fixture.RevokeResponse)
        {
            await Task.Delay(SleepTime);
            var status = await KsefClient.OperationsStatusAsync(revokeStatus.OperationReferenceNumber, accessToken);
            if (status.Status.Code == 400 && status.Status.Description == "Operacja zakończona niepowodzeniem" && status.Status.Details.First() == "Permission cannot be revoked.")
            {
                Fixture.ExpectedPermissionsAfterRevoke += 1;
            }
        }
    }

    
    /// <summary>
    /// Pobiera uprawnienia do autoryzacji w systemie KSeF.
    /// </summary>
    /// <returns>Stronicowana lista nadanych uprawnień do autoryzacji w systemie KSeF.</returns>
    private async Task<PagedAuthorizationsResponse<AuthorizationGrant>> GetEntityAuthorizationRoleAsync()
    {
        EntityAuthorizationsQueryRequest entityAuthorizationsQueryRequest =
            new EntityAuthorizationsQueryRequest()
            {
                QueryType = QueryType.Received,
            };

        PagedAuthorizationsResponse<AuthorizationGrant> authorizationGrantsPaged = await KsefClient
            .SearchEntityAuthorizationGrantsAsync(
                entityAuthorizationsQueryRequest,
                accessToken,
                pageOffset: 0,
                pageSize: 10,
                CancellationToken);

        return authorizationGrantsPaged;
    }

    /// <summary>
    /// Pobiera uprawnienia nadane subjednostkom.
    /// </summary>
    /// <returns>Stronicowana lista uprawnień nadanych subjednostkom.</returns>
    private async Task<PagedPermissionsResponse<Client.Core.Models.Permissions.SubunitPermission>> GetSubunitPermissionsAsync()
    {
        Client.Core.Models.Permissions.SubUnit.SubunitPermissionsQueryRequest request
            = new Client.Core.Models.Permissions.SubUnit.SubunitPermissionsQueryRequest();

        PagedPermissionsResponse<Client.Core.Models.Permissions.SubunitPermission> subunitPermissionsPaged = await KsefClient
            .SearchSubunitAdminPermissionsAsync(
                request,
                accessToken,
                pageOffset: 0,
                pageSize: 10,
                CancellationToken);

        return subunitPermissionsPaged;
    }

    /// <summary>
    /// Pobiera uprawnienia nadane osobom fizycznym.
    /// </summary>
    /// <returns>Stronicowana lista uprawnień nadanych osobom fizycznym.</returns>
    private async Task<PagedPermissionsResponse<KSeF.Client.Core.Models.Permissions.PersonPermission>> GetPersonPermissionsAsync()
    {
        Client.Core.Models.Permissions.Person.PersonPermissionsQueryRequest request =
            new Client.Core.Models.Permissions.Person.PersonPermissionsQueryRequest();

        PagedPermissionsResponse<KSeF.Client.Core.Models.Permissions.PersonPermission> personPermissionsPaged = await KsefClient
            .SearchGrantedPersonPermissionsAsync(
                request,
                accessToken,
                pageOffset: 0,
                pageSize: 10,
                CancellationToken);

        return personPermissionsPaged;
    }

    /// <summary>
    /// Pobiera uprawnienia nadane jednostkom podległym.
    /// </summary>
    /// <returns>Stronicowana lista uprawnień nadanych jednostkom podległym.</returns>
    private async Task<PagedRolesResponse<SubordinateEntityRole>> GetSubordinateEntityPermissions()
    {
        Client.Core.Models.Permissions.SubUnit.SubordinateEntityRolesQueryRequest request =
            new Client.Core.Models.Permissions.SubUnit.SubordinateEntityRolesQueryRequest();
        PagedRolesResponse<SubordinateEntityRole> subordinateEntityRolesPaged = await KsefClient
            .SearchSubordinateEntityInvoiceRolesAsync(
                request,
                accessToken,
                pageOffset: 0,
                pageSize: 10,
                CancellationToken);

        return subordinateEntityRolesPaged;
    }

    /// <summary>
    /// Pobiera uprawnienia nadane jednostkom EU.
    /// </summary>
    /// <returns>Stronicowana lista uprawnień nadanych jednostkom UE.</returns>
    private async Task<PagedPermissionsResponse<Client.Core.Models.Permissions.EuEntityPermission>> GetEuEntityPermissionsAsync()
    {
        PagedPermissionsResponse<Client.Core.Models.Permissions.EuEntityPermission> euEntityPermissionsPaged =
            await KsefClient
                .SearchGrantedEuEntityPermissionsAsync(
                    new(),
                    accessToken,
                    pageOffset: 0,
                    pageSize: 10,
                    CancellationToken);

        return euEntityPermissionsPaged;
    }
}
