using KSeF.Client.Core.Models.Permissions.EUEntity;
namespace KSeF.Client.Api.Builders.EUEntityPermissions;

public static class GrantEUEntityPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IPermissionsStep WithSubject(SubjectIdentifier subject);
    }

    public interface IPermissionsStep
    {
        IDescriptionStep WithContext(ContextIdentifier subject);
    }

    public interface IDescriptionStep
    {
        IBuildStep WithDescription(string description);        
    }

    public interface IBuildStep
    {       
        GrantPermissionsRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IDescriptionStep,
        IBuildStep
    {
        private SubjectIdentifier _subject;
        private ContextIdentifier _context;
        private string _description;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IPermissionsStep WithSubject(SubjectIdentifier subject)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            return this;
        }

        public IDescriptionStep WithContext(ContextIdentifier context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            return this;
        }

        public IBuildStep WithDescription(string description)
        {
            _description = description ?? throw new ArgumentNullException(nameof(description));
            return this;
        }

        public GrantPermissionsRequest Build()
        {
            if (_subject is null)
                throw new InvalidOperationException("WithSubject(...) must be called first.");
            if (_context is null)
                throw new InvalidOperationException("WithContext(...) must be called after subject.");
            if (_description is null)
                throw new InvalidOperationException("WithDescription(...) must be called after permissions.");

            return new GrantPermissionsRequest
            {
                SubjectIdentifier = _subject,
                ContextIdentifier = _context,
                Description = _description,
            };
        }
    }
}
