using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.SubunitPermissions;

public class SubunitPermissionsScenarioE2EFixture
{
    public SubUnitContextIdentifier Unit { get; } = new SubUnitContextIdentifier
    {
        Type = SubUnitContextIdentifierType.Nip,
        Value = MiscellaneousUtils.GetRandomNip()
    };

    public string UnitNipInternal { get; set; }

    public SubUnitContextIdentifier Subunit { get; } = new SubUnitContextIdentifier
    {
        Type = SubUnitContextIdentifierType.Nip,
        Value = MiscellaneousUtils.GetRandomNip()
    };

    public SubUnitSubjectIdentifier SubjectIdentifier { get; } = new SubUnitSubjectIdentifier
    {
        Type = SubUnitSubjectIdentifierType.Nip,
        Value = MiscellaneousUtils.GetRandomNip()
    };

    public OperationResponse GrantResponse { get; set; }
    public List<PermissionsOperationStatusResponse> RevokeStatusResults { get; set; } = new();
    public PagedPermissionsResponse<SubunitPermission> SearchResponse { get; internal set; }
}
