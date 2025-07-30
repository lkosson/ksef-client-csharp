using KSeF.Client.Api.Services;
using KSeFClient.DI;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KSeF.Client.Tests
{
    public class QrCodeTests
    {
        private readonly VerificationLinkService _linkSvc;
        private readonly QrCodeService _qrSvc;

        public QrCodeTests()
        {
            _linkSvc = new VerificationLinkService(new KSeFClientOptions() { BaseUrl = KsefEnviromentsUris.TEST });
            _qrSvc = new QrCodeService();
        }

        // =============================================
        // Testy RSA; NIEZALECANE):
        // - Klucze RSA 2048-bit:
        //   • Bezpieczeństwo porównywalne z ECC P-256, ale wyższy rozmiar
        //   • Dłuższe linki QR (więcej miejsca w wizualizacji)
        //   • Wolniejsze generowanie kluczy, podpis i weryfikacja
        //   • Większe zużycie pamięci i miejsca na dysku
        // =============================================
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
            var xml = "<x/>";
            var serial = Guid.NewGuid().ToString();
            string invoiceHash;
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(xml));
                invoiceHash = Convert.ToBase64String(hashBytes);
            }

            // Act: brak privateKey → użyje wbudowanego klucza
            var url = _linkSvc.BuildCertificateVerificationUrl(nip,Core.Models.QRCode.ContextIdentifierType.Nip,nip, serial, invoiceHash, certWithKey);
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
            var xml = "<x/>";
            var serial = Guid.NewGuid().ToString();
            string invoiceHash;
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(xml));
                invoiceHash = Convert.ToBase64String(hashBytes);
            }

            // Act & Assert: brak privateKey i brak wbudowanego → InvalidOperationException
            Assert.Throws<InvalidOperationException>(() =>
                _linkSvc.BuildCertificateVerificationUrl(nip, Core.Models.QRCode.ContextIdentifierType.Nip, nip, serial, invoiceHash, publicCert)
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
            string invoiceHash;
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(xml));
                invoiceHash = Convert.ToBase64String(hashBytes);
            }

            // Act: nie podajemy privateKey — metoda użyje certWithKey.GetRSAPrivateKey()
            var url = _linkSvc.BuildCertificateVerificationUrl(nip, Core.Models.QRCode.ContextIdentifierType.Nip,
                nip,
                serial,
                invoiceHash,
                certWithKey
            );

            byte[] qrBytes = _qrSvc.GenerateQrCode(url, 5);
            byte[] labeled = _qrSvc.AddLabelToQrCode(qrBytes, "CERTYFIKAT");
            string pngBase64 = Convert.ToBase64String(labeled);

            // Assert: Base64 PNG zaczyna się od "iVBOR"
            Assert.StartsWith("iVBOR", pngBase64);
        }

        // =============================================
        // Rekomendowane testy ECC (ECDSA P-256):
        // • Bezpieczeństwo jak RSA-2048 przy mniejszych kluczach i podpisach
        // • Krótsze linki QR i mniejsze zużycie zasobów
        // • Szybsze operacje: generowanie, podpis, weryfikacja
        // =============================================

        [Fact]
        public void BuildCertificateQr_WithEmbeddedEccPrivateKey_ShouldReturnBase64Png()
        {
            // Rekomendowane: użyj ECDSA prime256v1 (P-256)
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest(
                "CN=TestEcc", ecdsa,
                HashAlgorithmName.SHA256
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
            var xml = "<x/>";
            var serial = Guid.NewGuid().ToString();
            string invoiceHash;
            using (var sha256 = SHA256.Create())
            {
                invoiceHash = Convert.ToBase64String(
                    sha256.ComputeHash(Encoding.UTF8.GetBytes(xml))
                );
            }

            // Act: brak jawnego klucza prywatnego → używa osadzonego klucza ECDSA
            var url = _linkSvc.BuildCertificateVerificationUrl(nip, Core.Models.QRCode.ContextIdentifierType.Nip, nip, serial, invoiceHash, certWithKey);
            var qrBytes = _qrSvc.GenerateQrCode(url, 5);
            var labeled = _qrSvc.AddLabelToQrCode(qrBytes, "CERTYFIKAT");
            var pngBase64 = Convert.ToBase64String(labeled);

            // Assert
            Assert.StartsWith("iVBOR", pngBase64);
        }

        [Fact]
        public void BuildCertificateQr_PublicEccOnlyWithoutPrivateKey_ShouldThrowInvalidOperationException()
        {
            // Rekomendowane: klienci powinni generować klucze ECDSA; public-only powinno zwrócić błąd
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest(
                "CN=TestEccPublic", ecdsa,
                HashAlgorithmName.SHA256
            );
            var fullCert = req.CreateSelfSigned(
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(1)
            );
            var publicBytes = fullCert.Export(X509ContentType.Cert);
            var publicCert = new X509Certificate2(publicBytes);

            var nip = "0000000000";
            var xml = "<x/>";
            var serial = Guid.NewGuid().ToString();
            string invoiceHash;
            using (var sha256 = SHA256.Create())
            {
                invoiceHash = Convert.ToBase64String(
                    sha256.ComputeHash(Encoding.UTF8.GetBytes(xml))
                );
            }

            Assert.Throws<InvalidOperationException>(() =>
                _linkSvc.BuildCertificateVerificationUrl(nip, Core.Models.QRCode.ContextIdentifierType.Nip, nip, serial, invoiceHash, publicCert)
            );
        }

        [Fact]
        public void BuildCertificateQr_PublicEccOnlyWithPrivateKeyParam_ShouldReturnBase64Png()
        {
            // Rekomendowane: jawny import klucza prywatnego P-256
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var req = new CertificateRequest(
                "CN=FullEccCert", ecdsa,
                HashAlgorithmName.SHA256
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
            var xml = "<x/>";
            var serial = Guid.NewGuid().ToString();
            string invoiceHash;
            using (var sha256 = SHA256.Create())
            {
                invoiceHash = Convert.ToBase64String(
                    sha256.ComputeHash(Encoding.UTF8.GetBytes(xml))
                );
            }

            var url = _linkSvc.BuildCertificateVerificationUrl(nip, Core.Models.QRCode.ContextIdentifierType.Nip, nip, serial, invoiceHash, certWithKey);
            var qrBytes = _qrSvc.GenerateQrCode(url, 5);
            var labeled = _qrSvc.AddLabelToQrCode(qrBytes, "CERTYFIKAT");
            var pngBase64 = Convert.ToBase64String(labeled);

            Assert.StartsWith("iVBOR", pngBase64);
        }
    }
}
