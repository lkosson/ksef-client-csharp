using KSeF.Client.Api.Builders.EUEntityPermissions;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Authorization;

namespace KSeF.Client.Tests.Core.E2E.Permissions.EuEntityPermission;

[Collection("EuEntityPermissionE2EScenarioCollection")]
public class EuEntityPermissionE2ETests : TestBase
{   
    private const string EuEntitySubjectName = "Sample Subject Name";
    private const string EuEntityDescription = "E2E EU Entity Permission Test";
    private const string StatusCode400Description = "Operacja zakończona niepowodzeniem";
    private const string StatusCode400FirstDetails = "Permission cannot be revoked.";
    private readonly EuEntityPermissionsQueryRequest EuEntityPermissionsQueryRequest =
            new EuEntityPermissionsQueryRequest { /* e.g. filtrowanie */ };
    private readonly EuEntityPermissionScenarioE2EFixture TestFixture;
    private string accessToken = string.Empty;

    public EuEntityPermissionE2ETests()
    {
        TestFixture = new EuEntityPermissionScenarioE2EFixture();
        string nip = MiscellaneousUtils.GetRandomNip();
        TestFixture.NipVatUe = MiscellaneousUtils.GetRandomNipVatEU(nip, "CZ");
        AuthOperationStatusResponse authOperationStatusResponse =
            AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, nip).GetAwaiter().GetResult();
        accessToken = authOperationStatusResponse.AccessToken.Token;
        TestFixture.EuEntity.Value = MiscellaneousUtils.GetRandomNipVatEU("CZ");
    }

    /// <summary>
    /// Nadaje uprawnienia dla podmiotu, weryfikuje ich nadanie, następnie odwołuje nadane uprawnienia i ponownie weryfikuje.
    /// </summary>
    [Fact]
    public async Task EuEntityGrantSearchRevokeSearch_E2E_ReturnsExpectedResults()
    {
        #region Nadaj uprawnienia jednostce EU
        // Arrange
        Client.Core.Models.Permissions.EUEntity.ContextIdentifier contextIdentifier = new Client.Core.Models.Permissions.EUEntity.ContextIdentifier
        {
            Type = Client.Core.Models.Permissions.EUEntity.ContextIdentifierType.NipVatUe,
            Value = TestFixture.NipVatUe
        };

        // Act
        OperationResponse operationResponse = await GrantPermissionForEuEntityAsync(contextIdentifier);
        TestFixture.GrantResponse = operationResponse;

        // Assert
        Assert.NotNull(TestFixture.GrantResponse);
        Assert.False(string.IsNullOrEmpty(TestFixture.GrantResponse.OperationReferenceNumber));
        #endregion

        await Task.Delay(SleepTime);

        #region Wyszukaj nadane uprawnienia
        // Act
        PagedPermissionsResponse<Client.Core.Models.Permissions.EuEntityPermission> grantedPermissionsPaged = 
            await SearchPermissionsAsync(EuEntityPermissionsQueryRequest);
        TestFixture.SearchResponse = grantedPermissionsPaged;

        // Assert
        Assert.NotNull(TestFixture.SearchResponse);
        Assert.NotEmpty(TestFixture.SearchResponse.Permissions);
        #endregion

        await Task.Delay(SleepTime);

        #region Odwołaj uprawnienia
        // Act
        await RevokePermissionsAsync();
        #endregion

        await Task.Delay(SleepTime);

        #region Sprawdź czy po odwołaniu uprawnienia już nie występują
        // Act
        PagedPermissionsResponse<Client.Core.Models.Permissions.EuEntityPermission> euEntityPermissionsWhenRevoked = 
            await SearchPermissionsAsync(EuEntityPermissionsQueryRequest);
        TestFixture.SearchResponse = euEntityPermissionsWhenRevoked;

        // Assert
        Assert.NotNull(TestFixture.SearchResponse);
        if (TestFixture.ExpectedPermissionsAfterRevoke > 0)
        {
            Assert.True(TestFixture.SearchResponse.Permissions.Count == TestFixture.ExpectedPermissionsAfterRevoke);
        }
        else
        {
            Assert.Empty(TestFixture.SearchResponse.Permissions);
        }
        #endregion
    }

    /// <summary>
    /// Tworzy żądanie nadania uprawnień jednostce UE oraz wysyła żądanie do KSeF API.
    /// </summary>
    /// <param name="contextIdentifier"></param>
    /// <returns>Numer referencyjny operacji</returns>
    private async Task<OperationResponse> GrantPermissionForEuEntityAsync(Client.Core.Models.Permissions.EUEntity.ContextIdentifier contextIdentifier)
    {
        GrantPermissionsRequest grantPermissionsRequest = GrantEUEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(TestFixture.EuEntity)
            .WithSubjectName(EuEntitySubjectName)
            .WithContext(contextIdentifier)
            .WithDescription(EuEntityDescription)
            .Build();

        OperationResponse operationResponse = await KsefClient
            .GrantsPermissionEUEntityAsync(grantPermissionsRequest, accessToken, CancellationToken);

        return operationResponse;
    }

    /// <summary>
    /// Wyszukuje uprawnienia nadane jednostce EU.
    /// </summary>
    /// <param name="expectAny"></param>
    /// <returns>Stronicowana lista wyszukanych uprawnień</returns>
    private async Task<PagedPermissionsResponse<Client.Core.Models.Permissions.EuEntityPermission>> SearchPermissionsAsync(EuEntityPermissionsQueryRequest euEntityPermissionsQueryRequest)
    {
        PagedPermissionsResponse<Client.Core.Models.Permissions.EuEntityPermission> response = 
            await KsefClient
            .SearchGrantedEuEntityPermissionsAsync(
                euEntityPermissionsQueryRequest, 
                accessToken, 
                pageOffset: 0, 
                pageSize: 10, 
                CancellationToken);

        return response;
    }


    /// <summary>
    /// Wysyła żądanie odwołania uprawnień do KSeF API.
    /// </summary>
    private async Task RevokePermissionsAsync()
    {
        foreach (Client.Core.Models.Permissions.EuEntityPermission permission in TestFixture.SearchResponse.Permissions)
        {
            OperationResponse operationResponse = await KsefClient
                .RevokeCommonPermissionAsync(permission.Id, accessToken, CancellationToken);

            TestFixture.RevokeResponse.Add(operationResponse);

            await Task.Delay(SleepTime);
        }

        foreach (OperationResponse operationResponse in TestFixture.RevokeResponse)
        {
            PermissionsOperationStatusResponse statusResponse = 
                await KsefClient.OperationsStatusAsync(operationResponse.OperationReferenceNumber, accessToken);

            if (statusResponse.Status.Code == 400 
                && statusResponse.Status.Description == StatusCode400Description
                && statusResponse.Status.Details.First() == StatusCode400FirstDetails)
            {
                TestFixture.ExpectedPermissionsAfterRevoke += 1;
            }
        }
    }
}   
