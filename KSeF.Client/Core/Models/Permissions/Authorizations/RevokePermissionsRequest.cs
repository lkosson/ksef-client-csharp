namespace KSeF.Client.Core.Models.Permissions.AuthorizationEntity;

public class RevokePermissionsRequest
{
    public SubjectIdentifier SubjectIdentifier { get; set; }
    public StandardPermissionType Permission { get; set; }
}
