using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermission;


public class PersonPermissionE2ETests : TestBase
{
    private const string OperationFailureDescription = "Operacja zakończona niepowodzeniem";
    private const string PermissionRevokeFailureDetail = "Permission cannot be revoked.";
    private const int OperationFailureStatusCode = 400;
    private const string PermissionDescription = "E2E test grant";

    // Zamiast fixture: prywatne readonly pola
    private string accessToken = string.Empty;
    private SubjectIdentifier Person { get; } = new();

    public PersonPermissionE2ETests()
    {
        Client.Core.Models.Authorization.AuthOperationStatusResponse auth = AuthenticationUtils
            .AuthenticateAsync(KsefClient, SignatureService)
            .GetAwaiter().GetResult();

        accessToken = auth.AccessToken.Token;

        // Ustaw dane osoby testowej (PESEL)
        Person.Value = MiscellaneousUtils.GetRandomPesel();
        Person.Type = SubjectIdentifierType.Pesel;
    }

    /// <summary>
    /// Testy E2E nadawania i cofania uprawnień dla osób:
    /// - nadanie uprawnień
    /// - wyszukanie nadanych uprawnień
    /// - cofnięcie uprawnień
    /// - ponowne wyszukanie (weryfikacja, że zostały cofnięte)
    /// </summary>
    [Fact]
    public async Task PersonPermissions_FullFlow_GrantSearchRevokeSearch()
    {
        // 1) Nadaj uprawnienia dla osoby
        OperationResponse grantResponse =
            await GrantPersonPermissionsAsync(Person, PermissionDescription, accessToken);

        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrEmpty(grantResponse.OperationReferenceNumber));

        await Task.Delay(SleepTime);

        // 2) Wyszukaj nadane uprawnienia — powinny być widoczne
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> searchAfterGrant =
            await SearchGrantedPersonPermissionsAsync(accessToken);

        Assert.NotNull(searchAfterGrant);
        Assert.NotEmpty(searchAfterGrant.Permissions);

        // Zawężenie do uprawnień nadanych w tym teście po opisie
        List<KSeF.Client.Core.Models.Permissions.PersonPermission> grantedNow =
            searchAfterGrant.Permissions
                .Where(p => p.Description == PermissionDescription)
                .ToList();

        Assert.NotEmpty(grantedNow);
        Assert.Contains(grantedNow, x => Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceRead);
        Assert.Contains(grantedNow, x => Enum.Parse<StandardPermissionType>(x.PermissionScope) == StandardPermissionType.InvoiceWrite);

        await Task.Delay(SleepTime);

        // 3) Cofnij nadane uprawnienia
        RevokeResult revokeResult = await RevokePermissionsAsync(grantedNow, accessToken);

        Assert.NotNull(revokeResult);
        Assert.NotNull(revokeResult.RevokeResponses);
        Assert.Equal(grantedNow.Count, revokeResult.RevokeResponses.Count);

        await Task.Delay(SleepTime);

        // 4) Wyszukaj ponownie — po cofnięciu nie powinno być wpisów (lub zgodnie z oczekiwaniem po błędach cofania)
        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> searchAfterRevoke =
            await SearchGrantedPersonPermissionsAsync(accessToken);

        Assert.NotNull(searchAfterRevoke);

        List<KSeF.Client.Core.Models.Permissions.PersonPermission> remaining =
            searchAfterRevoke.Permissions
                .Where(p => p.Description == PermissionDescription)
                .ToList();

        if (revokeResult.ExpectedPermissionsAfterRevoke > 0)
        {
            Assert.Equal(revokeResult.ExpectedPermissionsAfterRevoke, remaining.Count);
        }
        else
        {
            Assert.Empty(remaining);
        }
    }

    /// <summary>
    /// Nadaje uprawnienia dla osoby i zwraca odpowiedź operacji.
    /// </summary>
    private async Task<OperationResponse> GrantPersonPermissionsAsync(
        SubjectIdentifier subject,
        string description,
        string accessToken)
    {
        GrantPermissionsPersonRequest request = GrantPersonPermissionsRequestBuilder
            .Create()
            .WithSubject(subject)
            .WithPermissions(
                StandardPermissionType.InvoiceRead,
                StandardPermissionType.InvoiceWrite)
            .WithDescription(description)
            .Build();

        OperationResponse response =
            await KsefClient.GrantsPermissionPersonAsync(request, accessToken, CancellationToken);

        return response;
    }

    /// <summary>
    /// Wyszukuje nadane uprawnienia dla osób i zwraca wynik wyszukiwania.
    /// </summary>
    private async Task<PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission>>
        SearchGrantedPersonPermissionsAsync(string accessToken)
    {
        PersonPermissionsQueryRequest query = new PersonPermissionsQueryRequest
        {
            PermissionTypes = new List<PersonPermissionType>
            {
                PersonPermissionType.InvoiceRead,
                PersonPermissionType.InvoiceWrite
            }
        };

        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission> response =
            await KsefClient.SearchGrantedPersonPermissionsAsync(query, accessToken, pageOffset: 0, pageSize: 10, CancellationToken);

        return response;
    }

    /// <summary>
    /// Cofnięcie wszystkich przekazanych uprawnień; zwraca listę odpowiedzi cofnięcia
    /// oraz wyliczoną liczbę uprawnień, które pozostały po nieudanych próbach cofnięcia.
    /// </summary>
    private async Task<RevokeResult> RevokePermissionsAsync(
        IEnumerable<KSeF.Client.Core.Models.Permissions.PersonPermission> grantedPermissions,
        string accessToken)
    {
        List<OperationResponse> revokeResponses = new();

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

    private sealed record RevokeResult(
        IReadOnlyList<OperationResponse> RevokeResponses,
        int ExpectedPermissionsAfterRevoke);
}