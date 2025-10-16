using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Permissions.IndirectEntity
{
    public class GrantPermissionsIndirectEntityRequest
    {
        public IndirectEntitySubjectIdentifier SubjectIdentifier { get; set; }
        public IndirectEntityTargetIdentifier TargetIdentifier { get; set; }
        public ICollection<IndirectEntityStandardPermissionType> Permissions { get; set; }
        public string Description { get; set; }
    }

    public enum IndirectEntityStandardPermissionType
    {
        InvoiceRead,
        InvoiceWrite,
    }

    public partial class IndirectEntitySubjectIdentifier
    {
        public IndirectEntitySubjectIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public partial class IndirectEntityTargetIdentifier
    {
        public IndirectEntityTargetIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum IndirectEntitySubjectIdentifierType
    {
        Nip,
        Pesel,
        Fingerprint
    }

    public enum IndirectEntityTargetIdentifierType
    {
        Nip,
        AllPartners,
    }
}
