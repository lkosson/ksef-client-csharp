namespace KSeF.Client.Tests.Core.E2E.Permissions.EntityPermission;

public partial class EntityPermissionE2ETests
{
    private sealed record RevokeResult(
        IList<Client.Core.Models.Permissions.OperationResponse> RevokeResponses,
        int ExpectedPermissionsAfterRevoke
    );
}