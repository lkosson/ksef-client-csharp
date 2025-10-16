using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Permissions.Person
{
    public class PersonalPermissionsQueryRequest
    {
        public PersonalContextIdentifier ContextIdentifier { get; set; }
        public PersonTargetIdentifier TargetIdentifier { get; set; }
        public List<PersonPermissionType> PermissionTypes { get; set; }
        public PersonPermissionState? PermissionState { get; set; }
    }

    public partial class PersonalContextIdentifier
    {
        public PersonalContextIdentifierType Type { get; set; }
        public string Value { get; set; }

    }

    public enum PersonalContextIdentifierType
    {
        Nip,
        InternalId,
    }
}