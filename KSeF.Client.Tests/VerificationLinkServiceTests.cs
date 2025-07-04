using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

namespace KSeF.Client.Tests
{
    public class VerificationLinkServiceTests
    {
        private readonly IVerificationLinkService _svc = new VerificationLinkService();
        private const string BaseUrl = "https://ksef.mf.gov.pl/web";

        [Theory]
        [InlineData("<root>test</root>")]
        [InlineData("<data>special & chars /?</data>")]
        public void BuildInvoiceVerificationUrl_EncodesHashCorrectly(string xml)
        {
            // Arrange
            var nip = "1234567890";
            var issueDate = new DateTime(2026, 1, 5);

            byte[] sha;
            using (var sha256 = SHA256.Create())
                sha = sha256.ComputeHash(Encoding.UTF8.GetBytes(xml));

            var expectedHash = HttpUtility.UrlEncode(Convert.ToBase64String(sha));
            var expectedUrl = $"{BaseUrl}/verify-invoice/{nip}/{issueDate:dd-MM-yyyy}/{expectedHash}";

            // Act
            var url = _svc.BuildInvoiceVerificationUrl(nip, issueDate, xml);

            // Assert
            Assert.Equal(expectedUrl, url);

            var segments = new Uri(url)
                .Segments
                .Select(s => s.Trim('/'))
                .ToArray();

            Assert.Equal("web", segments[1]);
            Assert.Equal("verify-invoice", segments[2]);
            Assert.Equal(nip, segments[3]);
            Assert.Equal(issueDate.ToString("dd-MM-yyyy"), segments[4]);
            Assert.Equal(expectedHash, segments[5]);
        }

        [Fact]
        public void BuildCertificateVerificationUrl_WithRsaCertificate_ShouldMatchFormat()
        {
            // Arrange
            var nip = "4564564567";
            var xml = "<root>foo</root>";
            var serial = Guid.NewGuid();

            // Create full self-signed RSA cert with private key
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest("CN=TestRSA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var fullCert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));

            // Act
            var url = _svc.BuildCertificateVerificationUrl(nip, serial, xml, fullCert);

            // Assert
            var segments = new Uri(url)
                .Segments
                .Select(s => s.Trim('/'))
                .ToArray();

            Assert.Equal("web", segments[1]);
            Assert.Equal("verify-certificate", segments[2]);
            Assert.Equal(nip, segments[3]);
            Assert.Equal(serial.ToString(), segments[4]);
            Assert.False(string.IsNullOrWhiteSpace(segments[5])); // hash
            Assert.False(string.IsNullOrWhiteSpace(segments[6])); // signed hash
        }

        [Fact]
        public void BuildCertificateVerificationUrl_WithEcdsaCertificate_ShouldMatchFormat()
        {
            // Arrange
            var nip = "1234567890";
            var xml = "<data>ecdsa</data>";
            var serial = Guid.NewGuid();

            // Create full self-signed ECDsa cert with private key
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest("CN=TestECDSA", ecdsa, HashAlgorithmName.SHA256);
            var fullCert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

            // Act
            var url = _svc.BuildCertificateVerificationUrl(nip, serial, xml, fullCert);

            // Assert
            var segments = new Uri(url)
                .Segments
                .Select(s => s.Trim('/'))
                .ToArray();

            Assert.Equal("web", segments[1]);
            Assert.Equal("verify-certificate", segments[2]);
            Assert.Equal(nip, segments[3]);
            Assert.Equal(serial.ToString(), segments[4]);
            Assert.False(string.IsNullOrWhiteSpace(segments[5])); // hash
            Assert.False(string.IsNullOrWhiteSpace(segments[6])); // signed hash
        }

        [Fact]
        public void BuildCertificateVerificationUrl_WithoutPrivateKey_ShouldThrow()
        {
            // Arrange: export public part only
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest("CN=PublicOnly", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var fullCert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));
            var publicBytes = fullCert.Export(X509ContentType.Cert);
            var pubOnly = new X509Certificate2(publicBytes);

            var nip = "0000000000";
            var xml = "<x/>";
            var serial = Guid.NewGuid();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _svc.BuildCertificateVerificationUrl(nip, serial, xml, pubOnly)
            );
        }
    }
}
