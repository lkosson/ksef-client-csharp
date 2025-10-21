using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Permissions.Person
{
    public class GrantPermissionsPersonRequest
    {
        public PersonSubjectIdentifier SubjectIdentifier { get; set; }
        public ICollection<PersonStandardPermissionType> Permissions { get; set; }
        public string Description { get; set; }
    }

    public enum PersonStandardPermissionType
    {
        InvoiceRead,
        InvoiceWrite,
        Introspection,
        CredentialsRead,
        CredentialsManage,
        EnforcementOperations,
        SubunitManage
    }

    public partial class PersonSubjectIdentifier
    {
        public PersonSubjectIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public partial class PersonAuthorizedIdentifier
    {
        public PersonAuthorizedIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public partial class PersonTargetIdentifier
    {
        public PersonTargetIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum PersonSubjectIdentifierType
    {
        Nip,
        Pesel,
        Fingerprint,
    }

    public enum PersonAuthorizedIdentifierType
    {
        Nip,
        Pesel,
        Fingerprint
    }

    public enum PersonTargetIdentifierType
    {
        Nip,
        AllPartners
    }
}
