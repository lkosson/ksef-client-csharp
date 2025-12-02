using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Validation;
namespace KSeF.Client.Api.Builders.EuEntityPermissions;

public static class GrantEuEntityPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        ISubjectNameStep WithSubject(EuEntitySubjectIdentifier subject);
    }

    public interface ISubjectNameStep
    {
        IPermissionsStep WithSubjectName(string subjectName);
    }

    public interface IPermissionsStep
    {
        IDescriptionStep WithContext(EuEntityContextIdentifier subject);
    }

    public interface IDescriptionStep
    {
        IBuildStep WithDescription(string description);
    }

    public interface IBuildStep
    {
        // Opcjonalne
        IBuildStep WithSubjectDetails(PermissionsEuEntitySubjectDetails subjectDetails);
        IBuildStep WithEuEntityDetails(PermissionsEuEntityDetails euEntityDetails);
        GrantPermissionsEuEntityRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        ISubjectNameStep,
        IPermissionsStep,
        IDescriptionStep,
        IBuildStep
    {
        private EuEntitySubjectIdentifier _subject;
        private EuEntityContextIdentifier _context;
        private string _description;
        private string _subjectName;
        private PermissionsEuEntitySubjectDetails _subjectDetails;
        private PermissionsEuEntityDetails _euEntityDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public ISubjectNameStep WithSubject(EuEntitySubjectIdentifier subject)
        {
            ArgumentNullException.ThrowIfNull(subject);
            if (!TypeValueValidator.Validate(subject))
            {
                throw new ArgumentException($"Nieprawidłowa wartość dla typu {subject.Type}", nameof(subject));
            }
            _subject = subject;
            return this;
        }

        public IPermissionsStep WithSubjectName(string subjectName)
        {
            if (string.IsNullOrWhiteSpace(subjectName))
            {
                throw new ArgumentException("Wartość nie może być pusta ani zawierać wyłącznie białych znaków.", nameof(subjectName));
            }

            _subjectName = subjectName;
            return this;
        }

        public IDescriptionStep WithContext(EuEntityContextIdentifier context)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (!TypeValueValidator.Validate(context))
            {
                throw new ArgumentException($"Nieprawidłowa wartość dla typu {context.Type}", nameof(context));
            }
            _context = context;
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

        public IBuildStep WithSubjectDetails(PermissionsEuEntitySubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails;
            return this;
        }

        public IBuildStep WithEuEntityDetails(PermissionsEuEntityDetails euEntityDetails)
        {
            _euEntityDetails = euEntityDetails;
            return this;
        }

        public GrantPermissionsEuEntityRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            }
            if (_context is null)
            {
                throw new InvalidOperationException("Metoda WithContext(...) musi zostać wywołana po ustawieniu podmiotu.");
            }
            if (_description is null)
            {
                throw new InvalidOperationException("Metoda WithDescription(...) musi zostać wywołana po ustawieniu uprawnień.");
            }

            return new GrantPermissionsEuEntityRequest
            {
                SubjectIdentifier = _subject,
                ContextIdentifier = _context,
                Description = _description,
                EuEntityName = _subjectName,
                SubjectDetails = _subjectDetails,
                EuEntityDetails = _euEntityDetails
            };
        }
    }
}