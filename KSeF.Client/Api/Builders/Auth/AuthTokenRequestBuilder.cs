using KSeF.Client.Core.Models.Authorization;

namespace KSeF.Client.Api.Builders.Auth;

public static class AuthTokenRequestBuilder
{

    public static IAuthTokenRequestBuilder Create() =>
        AuthTokenRequestBuilderImpl.Create();
}

public interface IAuthTokenRequestBuilder
{
    IAuthTokenRequestBuilderWithChallenge WithChallenge(string challenge);
}

public interface IAuthTokenRequestBuilderWithChallenge
{
    IAuthTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifierType type, string value);
}
public interface IAuthTokenRequestBuilderWithContext
{
    IAuthTokenRequestBuilderReady WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum type);
}

public interface IAuthTokenRequestBuilderReady
{
    IAuthTokenRequestBuilderReady WithAuthorizationPolicy(AuthenticationTokenAuthorizationPolicy authorizationPolicy);
    AuthenticationTokenRequest Build();
}

internal sealed class AuthTokenRequestBuilderImpl :
    IAuthTokenRequestBuilder,
    IAuthTokenRequestBuilderWithChallenge,
    IAuthTokenRequestBuilderReady,
    IAuthTokenRequestBuilderWithContext
{
    private string _challenge;
    private AuthenticationTokenContextIdentifier  _context;
    private AuthenticationTokenAuthorizationPolicy _authorizationPolicy;
    private AuthenticationTokenSubjectIdentifierTypeEnum _authIdentifierType;

    private AuthTokenRequestBuilderImpl() { }

    public IAuthTokenRequestBuilderWithChallenge WithChallenge(string challenge)
    {
        if (string.IsNullOrWhiteSpace(challenge))
            throw new ArgumentException(nameof(challenge));

        _challenge = challenge;
        return this;
    }

    public IAuthTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifierType type, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(nameof(value));

        _context = new AuthenticationTokenContextIdentifier  {  Type = type, Value = value };
        return this;
    }

    public IAuthTokenRequestBuilderReady WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum type)
    {
        _authIdentifierType = type;
        return this;
    }

    public IAuthTokenRequestBuilderReady WithAuthorizationPolicy(AuthenticationTokenAuthorizationPolicy authorizationPolicy)
    {
        if (authorizationPolicy is null) return this;
        _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        return this;
    }

    public AuthenticationTokenRequest Build()
    {
        if (_challenge is null)
            throw new InvalidOperationException();
        if (_context is null)
            throw new InvalidOperationException();

        return new AuthenticationTokenRequest
        {
            Challenge = _challenge,
            ContextIdentifier = _context,
            SubjectIdentifierType = _authIdentifierType,
            AuthorizationPolicy = _authorizationPolicy,
        };
    }

    public static IAuthTokenRequestBuilder Create() =>
        new AuthTokenRequestBuilderImpl();
}
