using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.AuthorizationPermission;

public class AuthorizationPermissionsScenarioE2EFixture
{
    public Client.Core.Models.Permissions.AuthorizationEntity.SubjectIdentifier SubjectIdentifier { get; } =
        new Client.Core.Models.Permissions.AuthorizationEntity.SubjectIdentifier
        {
            Type = Client.Core.Models.Permissions.AuthorizationEntity.SubjectIdentifierType.Nip,
            Value = MiscellaneousUtils.GetRandomNip()
        };

    public OperationResponse GrantResponse { get; set; }
    public List<OperationResponse> RevokeResponse { get; set; } = new();
    public PagedRolesResponse<EntityRole> SearchResponse { get; set; }
    public int ExpectedPermissionsAfterRevoke { get; internal set; }
}
