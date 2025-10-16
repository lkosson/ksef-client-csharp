using KSeF.Client.Core.Models.Permissions.EUEntityRepresentative;

namespace KSeF.Client.Api.Builders.EUEntityRepresentativePermissions;

public static class GrantEUEntityRepresentativePermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IPermissionsStep WithSubject(EUEntitRepresentativeSubjectIdentifier subject);
    }

    public interface IPermissionsStep
    {
        IOptionalStep WithPermissions(params EUEntitRepresentativeStandardPermissionType[] permissions);
    }

    public interface IOptionalStep
    {
        IOptionalStep WithDescription(string description);
        GrantPermissionsEUEntitRepresentativeRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IOptionalStep
    {
        private EUEntitRepresentativeSubjectIdentifier _subject;
        private ICollection<EUEntitRepresentativeStandardPermissionType> _permissions;
        private string _description;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IPermissionsStep WithSubject(EUEntitRepresentativeSubjectIdentifier subject)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            return this;
        }

        public IOptionalStep WithPermissions(params EUEntitRepresentativeStandardPermissionType[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
                throw new ArgumentException("Należy podać co najmniej jedno uprawnienie.", nameof(permissions));

            _permissions = permissions;
            return this;
        }

        public IOptionalStep WithDescription(string description)
        {
            _description = description ?? throw new ArgumentNullException(nameof(description));
            return this;
        }

        public GrantPermissionsEUEntitRepresentativeRequest Build()
        {
            if (_subject is null)
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            if (_permissions is null)
                throw new InvalidOperationException("Metoda WithPermissions(...) musi zostać wywołana po ustawieniu podmiotu.");

            return new GrantPermissionsEUEntitRepresentativeRequest
            {
                SubjectIdentifier = _subject,
                Permissions = _permissions,
                Description = _description,
            };
        }
    }
}