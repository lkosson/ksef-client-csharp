using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace KSeF.Client.Tests
{
    // Fixture przygotowujący dane do testów
    public class VerificationLinkScenarioFixture
    {
        public string Nip { get; set; } = "4564564567";
        public DateTime IssueDate { get; } = new DateTime(2026, 2, 1);
        public string XmlContent { get; } = "<root>test</root>";
        public X509Certificate2 Certificate { get; }

        public VerificationLinkScenarioFixture()
        {
            // Generujemy self-signed cert z kluczem prywatnym RSA
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(
                "CN=TestCert",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
            Certificate = req.CreateSelfSigned(
                DateTimeOffset.Now,
                DateTimeOffset.Now.AddDays(1)
            );
        }
    }

    // Definicja kolekcji xUnit
    [CollectionDefinition("VerificationLinkScenario")]
    public class VerificationLinkScenarioCollection
        : ICollectionFixture<VerificationLinkScenarioFixture>
    { }

    [Collection("VerificationLinkScenario")]
    public class VerificationLinkServiceTests : TestBase
    {
        private readonly VerificationLinkScenarioFixture _f;
        private readonly IVerificationLinkService _svc;

        public VerificationLinkServiceTests(VerificationLinkScenarioFixture f)
        {
            _f = f;
            _svc = new VerificationLinkService();
        }

        [Fact]
        public void Step1_BuildInvoiceVerificationUrl_ShouldMatchExpectedFormat()
        {
            // Arrange
            // Compute expected hash
            byte[] sha;
            using (var sha256 = SHA256.Create())
                sha = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(_f.XmlContent));

            var b64 = Convert.ToBase64String(sha);
            var encoded = WebUtility.UrlEncode(b64);
            var expected = $"https://ksef.mf.gov.pl/web/verify-invoice/{_f.Nip}/{_f.IssueDate:dd-MM-yyyy}/{encoded}";

            // Act
            var url = _svc.BuildInvoiceVerificationUrl(_f.Nip, _f.IssueDate, _f.XmlContent);

            // Assert
            Assert.Equal(expected, url);
        }

        [Fact]
        public void Step2_BuildCertificateVerificationUrl_ShouldContainAllSegments()
        {
            // Act
            var url = _svc.BuildCertificateVerificationUrl(
                _f.Nip,
                _f.Certificate.SerialNumberGUID(),
                _f.XmlContent,
                _f.Certificate
            );

            // url.Split('/')
            var parts = url.Split('/');

            // ["https:", "", "ksef.mf.gov.pl", "web", "verify-certificate", nip, serial, hash, signed]
            Assert.Equal("https:", parts[0]);
            Assert.Equal("ksef.mf.gov.pl", parts[2]);
            Assert.Equal("verify-certificate", parts[4]);
            Assert.Equal(_f.Nip, parts[5]);
            Assert.Equal(_f.Certificate.SerialNumberGUID().ToString(), parts[6]);
            Assert.False(string.IsNullOrWhiteSpace(parts[7])); // hash
            Assert.False(string.IsNullOrWhiteSpace(parts[8])); // signed hash
        }

        [Fact]
        public void Step3_BuildCertificateVerificationUrl_WithoutPrivateKey_ShouldThrow()
        {
            // Arrange: nowy cert bez klucza prywatnego
            var publicOnly = new X509Certificate2();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _svc.BuildCertificateVerificationUrl(
                    _f.Nip,
                    Guid.NewGuid(),
                    "<x/>",
                    publicOnly
                )
            );
        }
    }

    // Helper extension to convert SerialNumber hex into Guid
    internal static class X509Certificate2Extensions
    {
        public static Guid SerialNumberGUID(this X509Certificate2 cert)
        {
            // zakładamy, że serial ma długość 32 hex
            var bytes = Enumerable.Range(0, cert.SerialNumber.Length / 2)
                .Select(i => Convert.ToByte(cert.SerialNumber.Substring(i * 2, 2), 16))
                .ToArray();
            return new Guid(bytes);
        }
    }
}
