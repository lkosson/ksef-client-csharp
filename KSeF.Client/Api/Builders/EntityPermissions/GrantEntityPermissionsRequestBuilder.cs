using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.EntityPermissions;

public static class GrantEntityPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IPermissionsStep WithSubject(GrantPermissionsEntitySubjectIdentifier subject);
    }

    public interface IPermissionsStep
    {
        IOptionalStep WithPermissions(params EntityPermission[] permissions);
    }

    public interface IOptionalStep
    {
        IOptionalStep WithDescription(string description);
        IOptionalStep WithSubjectDetails(PermissionsEntitySubjectDetails subjectDetails);
        GrantPermissionsEntityRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IOptionalStep
    {
        private GrantPermissionsEntitySubjectIdentifier _subject;
        private ICollection<EntityPermission> _permissions;
        private string _description;
        private PermissionsEntitySubjectDetails _subjectDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IPermissionsStep WithSubject(GrantPermissionsEntitySubjectIdentifier subject)
        {
            ArgumentNullException.ThrowIfNull(subject);
            if (!TypeValueValidator.Validate(subject))
            {
                throw new ArgumentException($"Nieprawidłowa wartość dla typu {subject.Type}", nameof(subject));
            }
            _subject = subject;
            return this;
        }

        public IOptionalStep WithPermissions(params EntityPermission[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
            {
                throw new ArgumentException("Wymagane jest co najmniej jedno uprawnienie.", nameof(permissions));
            }

            _permissions = permissions;
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

        public IOptionalStep WithSubjectDetails(PermissionsEntitySubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails ?? throw new ArgumentNullException(nameof(subjectDetails));
            return this;
        }

        public GrantPermissionsEntityRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Najpierw należy wywołać WithSubject(...).");
            }
            if (_permissions is null)
            {
                throw new InvalidOperationException("Po wskazaniu podmiotu należy wywołać WithPermissions(...).");
            }

            return new GrantPermissionsEntityRequest
            {
                SubjectIdentifier = _subject,
                Permissions = _permissions,
                Description = _description,
                SubjectDetails = _subjectDetails,
            };
        }
    }
}
