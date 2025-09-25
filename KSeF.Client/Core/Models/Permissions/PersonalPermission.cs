using KSeF.Client.Core.Models.Permissions.Person;

namespace KSeF.Client.Core.Models.Permissions
{
    public class PersonalPermission
    {
        public string Id { get; set; }
        public string ContextIdentifier { get; set; }
        public TargetIdentifierType ContextIdentifierType { get; set; }
        public string AuthorizedIdentifier { get; set; }
        public AuthorizedIdentifierType AuthorizedIdentifierType { get; set; }
        public string TargetIdentifier { get; set; }
        public TargetIdentifierType TargetIdentifierType { get; set; }
        public PersonPermissionType PermissionScope { get; set; }
        public string Description { get; set; }
        public PermissionState PermissionState { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public bool CanDelegate { get; set; }
    }
}
