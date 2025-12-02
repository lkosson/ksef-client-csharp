using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography.X509Certificates;
namespace KSeF.Client.Tests.Core.E2E.Permissions.EntityPermission;


public class EntityPermissionsE2ETestsScenarios : TestBase
{
    /// <summary>
    /// Potwierdza że grantor widzi uprawnienia które nadał innemu podmiotowi.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GrantPermissions_E2E_ShouldReturnPermissionsGrantedByGrantor()
    {
        // Arrange: NIP-y i Subjecty
        string contextNip = MiscellaneousUtils.GetRandomNip();
        string subjectNip = MiscellaneousUtils.GetRandomNip();

        GrantPermissionsEntitySubjectIdentifier BR_subject =
            new()
            {
                Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
                Value = subjectNip
            };

        // Auth
        AuthenticationOperationStatusResponse authorizationInfo =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, contextNip);

        GrantPermissionsEntityRequest grantsPermissionsRequest =
            GrantEntityPermissionsRequestBuilder
                .Create()
                .WithSubject(BR_subject)
                .WithPermissions(
                    Client.Core.Models.Permissions.Entity.EntityPermission.New(
                        EntityStandardPermissionType.InvoiceRead, true),
                    Client.Core.Models.Permissions.Entity.EntityPermission.New(
                        EntityStandardPermissionType.InvoiceWrite, false)
                )
                .WithDescription("Read and Write permissions")
                .Build();

        OperationResponse grantsPermissionsResponse =
            await KsefClient.GrantsPermissionEntityAsync(
                grantsPermissionsRequest,
                authorizationInfo.AccessToken.Token,
                CancellationToken);

        Assert.NotNull(grantsPermissionsResponse);

        PersonPermissionsQueryRequest queryForAllPermissions =
            new()
            {
                QueryType = PersonQueryType.PermissionsGrantedInCurrentContext
            };

        PagedPermissionsResponse<Client.Core.Models.Permissions.PersonPermission>
            queryForAllPermissionsResponse =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonPermissionsAsync(
                    queryForAllPermissions,
                    authorizationInfo.AccessToken.Token),
                condition: r => r is not null && r.Permissions is not null && r.Permissions.Count == 2,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(queryForAllPermissionsResponse);
        Assert.NotEmpty(queryForAllPermissionsResponse.Permissions);
        Assert.Equal(2, queryForAllPermissionsResponse.Permissions.Count);
    }

    /// <summary>
    /// Potwierdza że podmiot któremu nadano uprawnienia widzi je w swoim kontekście.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GrantPermissions_E2E_ShouldReturnPersonalPermissions()
    {
        // Arrange + Grants
        string contextNip = MiscellaneousUtils.GetRandomNip();
        string subjectNip = MiscellaneousUtils.GetRandomNip();

        GrantPermissionsEntitySubjectIdentifier subject =
            new()
            {
                Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
                Value = subjectNip
            };

        AuthenticationOperationStatusResponse authorizationInfo = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, contextNip);

        GrantPermissionsEntityRequest grantPermissionsEntityRequest = GrantEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(subject)
            .WithPermissions(
                Client.Core.Models.Permissions.Entity.EntityPermission.New(
                    EntityStandardPermissionType.InvoiceRead, true),
                Client.Core.Models.Permissions.Entity.EntityPermission.New(
                    EntityStandardPermissionType.InvoiceWrite, true)
            )
            .WithDescription("Grant read and write permissions")
            .Build();

        OperationResponse grantPermissionsEntityResponse = await KsefClient.GrantsPermissionEntityAsync(
            grantPermissionsEntityRequest, authorizationInfo.AccessToken.Token, CancellationToken);
        Assert.NotNull(grantPermissionsEntityResponse);

        // Auth: Entity we własnym kontekście
        AuthenticationOperationStatusResponse entityAuthorizationInfo = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, subjectNip);

        PersonalPermissionsQueryRequest queryForAllPermissions = new();
        PagedPermissionsResponse<PersonalPermission> queryForAllPermissionsResponse =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(
                    queryForAllPermissions, entityAuthorizationInfo.AccessToken.Token),
                condition: r => r is not null && r.Permissions is not null && r.Permissions.Count == 2,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(queryForAllPermissionsResponse);
        Assert.NotEmpty(queryForAllPermissionsResponse.Permissions);
        Assert.Equal(2, queryForAllPermissionsResponse.Permissions.Count);

        List<PersonalPermission> permissionsGrantedByEntity = [.. queryForAllPermissionsResponse.Permissions.Where(p => p.ContextIdentifier.Value == contextNip)];
        Assert.Equal(2, permissionsGrantedByEntity.Count);
    }

    /// <summary>
    /// Potwierdza że podmiot któremu nadano uprawnienia widzi je w swoim kontekście, 
    /// a nie widzi uprawnień nadanych przez inny podmiot.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GrantPermissions_E2E_ShouldReturnPermissionsSearchedBySubjectInEntityContext()
    {
        // Arrange
        string jdgNip = MiscellaneousUtils.GetRandomNip(); // jdg
        string otherJdgNip = MiscellaneousUtils.GetRandomNip(); // inna jdg
        string brNip = MiscellaneousUtils.GetRandomNip(); // biuro rachunkowe
        string kdpNip = MiscellaneousUtils.GetRandomNip(); // kancelaria doradztwa podatkowego

        GrantPermissionsEntitySubjectIdentifier brSubject =
            new()
            {
                Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
                Value = brNip
            };

        GrantPermissionsEntitySubjectIdentifier kdpSubject =
            new()
            {
                Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
                Value = kdpNip
            };

        Client.Core.Models.Permissions.Entity.EntityPermission[] permissions =
        [
            Client.Core.Models.Permissions.Entity.EntityPermission.New(
                EntityStandardPermissionType.InvoiceRead, true),
            Client.Core.Models.Permissions.Entity.EntityPermission.New(
                EntityStandardPermissionType.InvoiceWrite, false)
        ];

        // Act
        // uwierzytelnienie jdg we własnym kontekście
        AuthenticationOperationStatusResponse authorizationInfo = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, jdgNip);
        // nadanie uprawnień biuru rachunkowemu
        OperationResponse brGrantInJdg = await GrantPermissionsAsync(brSubject, authorizationInfo, permissions);
        Assert.NotNull(brGrantInJdg);
        // nadanie uprawnień kancelarii doradztwa podatkowego
        OperationResponse kdpGrantInJdg = await GrantPermissionsAsync(kdpSubject, authorizationInfo, permissions);
        Assert.NotNull(kdpGrantInJdg);

        // uwierzytelnienie otherJdg we własnym kontekście
        AuthenticationOperationStatusResponse otherJdgAuthorizationInfo = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, otherJdgNip);
        // nadanie uprawnień biuru rachunkowemu
        OperationResponse brGrantInOtherJdg = await GrantPermissionsAsync(brSubject, otherJdgAuthorizationInfo, permissions);
        Assert.NotNull(brGrantInOtherJdg);
        // nadanie uprawnień kancelarii doradztwa podatkowego
        OperationResponse kdpGrantInOtherJdg = await GrantPermissionsAsync(kdpSubject, otherJdgAuthorizationInfo, permissions);
        Assert.NotNull(kdpGrantInOtherJdg);

        // w tym momencie:
        // biuro rachunkowe ma uprawnienia w kontekście jdg i otherJdg (razem 4 uprawnienia)
        // kancelaria doradztwa podatkowego ma uprawnienia w kontekście jdg i otherJdg (razem 4 uprawnienia)

        // Assert
        // uwierzytelnienie: biuro rachunkowe w kontekście jdg
        X509Certificate2 personalCertificate = CertificateUtils.GetPersonalCertificate(
            givenName: "Jan",
            surname: "Kowalski",
            serialNumberPrefix: "TINPL",
            serialNumber: brNip,
            commonName: "Jan Kowalski Certificate");

        AuthenticationOperationStatusResponse entityAuthorizationInfo = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            jdgNip,
            AuthenticationTokenContextIdentifierType.Nip,
            personalCertificate);

        PersonalPermissionsQueryRequest queryForContextPermissions =
            new();

        // uprawnienia biura rachunkowego w kontekście jdg
        PagedPermissionsResponse<PersonalPermission>
            queryForContextPermissionsResponse =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(
                    queryForContextPermissions,
                    entityAuthorizationInfo.AccessToken.Token),
                condition: r => r is not null && r.Permissions is not null && r.Permissions.Count == 2,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        Assert.NotNull(queryForContextPermissionsResponse);
        Assert.Equal(2, queryForContextPermissionsResponse.Permissions.Count); // biuro rachunkowe ma 2 uprawnienia w kontekście jdg
    }

    private async Task<OperationResponse> GrantPermissionsAsync(
            GrantPermissionsEntitySubjectIdentifier subject,
            AuthenticationOperationStatusResponse authorizationInfo,
            Client.Core.Models.Permissions.Entity.EntityPermission[] permissions)
    {
        GrantPermissionsEntityRequest grantEntityPermissionsRequest = GrantEntityPermissionsRequestBuilder
                    .Create()
                    .WithSubject(subject)
                    .WithPermissions(permissions)
                    .WithDescription("Uprawnienia do odczytu i wystawiania faktur")
                    .Build();

        OperationResponse grantEntityPermissionsResponse = await KsefClient.GrantsPermissionEntityAsync(
            grantEntityPermissionsRequest, authorizationInfo.AccessToken.Token, CancellationToken).ConfigureAwait(false);

        return grantEntityPermissionsResponse;
    }
}