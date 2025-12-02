using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Validation;

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
    IAuthTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifier contextIdentifier);
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
    private AuthenticationTokenContextIdentifier _context;
    private AuthenticationTokenAuthorizationPolicy _authorizationPolicy;
    private AuthenticationTokenSubjectIdentifierTypeEnum _authIdentifierType;

    private AuthTokenRequestBuilderImpl() { }

    public IAuthTokenRequestBuilderWithChallenge WithChallenge(string challenge)
    {
        if (string.IsNullOrWhiteSpace(challenge))
        {
            throw new ArgumentNullException(nameof(challenge));
        }
        if (challenge.Length != ValidValues.RequiredChallengeLength)
        {
            throw new ArgumentException($"Podany parametr: {nameof(challenge)} nie ma wymaganej liczby {ValidValues.RequiredChallengeLength} znaków", nameof(challenge));
        }

        _challenge = challenge;
        return this;
    }

    public IAuthTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifierType type, string value)
    {
        AuthenticationTokenContextIdentifier context = new() { Type = type, Value = value };
        return WithContext(context);
    }

    public IAuthTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifier contextIdentifier)
    {
        ArgumentNullException.ThrowIfNull(contextIdentifier);
        if (!TypeValueValidator.Validate(contextIdentifier))
        {
            throw new ArgumentException($"Nieprawidłowa wartość dla typu {contextIdentifier.Type}", nameof(contextIdentifier));
        }

        _context = contextIdentifier;
        return this;
    }

    public IAuthTokenRequestBuilderReady WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum type)
    {
        _authIdentifierType = type;
        return this;
    }

    public IAuthTokenRequestBuilderReady WithAuthorizationPolicy(AuthenticationTokenAuthorizationPolicy authorizationPolicy)
    {
        if (authorizationPolicy is null)
        {
            return this;
        }

        AuthenticationTokenAllowedIps allowedIps = authorizationPolicy.AllowedIps;
        if (allowedIps is null)
        {
            return this;
        }

        foreach (string ipAddress in allowedIps.Ip4Addresses ?? Enumerable.Empty<string>())
        {
            if (!RegexPatterns.Ip4Address.IsMatch(ipAddress))
            {
                throw new ArgumentException($"Nieprawidłowy adres IP: {ipAddress}", nameof(authorizationPolicy));
            }
        }
        foreach (string ipRange in allowedIps.Ip4Ranges ?? Enumerable.Empty<string>())
        {
            if (!RegexPatterns.Ip4Range.IsMatch(ipRange))
            {
                throw new ArgumentException($"Nieprawidłowy zakres adresów IP: {ipRange}", nameof(authorizationPolicy));
            }
        }
        foreach (string ipMask in allowedIps.Ip4Masks ?? Enumerable.Empty<string>())
        {
            if (!RegexPatterns.Ip4Mask.IsMatch(ipMask))
            {
                throw new ArgumentException($"Nieprawidłowy adres IP/maska: {ipMask}", nameof(authorizationPolicy));
            }
        }

        _authorizationPolicy = authorizationPolicy;
        return this;
    }

    public AuthenticationTokenRequest Build()
    {
        if (_challenge is null)
        {
            throw new InvalidOperationException();
        }

        if (_context is null)
        {
            throw new InvalidOperationException();
        }

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
