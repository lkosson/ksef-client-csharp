using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features
{
    [CollectionDefinition("RevokeCredentials.feature")]
    [Trait("Category", "Features")]
    [Trait("Features", "revoke_credentials.feature")]
    public partial class RevokeCredentialsTests : TestBase
    {
        [Fact]
        [Trait("Scenario", "Nadaje uprawnienie 'InvoiceWrite' dla PESEL, potwierdza nadanie, następnie odbiera i potwierdza brak.")]
        public async Task Owner_GrantAndRevoke_InvoiceWrite_ForPesel_LeavesNoActivePermission()
        {
            // ========== Arrange ==========
            var nip = MiscellaneousUtils.GetRandomNip();
            var pesel = MiscellaneousUtils.GetRandomPesel();
            var token = (await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService, nip)).AccessToken.Token;

            // ============ Act ============

            // ========== Act: GRANT ==========
            var grantSuccessful = await RevokeCredentialsHelpers.GrantInvoiceWriteToPeselAsync(ksefClient, token, pesel);
            Assert.True(grantSuccessful);

            // ========== Act: SEARCH ==========
            var grantedPermissions = await RevokeCredentialsHelpers.SearchPersonPermissionsAsync(ksefClient, token, PermissionState.Active);
            var grantedPermission = Assert.Single(grantedPermissions);

            // ========== Act: REVOKE ==========
            var revokeSuccessful = await RevokeCredentialsHelpers.RevokePersonPermissionAsync(ksefClient, token, grantedPermission.Id);
            Assert.True(revokeSuccessful);

            // ============ Assert ============
            var activePermissionsAfterRevoke = await RevokeCredentialsHelpers.SearchPersonPermissionsAsync(ksefClient, token, PermissionState.Active);
            Assert.Empty(activePermissionsAfterRevoke);
        }

        [Fact]
        [Trait("Scenario", "Właściciel nadaje CredentialsManage dla delegata, delegat nadaje 'InvoiceWrite' dla PESEL i następnie odbiera.")]
        public async Task Delegate_GrantAndRevoke_InvoiceWrite_ForPesel_AsManager_LeavesNoActivePermission()
        {
            // ===== Arrange =====
            var nipOwner = MiscellaneousUtils.GetRandomNip();
            var nipDelegate = MiscellaneousUtils.GetRandomNip();
            var pesel = MiscellaneousUtils.GetRandomPesel();

            var ownerToken = (await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService, nipOwner)).AccessToken.Token;
            var delegateToken = (await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService, nipDelegate)).AccessToken.Token;

            // ============ Act ============

            // ========== Act: GRANT AS OWNER CredentialManage FOR DELEGATE ==========
            var manageGranted = await RevokeCredentialsHelpers.GrantCredentialsManageToDelegateAsync(ksefClient, ownerToken, nipDelegate);
            Assert.True(manageGranted);

            // ========== Act: SEARCH CredentialManage FOR DELEGATE ==========
            var delegatePermissions = await RevokeCredentialsHelpers.SearchPersonPermissionsAsync(ksefClient, ownerToken, PermissionState.Active);
            var delegatePermission = Assert.Single(delegatePermissions);

            // ========== Act: GRANT AS DELEGATE InvoiceWrite FOR PESEL ==========
            var invoiceWriteGranted = await RevokeCredentialsHelpers.GrantInvoiceWriteToPeselAsManagerAsync(ksefClient, delegateToken, nipOwner, pesel);
            Assert.True(invoiceWriteGranted);

            var peselPermissionsAfterGrant = await RevokeCredentialsHelpers.SearchPersonPermissionsAsync(ksefClient, delegateToken, PermissionState.Inactive);
            var grantedPermission = Assert.Single(peselPermissionsAfterGrant);

            // ========== Act: REVOKE AS DELEGATE InvoiceWrite FOR PESEL ==========
            var revokeSuccessful = await RevokeCredentialsHelpers.RevokePersonPermissionAsync(ksefClient, delegateToken, grantedPermission.Id);
            Assert.True(revokeSuccessful);

            // ===== Assert =====
            var activePermissionsAfterRevoke = await RevokeCredentialsHelpers.SearchPersonPermissionsAsync(ksefClient, delegateToken, PermissionState.Active);
            Assert.Empty(activePermissionsAfterRevoke);
        }
    }
}
