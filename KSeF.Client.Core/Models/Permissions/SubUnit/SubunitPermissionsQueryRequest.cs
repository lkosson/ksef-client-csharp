namespace KSeF.Client.Core.Models.Permissions.SubUnit
{
    public class SubunitPermissionsQueryRequest
    {
        public SubUnitPermissionsSubunitIdentifier SubunitIdentifier { get; set; }
    }
    public class SubUnitPermissionsSubunitIdentifier
    {
        public SubUnitIQuerydentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum SubUnitIQuerydentifierType
    {
        InternalId,
        Nip
    }
}
