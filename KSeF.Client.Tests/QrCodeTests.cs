using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using KSeF.Client.Api.Services;

namespace KSeF.Client.Tests
{
    public class QrCodeTests
    {
        private readonly VerificationLinkService _linkSvc;
        private readonly QrCodeService _qrSvc;

        public QrCodeTests()
        {
            _linkSvc = new VerificationLinkService();
            _qrSvc = new QrCodeService();
        }

        [Fact]
        public void BuildCertificateQr_WithEmbeddedPrivateKey_ShouldReturnBase64Png()
        {
            // Arrange: self-signed cert PFX z eksportowalnym kluczem
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(
                "CN=Test", rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
            var fullCert = req.CreateSelfSigned(
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(1)
            );
            var pfxBytes = fullCert.Export(X509ContentType.Pfx);
            var certWithKey = new X509Certificate2(
                pfxBytes,
                (string?)null,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet
            );

            var nip = "0000000000";
            var serial = Guid.NewGuid().ToString();
            var xml = "<x/>";

            // Act: brak privateKey → użyje wbudowanego klucza
            var url = _linkSvc.BuildCertificateVerificationUrl(nip, serial, xml, certWithKey, "");
            byte[] qrBytes = _qrSvc.GenerateQrCode(url, 5);
            byte[] labeled = _qrSvc.AddLabelToQrCode(qrBytes, "CERTYFIKAT");
            string pngBase64 = Convert.ToBase64String(labeled);

            // Assert: ciąg Base64 rozpoczyna się od standardowego PNG prefixu "iVBOR"
            Assert.StartsWith("iVBOR", pngBase64);
        }

        [Fact]
        public void BuildCertificateQr_PublicOnlyWithoutPrivateKey_ShouldThrowInvalidOperationException()
        {
            // Arrange: tylko certyfikat publiczny
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(
                "CN=Test", rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
            var fullCert = req.CreateSelfSigned(
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(1)
            );
            var publicBytes = fullCert.Export(X509ContentType.Cert);
            var publicCert = new X509Certificate2(publicBytes);

            var nip = "0000000000";
            var serial = Guid.NewGuid().ToString();
            var xml = "<x/>";

            // Act & Assert: brak privateKey i brak wbudowanego → InvalidOperationException
            Assert.Throws<InvalidOperationException>(() =>
                _linkSvc.BuildCertificateVerificationUrl(nip, serial, xml, publicCert, "")
            );
        }

        [Fact]
        public void BuildCertificateQr_PublicOnlyWithPrivateKeyParam_ShouldReturnBase64Png()
        {
            // Arrange: wygeneruj self-signed cert z kluczem RSA
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(
                "CN=FullCert",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
            var fullCert = req.CreateSelfSigned(
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(1)
            );

            // Zapisz PFX i zaimportuj z flagą Exportable — certyfikat ma teraz wbudowany klucz
            var pfxBytes = fullCert.Export(X509ContentType.Pfx);
            var certWithKey = new X509Certificate2(
                pfxBytes,
                (string?)null,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet
            );

            var nip = "0000000000";
            var xml = "<x/>";
            var serial = Guid.NewGuid().ToString();

            // Act: nie podajemy privateKey — metoda użyje certWithKey.GetRSAPrivateKey()
            var url = _linkSvc.BuildCertificateVerificationUrl(
                nip,
                serial,
                xml,
                certWithKey,
                privateKey: ""
            );

            byte[] qrBytes = _qrSvc.GenerateQrCode(url, 5);
            byte[] labeled = _qrSvc.AddLabelToQrCode(qrBytes, "CERTYFIKAT");
            string pngBase64 = Convert.ToBase64String(labeled);

            // Assert: Base64 PNG zaczyna się od "iVBOR"
            Assert.StartsWith("iVBOR", pngBase64);
        }
    }
}
