using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.PersonPermissions;

public static class GrantPersonPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IPermissionsStep WithSubject(GrantPermissionsPersonSubjectIdentifier subject);
    }

    public interface IPermissionsStep
    {
        IDescriptionStep WithPermissions(params PersonPermissionType[] permissions);
    }

    public interface IDescriptionStep
    {
        IBuildStep WithDescription(string description);
    }

    public interface IBuildStep
    {
        IBuildStep WithSubjectDetails(PersonPermissionSubjectDetails subjectDetails);
        GrantPermissionsPersonRequest Build();
    }


    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IDescriptionStep,
        IBuildStep
    {
        private GrantPermissionsPersonSubjectIdentifier _subject;
        private ICollection<PersonPermissionType> _permissions;
        private string _description;
        private PersonPermissionSubjectDetails _subjectDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IPermissionsStep WithSubject(GrantPermissionsPersonSubjectIdentifier subject)
        {
            ArgumentNullException.ThrowIfNull(subject);
            if (!TypeValueValidator.Validate(subject))
            {
                throw new ArgumentException($"Nieprawidłowa wartość dla typu {subject.Type}", nameof(subject));
            }
            _subject = subject;
            return this;
        }

        public IDescriptionStep WithPermissions(params PersonPermissionType[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
            {
                throw new ArgumentException("Należy podać co najmniej jedno uprawnienie.", nameof(permissions));
            }

            _permissions = permissions;
            return this;
        }

        public IBuildStep WithDescription(string description)
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

        public IBuildStep WithSubjectDetails(PersonPermissionSubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails;
            return this;
        }

        public GrantPermissionsPersonRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            }
            if (_permissions is null)
            {
                throw new InvalidOperationException("Metoda WithPermissions(...) musi zostać wywołana po ustawieniu podmiotu.");
            }
            if (_description is null)
            {
                throw new InvalidOperationException("Metoda WithDescription(...) musi zostać wywołana po ustawieniu uprawnień.");
            }
            return new GrantPermissionsPersonRequest
            {
                SubjectIdentifier = _subject,
                Permissions = _permissions,
                Description = _description,
                SubjectDetails = _subjectDetails,
            };
        }
    }
}