namespace KSeF.Client.Core.Models.Permissions.Authorizations
{
    public class GrantPermissionsAuthorizationRequest
    {
        public AuthorizationSubjectIdentifier SubjectIdentifier { get; set; }
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

    public partial class AuthorizationSubjectIdentifier
    {
        public AuthorizationSubjectIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum AuthorizationSubjectIdentifierType
    {
        Nip,
        PeppolId
    }
}
