using KSeF.Client.Core.Models.Permissions.Person;
using System;

namespace KSeF.Client.Core.Models.Permissions
{
    public class PersonalPermission
    {
        public string Id { get; set; }
        public PersonalContextIdentifier ContextIdentifier { get; set; }
        public PersonAuthorizedIdentifier AuthorizedIdentifier { get; set; }
        public PersonTargetIdentifier TargetIdentifier { get; set; }
        public PersonPermissionType PermissionScope { get; set; }
        public string Description { get; set; }
        public PersonPermissionState PermissionState { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public bool CanDelegate { get; set; }
    }
}
