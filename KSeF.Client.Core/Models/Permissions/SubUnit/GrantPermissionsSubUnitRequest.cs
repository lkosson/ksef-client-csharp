namespace KSeF.Client.Core.Models.Permissions.SubUnit
{
    public class GrantPermissionsSubUnitRequest
    {
        public SubUnitSubjectIdentifier SubjectIdentifier { get; set; }
        public SubUnitContextIdentifier ContextIdentifier { get; set; }
        public string Description { get; set; }
        public string SubunitName { get; set; }
    }

    public partial class SubUnitSubjectIdentifier 
    {
        public SubUnitSubjectIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public partial class SubUnitContextIdentifier
    {
        public SubUnitContextIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum SubUnitSubjectIdentifierType
    {
        Nip,
        Pesel,
        Fingerprint
    }

    public enum SubUnitContextIdentifierType
    {
        Nip,
        InternalId,
    }
}
