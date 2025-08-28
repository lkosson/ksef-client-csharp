using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;
using KSeFClient;

namespace KSeF.Client.Tests.Features
{
    public partial class RevokeCredentialsTests
    {
        private class RevokeCredentialsHelpers
        {
            public static async Task<IReadOnlyList<PersonPermission>> SearchPersonPermissionsAsync(
            IKSeFClient client, string token, PermissionState state
                )
            => await PermissionsUtils.SearchPersonPermissionsAsync(
                   client,
                   token,
                   QueryTypeEnum.PermissionsGrantedInCurrentContext,
                   state);

            public static async Task<bool> GrantInvoiceWriteToPeselAsync(
                IKSeFClient client, string delegateToken, string pesel)
            {
                var subjectIdentifier = new SubjectIdentifier { Type = SubjectIdentifierType.Pesel, Value = pesel };
                var permissions = new[] { StandardPermissionType.InvoiceWrite };

                var operationResponse = await PermissionsUtils.GrantPersonPermissionsAsync(client, delegateToken, subjectIdentifier, permissions);

                return await ConfirmOperationSuccessAsync(client, operationResponse, delegateToken);
            }

            public static async Task<bool> GrantCredentialsManageToDelegateAsync(
                IKSeFClient client, string ownerToken, string delegateNip)
            {
                var subjectIdentifier = new SubjectIdentifier { Type = SubjectIdentifierType.Nip, Value = delegateNip };
                var permissions = new[] { StandardPermissionType.CredentialsManage };

                var operationResponse = await PermissionsUtils.GrantPersonPermissionsAsync(client, ownerToken, subjectIdentifier, permissions);

                return await ConfirmOperationSuccessAsync(client, operationResponse, ownerToken);
            }

            public static async Task<bool> RevokePersonPermissionAsync(
                IKSeFClient client, string token, string permissionId)
            {
                var operationResponse = await PermissionsUtils.RevokePersonPermissionAsync(client, token, permissionId);

                return await ConfirmOperationSuccessAsync(client, operationResponse, token);
            }

            public static async Task<bool> GrantInvoiceWriteToPeselAsManagerAsync(
                IKSeFClient client, string delegateToken, string nipOwner, string pesel)
            {
                var subjectIdentifier = new Core.Models.Permissions.IndirectEntity.SubjectIdentifier
                {
                    Type = Core.Models.Permissions.IndirectEntity.SubjectIdentifierType.Pesel,
                    Value = pesel
                };

                var targetIdentifier = new Core.Models.Permissions.IndirectEntity.TargetIdentifier
                {
                    Type = Core.Models.Permissions.IndirectEntity.TargetIdentifierType.Nip,
                    Value = nipOwner
                };

                var permissions = new[] { Core.Models.Permissions.IndirectEntity.StandardPermissionType.InvoiceWrite };

                var operationResponse = await PermissionsUtils.GrantIndirectPermissionsAsync(client, delegateToken, subjectIdentifier, targetIdentifier, permissions);

                return await ConfirmOperationSuccessAsync(client, operationResponse, delegateToken);
            }

            private static async Task<bool> ConfirmOperationSuccessAsync(
                IKSeFClient client, OperationResponse operationResponse, string token)
            {
                if (string.IsNullOrWhiteSpace(operationResponse?.OperationReferenceNumber))
                    return false;

                await Task.Delay(1000);

                var status = await PermissionsUtils.GetPermissionsOperationStatusAsync(client, operationResponse.OperationReferenceNumber!, token);
                return status?.Status?.Code == 200;
            }
        }
    }
}
