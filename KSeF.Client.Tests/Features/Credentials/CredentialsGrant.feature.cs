using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features
{
    [Collection("CredentialsGrantScenario")]
    [Trait("Category", "Features")]
    [Trait("Features", "credentials_grant.feature")]
    public class CredentialsGrantTests : KsefIntegrationTestBase
    {
        [Theory]
        [InlineData("90091309123", new[] { PersonStandardPermissionType.InvoiceWrite })]
        [InlineData("90091309123", new[] { PersonStandardPermissionType.InvoiceRead, PersonStandardPermissionType.InvoiceWrite })]
        [InlineData("90091309123", new[] { PersonStandardPermissionType.CredentialsManage })]
        [InlineData("90091309123", new[] { PersonStandardPermissionType.CredentialsRead })]
        [InlineData("90091309123", new[] { PersonStandardPermissionType.Introspection })]
        [InlineData("90091309123", new[] { PersonStandardPermissionType.SubunitManage })]

        [InlineData("6651887777", new[] { PersonStandardPermissionType.InvoiceWrite })]
        [InlineData("6651887777", new[] { PersonStandardPermissionType.InvoiceRead })]
        [InlineData("6651887777", new[] { PersonStandardPermissionType.CredentialsManage })]
        [InlineData("6651887777", new[] { PersonStandardPermissionType.CredentialsRead })]
        [InlineData("6651887777", new[] { PersonStandardPermissionType.Introspection })]
        [InlineData("6651887777", new[] { PersonStandardPermissionType.InvoiceWrite, PersonStandardPermissionType.InvoiceRead })]
        [Trait("Scenario", "Nadanie uprawnienia wystawianie faktur")]
        public async Task GivenOwnerIsAuthenticated_WhenGrantInvoiceIssuingPermissionToEntity_ThenPermissionIsConfirmed(string identyficator, PersonStandardPermissionType[] permissions)
        {
            string ownerNIP = MiscellaneousUtils.GetRandomNip();

            await TestGrantPermissions(identyficator, permissions, ownerNIP);
        }

        [Theory]
        [InlineData("90091309123", new[] { PersonStandardPermissionType.InvoiceWrite })]
        [InlineData("90091309123", new[] { PersonStandardPermissionType.InvoiceRead, PersonStandardPermissionType.InvoiceWrite })]
        [InlineData("90091309123", new[] { PersonStandardPermissionType.CredentialsManage })]
        [InlineData("90091309123", new[] { PersonStandardPermissionType.CredentialsRead })]
        [InlineData("90091309123", new[] { PersonStandardPermissionType.Introspection })]
        [InlineData("90091309123", new[] { PersonStandardPermissionType.SubunitManage })]

        [InlineData("6651887777", new[] { PersonStandardPermissionType.InvoiceWrite })]
        [InlineData("6651887777", new[] { PersonStandardPermissionType.InvoiceRead })]
        [InlineData("6651887777", new[] { PersonStandardPermissionType.CredentialsManage })]
        [InlineData("6651887777", new[] { PersonStandardPermissionType.CredentialsRead })]
        [InlineData("6651887777", new[] { PersonStandardPermissionType.Introspection })]
        [InlineData("6651887777", new[] { PersonStandardPermissionType.InvoiceWrite, PersonStandardPermissionType.InvoiceRead })]
        [Trait("Scenario", "Nadanie uprawnień przez osobę z uprawnieniem do zarządzania uprawnieniami")]
        public async Task GivenDelegatedByOwnerIsAuthenticated_WhenGrantInvoiceIssuingPermissionToEntity_ThenPermissionIsConfirmed(string identyficator, PersonStandardPermissionType[] permissions)
        {
            string ownerNIP = MiscellaneousUtils.GetRandomNip();
            string authToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, ownerNIP)).AccessToken.Token;

            string nipWhichWillDelegatePermissions = MiscellaneousUtils.GetRandomNip();

            PersonSubjectIdentifier subjectIdentifier = new PersonSubjectIdentifier { Type = PersonSubjectIdentifierType.Nip, Value = nipWhichWillDelegatePermissions };

            PersonStandardPermissionType[] managePermission = new[] { PersonStandardPermissionType.CredentialsManage };

            OperationResponse operationResponse = await PermissionsUtils.GrantPersonPermissionsAsync(KsefClient, authToken, subjectIdentifier, permissions);

            await Task.Delay(1000);
            //tests
            await TestGrantPermissions(identyficator, permissions, nipWhichWillDelegatePermissions);

            //revoke permissions to delegate
            IReadOnlyList<PersonPermission> grantedPermissions = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, authToken, PersonPermissionState.Active);
            Assert.True(grantedPermissions.Any());

            foreach (PersonPermission item in grantedPermissions)
            {
                OperationResponse revokeSuccessful = await PermissionsUtils.RevokePersonPermissionAsync(KsefClient, authToken, item.Id);
                Assert.NotNull(revokeSuccessful);
                await Task.Delay(3000);
            }

            IReadOnlyList<PersonPermission> activePermissionsAfterRevoke = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, authToken, PersonPermissionState.Active);
            Assert.Empty(activePermissionsAfterRevoke);
        }

        private async Task TestGrantPermissions(string identyficator, PersonStandardPermissionType[] permissions, string nip)
        {
            string authToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, nip)).AccessToken.Token;

            bool isNIP = identyficator.Length == 10;

            PersonSubjectIdentifier subjectIdentifier = new PersonSubjectIdentifier { Type = isNIP ? PersonSubjectIdentifierType.Nip : PersonSubjectIdentifierType.Pesel, Value = identyficator };
            OperationResponse grantPermissionsResponse = await PermissionsUtils.GrantPersonPermissionsAsync(KsefClient,
                    authToken,
                    subjectIdentifier,
                    permissions, "CredentialsGrantTests");

            PermissionsOperationStatusResponse grantPermissionsActionStatus = await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient, grantPermissionsResponse.ReferenceNumber, authToken);

            await Task.Delay(3000);
            IReadOnlyList<PersonPermission> grantedPermissions = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, authToken, PersonPermissionState.Active);
            Assert.True(grantedPermissions.Count == permissions.Length);

            foreach (PersonPermission item in grantedPermissions)
            {
                OperationResponse revokeSuccessful = await PermissionsUtils.RevokePersonPermissionAsync(KsefClient, authToken, item.Id);
                Assert.NotNull(revokeSuccessful);
                await Task.Delay(3000);
            }

            IReadOnlyList<PersonPermission> activePermissionsAfterRevoke = await PermissionsUtils.SearchPersonPermissionsAsync(KsefClient, authToken, PersonPermissionState.Active);
            Assert.Empty(activePermissionsAfterRevoke);
        }
    }
}