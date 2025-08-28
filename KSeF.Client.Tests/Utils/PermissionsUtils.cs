using KSeF.Client.Api.Builders.IndirectEntityPermissions;
using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeFClient;

namespace KSeF.Client.Tests.Utils
{
    internal static class PermissionsUtils
    {
        internal static async Task<IReadOnlyList<PersonPermission>> SearchPersonPermissionsAsync(
            IKSeFClient ksefClient,
            string accessToken,
            QueryTypeEnum queryType,
            PermissionState state,
            int pageOffset = 0, int pageSize = 10)
        {
            var query = new PersonPermissionsQueryRequest
            {
                QueryType = queryType,
                PermissionState = state
            };

            var searchResult = await ksefClient.SearchGrantedPersonPermissionsAsync(query, accessToken, pageOffset: pageOffset, pageSize: pageSize);
            return searchResult?.Permissions ?? [];
        }

        internal static async Task<PermissionsOperationStatusResponse> GetPermissionsOperationStatusAsync(
            IKSeFClient ksefClient, string operationReferenceNumber, string accessToken)
            => await ksefClient.OperationsStatusAsync(operationReferenceNumber, accessToken);

        internal static async Task<OperationResponse> RevokePersonPermissionAsync(
            IKSeFClient ksefClient, string accessToken, string permissionId)
            => await ksefClient.RevokeCommonPermissionAsync(permissionId, accessToken);

        internal static async Task<OperationResponse> GrantPersonPermissionsAsync(
            IKSeFClient client,
            string accessToken,
            SubjectIdentifier subject,
            StandardPermissionType[] permissions,
            string? description = null)
        {
            var request = GrantPersonPermissionsRequestBuilder
                .Create()
                .WithSubject(subject)
                .WithPermissions(permissions)
                .WithDescription(description ?? $"Grant {string.Join(", ", permissions)} to {subject.Type}:{subject.Value}")
                .Build();

            return await client.GrantsPermissionPersonAsync(request, accessToken);
        }

        internal static async Task<OperationResponse> GrantIndirectPermissionsAsync(
            IKSeFClient client,
            string accessToken,
            Core.Models.Permissions.IndirectEntity.SubjectIdentifier subject,
            Core.Models.Permissions.IndirectEntity.TargetIdentifier context,
            Core.Models.Permissions.IndirectEntity.StandardPermissionType[] permissions,
            string? description = null)
        {
            var request = GrantIndirectEntityPermissionsRequestBuilder
                .Create()
                .WithSubject(subject)
                .WithContext(context)
                .WithPermissions(permissions)
                .WithDescription(description ?? $"Grant {string.Join(", ", permissions)} to {subject.Type}:{subject.Value} @ {context.Value}")
                .Build();

            return await client.GrantsPermissionIndirectEntityAsync(request, accessToken);
        }

        //
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

        public static async Task<bool> ConfirmOperationSuccessAsync(
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
