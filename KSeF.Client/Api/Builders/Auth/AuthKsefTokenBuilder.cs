using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.Auth;

/// <summary>
/// Zapewnia metody fabryczne do tworzenia instancji <see cref="IAuthKsefTokenRequestBuilder"/> używanych do konstruowania
/// żądań tokenów uwierzytelniających dla systemu KSeF.
/// </summary>
/// <remarks>Ta klasa nie może być instancjonowana i służy wyłącznie jako punkt wejścia do uzyskania <see
/// cref="IAuthKsefTokenRequestBuilder"/>. Aby rozpocząć tworzenie żądania tokenu, należy użyć metody <see cref="Create"/>.
/// </remarks>
public static class AuthKsefTokenRequestBuilder
{
    /// <summary>
    /// Tworzy nową instancję konstruktora żądań tokenów uwierzytelniających do integracji z KSeF.
    /// </summary>
    /// <remarks>Zwrócony konstruktor służy do określania parametrów uwierzytelniania i tworzenia żądań w celu
    /// uzyskania tokenów KSeF. Generator jest bezstanowy i może być ponownie użyty dla wielu żądań.</remarks>
    /// <returns>Instancja <see cref="IAuthKsefTokenRequestBuilder"/>, która może być użyta do konfiguracji i tworzenia żądań tokenów uwierzytelniających dla KSeF.</returns>
    public static IAuthKsefTokenRequestBuilder Create() =>
        AuthKsefTokenRequestBuilderImpl.Create();
}

/// <summary>
/// Definiuje konstruktor służący do tworzenia żądań tokenów uwierzytelniających z określonym wyzwaniem dla systemu KSeF.
/// </summary>
/// <remarks> Zwrócony konstruktor umożliwia dalszą konfigurację żądania przed jego przesłaniem.</remarks>
public interface IAuthKsefTokenRequestBuilder
{
    /// <summary>
    /// Ustawia wartość wyzwania, która ma być używana w konstruktorze żądania tokenu uwierzytelniającego.
    /// </summary>
    /// <param name="challenge">Ciąg znaków wyzwania dostarczony przez usługę uwierzytelniania. Nie może być null ani pusty.</param>
    /// <returns>Instancja <see cref="IAuthKsefTokenRequestBuilderWithChallenge"/> skonfigurowana z określoną wartością wyzwania.
    /// </returns>
    IAuthKsefTokenRequestBuilderWithChallenge WithChallenge(string challenge);
}

/// <summary>/// Definiuje metody tworzenia żądania tokenu uwierzytelniającego, które zawiera wyzwanie, umożliwiając określenie
/// informacji kontekstowych wymaganych do wygenerowania tokenu.
/// </summary>
/// <remarks>Ten interfejs służy do dodawania identyfikatorów kontekstowych do żądania tokenu uwierzytelniającego, gdy występuje wyzwanie.
/// Wynikowy konstruktor można dalej konfigurować, dodając dodatkowy kontekst przed sfinalizowaniem żądania. </remarks>
public interface IAuthKsefTokenRequestBuilderWithChallenge
{
    /// <summary>
    /// Określa kontekst żądania tokenu uwierzytelniającego przy użyciu podanego typu identyfikatora i wartości.
    /// </summary>
    /// <param name="type">Typ identyfikatora kontekstu, który ma być powiązany z żądaniem tokenu uwierzytelniającego. Określa on sposób interpretacji kontekstu przez system uwierzytelniający.</param>
    /// <param name="value">Wartość identyfikatora kontekstu. Musi to być niepusty ciąg znaków odpowiedni dla określonego typu identyfikatora.</param>
    /// <returns>Instancja <see cref="IAuthKsefTokenRequestBuilderWithContext"/> skonfigurowana z określonym kontekstem.</returns>
    IAuthKsefTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifierType type, string value);

    /// <summary>
    /// Określa kontekst żądania tokenu uwierzytelniającego przy użyciu podanego typu identyfikatora i wartości.
    /// </summary>
    /// <param name="contextIdentifier">Identyfikator reprezentujący kontekst tokenów uwierzytelniających, który ma być powiązany z konstruktorem żądań. Nie może być null. </param>
    /// <returns>Instancja <see cref="IAuthKsefTokenRequestBuilderWithContext"/> skonfigurowana do używania określonego kontekstu tokenu uwierzytelniającego.</returns>
    IAuthKsefTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifier contextIdentifier);
}

/// <summary>
/// Definiuje konstruktor do tworzenia żądań tokenów uwierzytelniających z dodatkowym kontekstem, umożliwiając określenie
/// zaszyfrowanego tokenu.
/// </summary>
public interface IAuthKsefTokenRequestBuilderWithContext
{
    /// <summary>
    /// Określa zaszyfrowany token uwierzytelniający, który ma być używany w kreatorze żądań tokenów.
    /// </summary>
    /// <param name="encryptedToken">Zaszyfrowany ciąg znaków tokenu, który zostanie dołączony do żądania uwierzytelnienia. Nie może być null ani pusty. </param>
    /// <returns>Instancja <see cref="IAuthKsefTokenRequestBuilderWithEncryptedToken"/> skonfigurowana z podanym
    /// zaszyfrowanym tokenem.</returns>
    IAuthKsefTokenRequestBuilderWithEncryptedToken WithEncryptedToken(string encryptedToken);
}

/// <summary>
/// Definiuje konstruktor służący do tworzenia żądań tokenów uwierzytelniających, które zawierają zaszyfrowany token i obsługują
/// określanie zasad autoryzacji.
/// </summary>
/// <remarks>Ten interfejs służy do płynnej konfiguracji i tworzenia instancji <see cref="AuthenticationKsefTokenRequest"/> z niestandardowymi zasadami autoryzacji.
/// Wzorzec konstruktora umożliwia konfigurację krok po kroku przed wygenerowaniem ostatecznego obiektu żądania.</remarks>
public interface IAuthKsefTokenRequestBuilderWithEncryptedToken
{
    /// <summary>
    /// Konfiguruje konstruktor tak, aby używał określonej polityki autoryzacji dla żądań tokenów uwierzytelniających.
    /// </summary>
    /// <param name="authorizationPolicy">Polityka autoryzacji, którą należy zastosować podczas generowania tokenu uwierzytelniającego. Określa, w jaki sposób token zostanie
    /// autoryzowany podczas procesu żądania. </param>
    /// <returns>Instancja <see cref="IAuthKsefTokenRequestBuilderWithEncryptedToken"/> skonfigurowana zgodnie z określoną polityką autoryzacji.</returns>
    IAuthKsefTokenRequestBuilderWithEncryptedToken WithAuthorizationPolicy(AuthenticationTokenAuthorizationPolicy authorizationPolicy);

    /// <summary>
    /// Tworzy nową instancję żądania tokenu uwierzytelniającego przy użyciu skonfigurowanych parametrów.
    /// </summary>
    /// <returns>Element <see cref="AuthenticationKsefTokenRequest"/> reprezentujący żądanie tokenu uwierzytelniającego z bieżącą
    /// konfiguracją.</returns>
    AuthenticationKsefTokenRequest Build();
}

///<inheritdoc/>
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

    ///<inheritdoc/>
    public static IAuthKsefTokenRequestBuilder Create() =>
        new AuthKsefTokenRequestBuilderImpl();

    ///<inheritdoc/>
    public IAuthKsefTokenRequestBuilderWithChallenge WithChallenge(string challenge)
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

    ///<inheritdoc/>
    public IAuthKsefTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifierType type, string value)
    {
        AuthenticationTokenContextIdentifier context = new() { Type = type, Value = value };
        return WithContext(context);
    }

    ///<inheritdoc/>
    public IAuthKsefTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifier contextIdentifier)
    {
        ArgumentNullException.ThrowIfNull(contextIdentifier);
        if (!TypeValueValidator.Validate(contextIdentifier))
        {
            throw new ArgumentException($"Nieprawidłowa wartość dla typu {contextIdentifier.Type}", nameof(contextIdentifier));
        }

        _contextIdentifier = contextIdentifier;
        return this;
    }

    ///<inheritdoc/>
    public IAuthKsefTokenRequestBuilderWithEncryptedToken WithEncryptedToken(string encryptedToken)
    {
        if (string.IsNullOrWhiteSpace(encryptedToken))
        {
            throw new ArgumentNullException(nameof(encryptedToken));
        }

        _encryptedToken = encryptedToken;
        return this;
    }

    ///<inheritdoc/>
    public IAuthKsefTokenRequestBuilderWithEncryptedToken WithAuthorizationPolicy(AuthenticationTokenAuthorizationPolicy authorizationPolicy)
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

    ///<inheritdoc/>
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
