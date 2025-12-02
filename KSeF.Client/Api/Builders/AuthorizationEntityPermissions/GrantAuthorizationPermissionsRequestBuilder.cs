using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.AuthorizationEntityPermissions;

public static class GrantAuthorizationPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IPermissionsStep WithSubject(AuthorizationSubjectIdentifier subject);
    }

    public interface IPermissionsStep
    {
        IOptionalStep WithPermission(AuthorizationPermissionType permission);
    }

    public interface IOptionalStep
    {
        IOptionalStep WithDescription(string description);
        IOptionalStep WithSubjectDetails(PermissionsAuthorizationSubjectDetails subjectDetails);
        GrantPermissionsAuthorizationRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IOptionalStep
    {
        private AuthorizationSubjectIdentifier _subject;
        private AuthorizationPermissionType _permission;
        private string _description;
        private PermissionsAuthorizationSubjectDetails _subjectDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IPermissionsStep WithSubject(AuthorizationSubjectIdentifier subject)
        {
            ArgumentNullException.ThrowIfNull(subject);
            TypeValueValidator.Validate(subject);
            _subject = subject;
            return this;
        }

        public IOptionalStep WithPermission(AuthorizationPermissionType permission)
        {
            _permission = permission;
            return this;
        }

        public IOptionalStep WithDescription(string description)
        {
            ArgumentNullException.ThrowIfNull(description);
            if (description.Length < ValidValues.PermissionDescriptionMinLength)
            {
                throw new ArgumentException($"Opis uprawnienia za krótki, minimalna długość: {ValidValues.PermissionDescriptionMinLength} znaków.", nameof(description));
            }
            if (description.Length > ValidValues.PermissionDescriptionMaxLength)
            {
                throw new ArgumentException($"Opis uprawnienia za długi, maksymalna długość: {ValidValues.PermissionDescriptionMaxLength} znaków.", nameof(description));
            }
            _description = description;
            return this;
        }

        public IOptionalStep WithSubjectDetails(PermissionsAuthorizationSubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails ?? throw new ArgumentNullException(nameof(subjectDetails));
            return this;
        }

        public GrantPermissionsAuthorizationRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            }

            return new GrantPermissionsAuthorizationRequest
            {
                SubjectIdentifier = _subject,
                Permission = _permission,
                Description = _description,
                SubjectDetails = _subjectDetails,
            };
        }
    }
}