using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.EuEntityPermission;

public class EuEntityPermissionScenarioE2EFixture
{
    public string AccessToken { get; set; }
    public SubjectIdentifier EuEntity { get; } = new SubjectIdentifier
    {
        Type = SubjectIdentifierType.Fingerprint,
        Value = MiscellaneousUtils.GetRandomNip()
    };
    public OperationResponse GrantResponse { get; set; }
    public List<OperationResponse> RevokeResponse { get; set; } = new List<OperationResponse>();
    public PagedPermissionsResponse<Client.Core.Models.Permissions.EuEntityPermission> SearchResponse { get; set; }
    public int ExpectedPermissionsAfterRevoke { get; internal set; }
    public string NipVatUe { get; set; }
}
