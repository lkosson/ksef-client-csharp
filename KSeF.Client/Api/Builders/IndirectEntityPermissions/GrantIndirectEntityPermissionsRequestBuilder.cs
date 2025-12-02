using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.IndirectEntityPermissions;

public static class GrantIndirectEntityPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IContextStep WithSubject(IndirectEntitySubjectIdentifier subject);
    }
    public interface IContextStep
    {
        IPermissionsStep WithContext(IndirectEntityTargetIdentifier context);
    }

    public interface IPermissionsStep
    {
        IOptionalStep WithPermissions(params IndirectEntityStandardPermissionType[] permissions);
    }

    public interface IOptionalStep
    {
        IOptionalStep WithDescription(string description);
        IOptionalStep WithSubjectDetails(PermissionsIndirectEntitySubjectDetails subjectDetails);
        GrantPermissionsIndirectEntityRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IContextStep,
        IPermissionsStep,
        IOptionalStep
    {
        private IndirectEntitySubjectIdentifier _subject;
        private ICollection<IndirectEntityStandardPermissionType> _permissions;
        private string _description;
        private IndirectEntityTargetIdentifier _context;
        private PermissionsIndirectEntitySubjectDetails _subjectDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IContextStep WithSubject(IndirectEntitySubjectIdentifier subject)
        {
            ArgumentNullException.ThrowIfNull(subject);
            if (!TypeValueValidator.Validate(subject))
            {
                throw new ArgumentException($"Nieprawidłowa wartość dla typu {subject.Type}", nameof(subject));
            }
            _subject = subject;
            return this;
        }

        public IOptionalStep WithPermissions(params IndirectEntityStandardPermissionType[] permissions)
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

        public IOptionalStep WithSubjectDetails(PermissionsIndirectEntitySubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails ?? throw new ArgumentNullException(nameof(subjectDetails));
            return this;
        }

        public GrantPermissionsIndirectEntityRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            }
            if (_context is null)
            {
                throw new InvalidOperationException("Metoda WithContext(...) musi zostać wywołana po ustawieniu podmiotu.");
            }
            if (_permissions is null)
            {
                throw new InvalidOperationException("Metoda WithPermissions(...) musi zostać wywołana po ustawieniu kontekstu.");
            }
            return new GrantPermissionsIndirectEntityRequest
            {
                SubjectIdentifier = _subject,
                TargetIdentifier = _context,
                Permissions = _permissions,
                Description = _description,
                SubjectDetails = _subjectDetails,
            };
        }

        public IPermissionsStep WithContext(IndirectEntityTargetIdentifier context)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (!TypeValueValidator.Validate(context))
            {
                throw new ArgumentException($"Nieprawidłowa wartość dla typu {context.Type}", nameof(context));
            }
            _context = context;
            return this;
        }
    }
}