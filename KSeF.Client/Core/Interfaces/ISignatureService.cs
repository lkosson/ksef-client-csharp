using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Core.Interfaces;

/// <summary>
/// Interfejs definiujący usługę podpisu elektronicznego 
/// w formacie XAdES na potrzeby procesu uwierzytelniania KSeF.
/// </summary>
public interface ISignatureService
{   /// <summary>
    /// Podpisuje wskazany dokument XML w formacie XAdES, 
    /// używając dostarczonego certyfikatu z kluczem prywatnym.
    /// </summary>
    /// <param name="unsignedXml">
    /// Dokument XML (AuthTokenRequest) w formie tekstowej.
    /// </param>
    /// <param name="certificate">
    /// Certyfikat X.509 zawierający klucz prywatny, 
    /// którym ma zostać złożony podpis.
    /// </param>
    /// <returns>
    /// Dokument XML podpisany w formacie XAdES (string).
    /// </returns>
    public Task<string> SignAsync(
        string xml, 
        X509Certificate2 certificate);
}
