using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Permissions.EUEntityRepresentative
{
    public class GrantPermissionsEUEntitRepresentativeRequest
    {
        public EUEntitRepresentativeSubjectIdentifier SubjectIdentifier { get; set; }
        public ICollection<EUEntitRepresentativeStandardPermissionType> Permissions { get; set; }
        public string Description { get; set; }
    }

    public enum EUEntitRepresentativeStandardPermissionType
    {
        InvoiceRead,
        InvoiceWrite,
    }

    public partial class EUEntitRepresentativeSubjectIdentifier
    {
        public EUEntitRepresentativeSubjectIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum EUEntitRepresentativeSubjectIdentifierType
    {
        Fingerprint,
    }
}
