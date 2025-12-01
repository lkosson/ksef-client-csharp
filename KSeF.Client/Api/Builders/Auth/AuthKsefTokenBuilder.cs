using KSeF.Client.Core.Models.Authorization;

namespace KSeF.Client.Api.Builders.Auth;

public static class AuthKsefTokenRequestBuilder
{
    public static IAuthKsefTokenRequestBuilder Create() =>
        AuthKsefTokenRequestBuilderImpl.Create();
}

public interface IAuthKsefTokenRequestBuilder
{
    IAuthKsefTokenRequestBuilderWithChallenge WithChallenge(string challenge);
}

public interface IAuthKsefTokenRequestBuilderWithChallenge
{
    IAuthKsefTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifierType type, string value);
}

public interface IAuthKsefTokenRequestBuilderWithContext
{
    IAuthKsefTokenRequestBuilderWithEncryptedToken WithEncryptedToken(string encryptedToken);
}

public interface IAuthKsefTokenRequestBuilderWithEncryptedToken
{
    IAuthKsefTokenRequestBuilderWithEncryptedToken WithAuthorizationPolicy(AuthenticationTokenAuthorizationPolicy authorizationPolicy);
    AuthenticationKsefTokenRequest Build();
}

internal sealed class AuthKsefTokenRequestBuilderImpl :
    IAuthKsefTokenRequestBuilder,
    IAuthKsefTokenRequestBuilderWithChallenge,
    IAuthKsefTokenRequestBuilderWithContext,
    IAuthKsefTokenRequestBuilderWithEncryptedToken
{
    private string _challenge;
    private AuthenticationTokenContextIdentifier _contextIdentifier;
    private string _encryptedToken;
    private AuthenticationTokenAuthorizationPolicy _authorizationPolicy; 

    private AuthKsefTokenRequestBuilderImpl() { }

    public static IAuthKsefTokenRequestBuilder Create() =>
        new AuthKsefTokenRequestBuilderImpl();

    public IAuthKsefTokenRequestBuilderWithChallenge WithChallenge(string challenge)
    {
        if (string.IsNullOrWhiteSpace(challenge))
        {
            throw new ArgumentNullException(nameof(challenge));
        }

        _challenge = challenge;
        return this;
    }

    public IAuthKsefTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifierType type, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(nameof(value));
        }

        _contextIdentifier = new AuthenticationTokenContextIdentifier { Type = type, Value = value };
        return this;
    }

    public IAuthKsefTokenRequestBuilderWithEncryptedToken WithEncryptedToken(string encryptedToken)
    {
        if (string.IsNullOrWhiteSpace(encryptedToken))
        {
            throw new ArgumentNullException(nameof(encryptedToken));
        }

        _encryptedToken = encryptedToken;
        return this;
    }

    public IAuthKsefTokenRequestBuilderWithEncryptedToken WithAuthorizationPolicy(AuthenticationTokenAuthorizationPolicy authorizationPolicy)
    {
        _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        return this;
    }

    public AuthenticationKsefTokenRequest Build()
    {
        if (_challenge is null || _contextIdentifier is null || _encryptedToken is null)
        {
            throw new InvalidOperationException("Brak wymaganych pól.");
        }

        return new AuthenticationKsefTokenRequest
        {
            Challenge = _challenge,
            ContextIdentifier = _contextIdentifier,
            EncryptedToken = _encryptedToken,
            AuthorizationPolicy = _authorizationPolicy
        };
    }
}
