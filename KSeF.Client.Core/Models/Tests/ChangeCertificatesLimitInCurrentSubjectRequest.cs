namespace KSeF.Client.Core.Models.Tests
{
    public sealed class ChangeCertificatesLimitInCurrentSubjectRequest
    {
        public SubjectIdentifierType SubjectIdentifierType { get; set; }
        public Enrollment Enrollment { get; set; }
        public Certificate Certificate { get; set; }
    }
}
