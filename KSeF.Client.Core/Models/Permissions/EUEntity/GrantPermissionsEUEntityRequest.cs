namespace KSeF.Client.Core.Models.Permissions.EUEntity
{
    public class GrantPermissionsEUEntityRequest
    {
        public EUEntitySubjectIdentifier SubjectIdentifier { get; set; }
        public EUEntityContextIdentifier ContextIdentifier { get; set; }
        public string Description { get; set; }
        public string EuEntityName { get; set; }
    }

    public partial class EUEntitySubjectIdentifier
    {
        public EUEntitySubjectIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum EUEntitySubjectIdentifierType
    {    
        Fingerprint
    }

    public partial class EUEntityContextIdentifier
    {
        public EUEntityContextIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum EUEntityContextIdentifierType
    {
        NipVatUe
    }

}
