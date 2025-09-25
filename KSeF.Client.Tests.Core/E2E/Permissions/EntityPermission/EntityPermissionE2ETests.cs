using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.EntityPermission;

/// <summary>
/// Testy E2E nadawania i cofania uprawnień dla podmiotów:
/// - nadanie uprawnień
/// - wyszukanie nadanych uprawnień
/// - cofnięcie uprawnień
/// - ponowne wyszukanie (weryfikacja, że zostały cofnięte)
/// </summary>
[Collection("EntityPermissionScenario")]
public partial class EntityPermissionE2ETests : TestBase
{
    private const string OperationFailureDescription = "Operacja zakończona niepowodzeniem";
    private const string PermissionRevokeFailureDetail = "Permission cannot be revoked.";
    private const int OperationFailureStatusCode = 400;
    private const string PermissionDescription = "E2E test grant";

    // Zamiast fixture: prywatne readonly pola
    private string accessToken = string.Empty;
    private SubjectIdentifier Entity { get; } = new();

    public EntityPermissionE2ETests()
    {
        Client.Core.Models.Authorization.AuthOperationStatusResponse authOperationStatusResponse = AuthenticationUtils
            .AuthenticateAsync(KsefClient, SignatureService)
            .GetAwaiter().GetResult();

        accessToken = authOperationStatusResponse.AccessToken.Token;
        Entity.Value = MiscellaneousUtils.GetRandomNip();
        Entity.Type = SubjectIdentifierType.Nip;
    }

    [Fact]
    public async Task EntityPermissions_FullFlow_GrantSearchRevokeSearch()
    {
        // 1) Nadaj uprawnienia dla podmiotu
        OperationResponse grantResponse = await GrantEntityPermissionsAsync(Entity, PermissionDescription, accessToken);
        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrEmpty(grantResponse.OperationReferenceNumber));

        await Task.Delay(SleepTime);

        // 2) Wyszukaj nadane uprawnienia — powinny być widoczne
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> searchAfterGrant = await SearchGrantedPersonPermissionsAsync(accessToken);
        Assert.NotNull(searchAfterGrant);
        Assert.NotEmpty(searchAfterGrant.Permissions);
        Assert.True(searchAfterGrant.Permissions.All(x => x.Description == PermissionDescription));
        Assert.True(searchAfterGrant.Permissions.First(x => x.CanDelegate == true && Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceRead) is not null);
        Assert.True(searchAfterGrant.Permissions.First(x => x.CanDelegate == false && Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceWrite) is not null);

        await Task.Delay(SleepTime);

        // 3) Cofnij nadane uprawnienia
        RevokeResult revokeResult = await RevokePermissionsAsync(searchAfterGrant.Permissions, accessToken);
        Assert.NotNull(revokeResult);
        Assert.NotNull(revokeResult.RevokeResponses);
        Assert.True(revokeResult.RevokeResponses.Count == searchAfterGrant.Permissions.Count);

        await Task.Delay(SleepTime);

        // 4) Wyszukaj ponownie — po cofnięciu nie powinno być wpisów (lub zgodnie z oczekiwaniem po błędach cofania)
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> searchAfterRevoke = await SearchGrantedPersonPermissionsAsync(accessToken);
        Assert.NotNull(searchAfterRevoke);
        if (revokeResult.ExpectedPermissionsAfterRevoke > 0)
        {
            Assert.Equal(revokeResult.ExpectedPermissionsAfterRevoke, searchAfterRevoke.Permissions.Count);
        }
        else
        {
            Assert.Empty(searchAfterRevoke.Permissions);
        }
    }

    /// <summary>
    /// Nadaje uprawnienia dla podmiotu i zwraca numer referencyjny operacji.
    /// </summary>
    private async Task<OperationResponse> GrantEntityPermissionsAsync(
        SubjectIdentifier subject,
        string description,
        string accessToken)
    {
        GrantPermissionsEntityRequest request = GrantEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(subject)
            .WithPermissions(
                Permission.New(StandardPermissionType.InvoiceRead, true),
                Permission.New(StandardPermissionType.InvoiceWrite, false)
            )
            .WithDescription(description)
            .Build();

        OperationResponse response = await KsefClient.GrantsPermissionEntityAsync(request, accessToken, CancellationToken);
        return response;
    }

    /// <summary>
    /// Wyszukuje nadane uprawnienia dla osób i zwraca wynik wyszukiwania.
    /// </summary>
    private async Task<PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission>>
        SearchGrantedPersonPermissionsAsync(string accessToken)
    {
        Client.Core.Models.Permissions.Person.PersonPermissionsQueryRequest query = new Client.Core.Models.Permissions.Person.PersonPermissionsQueryRequest();
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> response = await KsefClient.SearchGrantedPersonPermissionsAsync(query, accessToken);
        return response;
    }

    /// <summary>
    /// Cofnięcie wszystkich przekazanych uprawnień; zwraca listę odpowiedzi cofnięcia
    /// oraz wyliczoną liczbę uprawnień, które pozostały po nieudanych próbach cofnięcia.
    /// </summary>
    private async Task<RevokeResult> RevokePermissionsAsync(
        IEnumerable<Client.Core.Models.Permissions.PersonPermission> grantedPermissions,
        string accessToken)
    {
        List<OperationResponse> revokeResponses = new List<OperationResponse>();

        // Uruchomienie operacji cofania
        foreach (Client.Core.Models.Permissions.PersonPermission permission in grantedPermissions)
        {
            OperationResponse resp = await KsefClient.RevokeCommonPermissionAsync(permission.Id, accessToken, CancellationToken);
            revokeResponses.Add(resp);
        }

        // Sprawdzanie statusów cofnięć i zliczanie wyjątków
        int expectedPermissionsAfterRevoke = 0;

        foreach (OperationResponse revokeStatus in revokeResponses)
        {
            await Task.Delay(SleepTime);

            PermissionsOperationStatusResponse status = await KsefClient.OperationsStatusAsync(revokeStatus.OperationReferenceNumber, accessToken);

            if (status.Status.Code == OperationFailureStatusCode
                && status.Status.Description == OperationFailureDescription
                && status.Status.Details.First() == PermissionRevokeFailureDetail)
            {
                expectedPermissionsAfterRevoke += 1;
            }
        }

        return new RevokeResult(revokeResponses, expectedPermissionsAfterRevoke);
    }
}