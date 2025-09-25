namespace KSeF.Client.Core.Models.Token
{
    public sealed class TokenSubjectDetails
    {
        public TokenSubjectIdentifier? SubjectIdentifier { get; init; }
        public string[] GivenNames { get; init; } = Array.Empty<string>();
        public string? Surname { get; init; }
        public string? SerialNumber { get; init; }
        public string? CommonName { get; init; }
        public string? CountryName { get; init; }
    }
}
