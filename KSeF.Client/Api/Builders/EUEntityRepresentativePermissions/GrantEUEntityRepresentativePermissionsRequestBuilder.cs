using KSeF.Client.Core.Models.Permissions.EuEntityRepresentative;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.EUEntityRepresentativePermissions;

public static class GrantEUEntityRepresentativePermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IPermissionsStep WithSubject(EuEntityRepresentativeSubjectIdentifier subject);
    }

    public interface IPermissionsStep
    {
        IOptionalStep WithPermissions(params EuEntityRepresentativeStandardPermissionType[] permissions);
    }

    public interface IOptionalStep
    {
        IOptionalStep WithDescription(string description);
        IOptionalStep WithSubjectDetails(EuEntityRepresentativeSubjectDetails subjectDetails);
        GrantPermissionsEuEntityRepresentativeRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IOptionalStep
    {
        private EuEntityRepresentativeSubjectIdentifier _subject;
        private ICollection<EuEntityRepresentativeStandardPermissionType> _permissions;
        private string _description;
        private EuEntityRepresentativeSubjectDetails _subjectDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IPermissionsStep WithSubject(EuEntityRepresentativeSubjectIdentifier subject)
        {
            ArgumentNullException.ThrowIfNull(subject);
            if (!TypeValueValidator.Validate(subject))
            {
                throw new ArgumentException($"Nieprawidłowa wartość dla typu {subject.Type}", nameof(subject));
            }
            _subject = subject;
            return this;
        }

        public IOptionalStep WithPermissions(params EuEntityRepresentativeStandardPermissionType[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
            {
                throw new ArgumentException("Należy podać co najmniej jedno uprawnienie.", nameof(permissions));
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

        public IOptionalStep WithSubjectDetails(EuEntityRepresentativeSubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails ?? throw new ArgumentNullException(nameof(subjectDetails));
            return this;
        }

        public GrantPermissionsEuEntityRepresentativeRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            }
            if (_permissions is null)
            {
                throw new InvalidOperationException("Metoda WithPermissions(...) musi zostać wywołana po ustawieniu podmiotu.");
            }
            return new GrantPermissionsEuEntityRepresentativeRequest
            {
                SubjectIdentifier = _subject,
                Permissions = _permissions,
                Description = _description,
                SubjectDetails = _subjectDetails,
            };
        }
    }
}