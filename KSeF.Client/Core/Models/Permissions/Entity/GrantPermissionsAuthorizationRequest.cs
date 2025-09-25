namespace KSeF.Client.Core.Models.Permissions.Entity
{
    public class GrantPermissionsAuthorizationRequest
    {
        public SubjectIdentifier SubjectIdentifier { get; set; }
        public AuthorizationPermissionType Permission { get; set; }
        public string Description { get; set; }
    }

    public enum AuthorizationPermissionType
    {
        SelfInvoicing,
        RRInvoicing,
        TaxRepresentative,
        PefInvoicing
    }
}
