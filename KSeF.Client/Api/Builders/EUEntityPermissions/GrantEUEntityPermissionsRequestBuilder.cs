using KSeF.Client.Core.Models.Permissions.EUEntity;
namespace KSeF.Client.Api.Builders.EUEntityPermissions;

public static class GrantEUEntityPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        ISubjectNameStep WithSubject(EUEntitySubjectIdentifier subject);
    }

    public interface ISubjectNameStep
    {
        IPermissionsStep WithSubjectName(string subjectName);
    }

    public interface IPermissionsStep
    {
        IDescriptionStep WithContext(EUEntityContextIdentifier subject);
    }

    public interface IDescriptionStep
    {
        IBuildStep WithDescription(string description);
    }

    public interface IBuildStep
    {
        GrantPermissionsEUEntityRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        ISubjectNameStep,
        IPermissionsStep,
        IDescriptionStep,
        IBuildStep
    {
        private EUEntitySubjectIdentifier _subject;
        private EUEntityContextIdentifier _context;
        private string _description;
        private string _subjectName;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public ISubjectNameStep WithSubject(EUEntitySubjectIdentifier subject)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            return this;
        }

        public IPermissionsStep WithSubjectName(string subjectName)
        {
            if (string.IsNullOrWhiteSpace(subjectName))
                throw new ArgumentException("Wartość nie może być pusta ani zawierać wyłącznie białych znaków.", nameof(subjectName));
            _subjectName = subjectName;
            return this;
        }

        public IDescriptionStep WithContext(EUEntityContextIdentifier context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            return this;
        }

        public IBuildStep WithDescription(string description)
        {
            _description = description ?? throw new ArgumentNullException(nameof(description));
            return this;
        }

        public GrantPermissionsEUEntityRequest Build()
        {
            if (_subject is null)
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            if (_context is null)
                throw new InvalidOperationException("Metoda WithContext(...) musi zostać wywołana po ustawieniu podmiotu.");
            if (_description is null)
                throw new InvalidOperationException("Metoda WithDescription(...) musi zostać wywołana po ustawieniu uprawnień.");

            return new GrantPermissionsEUEntityRequest
            {
                SubjectIdentifier = _subject,
                ContextIdentifier = _context,
                Description = _description,
                EuEntityName = _subjectName
            };
        }
    }
}