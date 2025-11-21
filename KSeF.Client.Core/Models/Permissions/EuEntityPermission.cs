using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using System;

namespace KSeF.Client.Core.Models.Permissions
{
    public class EuEntityPermission
    {
        public string Id { get; set; }
        public AuthorIdentifier AuthorIdentifier { get; set; }
        public string VatUeIdentifier { get; set; }
        public string EuEntityName { get; set; }
        public string AuthorizedFingerprintIdentifier { get; set; }
        public EuEntityPermissionType PermissionScope { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
    }
}
