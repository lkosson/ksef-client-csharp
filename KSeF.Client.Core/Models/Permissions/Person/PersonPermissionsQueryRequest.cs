using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Permissions.Person
{
    public class PersonPermissionsQueryRequest
    {
        public PersonPermissionsAuthorIdentifier AuthorIdentifier { get; set; }
        public PersonPermissionsAuthorizedIdentifier AuthorizedIdentifier { get; set; }
        public PersonalContextIdentifier ContextIdentifier { get; set; }
        public PersonPermissionsTargetIdentifier TargetIdentifier { get; set; }
        public List<PersonPermissionType> PermissionTypes { get; set; }
        public PersonPermissionState PermissionState { get; set; }
        public PersonQueryType QueryType { get; set; }
    }

    public class PersonPermissionsAuthorIdentifier
    {
        public PersonSubjectIdentifierType Type { get; set; }
        public string Value { get; set; }
    }
    public class PersonPermissionsAuthorizedIdentifier
    {
        public PersonAuthorizedIdentifierType Type { get; set; }
        public string Value { get; set; }
    }
    public class PersonPermissionsTargetIdentifier
    {
        public PersonTargetIdentifierType Type { get; set; }
        public string Value { get; set; }
    }

    public enum PersonPermissionType
    {
        CredentialsManage,
        CredentialsRead,
        InvoiceWrite,
        InvoiceRead,
        Introspection,
        SubunitManage,
        EnforcementOperations,
        VatUeManage,
        Owner
    }
    public enum PersonPermissionState
    {
        Active,
        Inactive
    }
    public enum PersonQueryType
    {
        PermissionsInCurrentContext,
        PermissionsGrantedInCurrentContext
    }
}
