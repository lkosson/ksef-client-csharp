using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.SubEntityPermissions;

public static class GrantSubunitPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IContextStep WithSubject(SubunitSubjectIdentifier subject);
    }

    public interface IContextStep
    {
        IOptionalStep WithContext(SubunitContextIdentifier context);
    }

    public interface ISubunitNameStep
    {
        IOptionalStep WithSubunitName(string subunitName);
    }

    public interface IOptionalStep
    {
        IOptionalStep WithDescription(string description);
        IOptionalStep WithSubunitName(string subunitName);
        IOptionalStep WithSubjectDetails(SubunitSubjectDetails subjectDetails);
        GrantPermissionsSubunitRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IContextStep,
        IOptionalStep
    {
        private SubunitSubjectIdentifier _subject;
        private SubunitContextIdentifier _context;
        private string _description;
        private string _subunitName;
        private SubunitSubjectDetails _subjectDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IContextStep WithSubject(SubunitSubjectIdentifier subject)
        {
            ArgumentNullException.ThrowIfNull(subject);
            if (!TypeValueValidator.Validate(subject))
            {
                throw new ArgumentException($"Nieprawidłowa wartość dla typu {subject.Type}", nameof(subject));
            }
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            return this;
        }

        public IOptionalStep WithContext(SubunitContextIdentifier context)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (!TypeValueValidator.Validate(context))
            {
                throw new ArgumentException($"Nieprawidłowa wartość dla typu {context.Type}", nameof(context));
            }
            _context = context;
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

        public IOptionalStep WithSubunitName(string subunitName)
        {
            if (_context.Type == SubunitContextIdentifierType.InternalId)
            {
                if (string.IsNullOrWhiteSpace(subunitName))
                {
                    throw new InvalidOperationException($"Dla typu {nameof(SubunitContextIdentifierType.InternalId)} parametr {nameof(subunitName)} nie może być pusty");
                }
                if (subunitName.Length < ValidValues.SubunitNameMinLength)
                {
                    throw new ArgumentException($"Nazwa jednostki podrzędnej za krótka, minimalna długość: {ValidValues.SubunitNameMinLength} znaków.", nameof(subunitName));
                }
                if (subunitName.Length > ValidValues.SubunitNameMaxLength)
                {
                    throw new ArgumentException($"Nazwa jednostki podrzędnej za długa, maksymalna długość: {ValidValues.SubunitNameMaxLength} znaków.", nameof(subunitName));
                }
            }
            _subunitName = subunitName;
            return this;
        }

        public IOptionalStep WithSubjectDetails(SubunitSubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails ?? throw new ArgumentNullException(nameof(subjectDetails));
            return this;
        }

        public GrantPermissionsSubunitRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            }

            if (_context is null)
            {
                throw new InvalidOperationException("Metoda WithContext(...) musi zostać wywołana po ustawieniu podmiotu.");
            }

            if (_context.Type == SubunitContextIdentifierType.InternalId && string.IsNullOrWhiteSpace(_subunitName))
            {
                throw new InvalidOperationException($"Dla typu {nameof(SubunitContextIdentifierType.InternalId)} metoda WithSubunitName(...) musi zostać wywołana przed Build().");
            }

            return new GrantPermissionsSubunitRequest
            {
                SubjectIdentifier = _subject,
                ContextIdentifier = _context,
                Description = _description,
                SubunitName = _subunitName,
                SubjectDetails = _subjectDetails
            };
        }
    }
}