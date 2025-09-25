using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.QRCode;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.DI;
using KSeF.Client.Tests.Utils;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KSeF.Client.Tests;

public class QrCodeOfflineE2EScenarioFixture
{
    public string? AccessToken { get; set; }
    public string? Nip { get; set; }
    public string? InvoiceHash { get; set; }
    public string? PrivateKey { get; set; }
    public CertificateResponse? Certificate { get; set; }
    public DateTime? InvoiceDate { get; set; }
}

[CollectionDefinition("QrCodeOfflineE2EScenario")]
public class QrCodeE2EScenarioCollection
: ICollectionFixture<QrCodeOfflineE2EScenarioFixture>
{ }
[Collection("QrCodeOfflineE2EScenario")]
public class QrCodeOfflineE2ETests : KsefIntegrationTestBase
{
    private readonly QrCodeOfflineE2EScenarioFixture Fixture;

    public QrCodeOfflineE2ETests(QrCodeOfflineE2EScenarioFixture fixture)
    {
        Fixture = fixture;
        Fixture.Nip = MiscellaneousUtils.GetRandomNip();

        AuthOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, Fixture.Nip).GetAwaiter().GetResult();
        Fixture.AccessToken = authInfo.AccessToken.Token;
    }

    /// <summary>
    /// End-to-end test weryfikujący pełny, zakończony sukcesem przebieg wystawienia kodów QR do faktury w trybie offline (offlineMode = true).
    /// Test używa faktury FA(2) oraz szyfrowania RSA.
    /// </summary>
    /// <remarks>
    /// Kroki:
    /// 1. Autoryzacja, pozyskanie tokenu dostępu. (w konstruktorze).
    /// 2. Utworzenie Certificate Signing Request (CSR) oraz klucz prywatny za pomocą RSA.
    /// 3. Zapisanie klucza prywatnego (private key).
    /// 4. Utworzenie i wysłanie żądania wystawienia certyfikatu KSeF.
    /// 5. Sprawdzenie statusu żądania, oczekiwanie na zakończenie przetwarzania CSR.
    /// 6. Pobranie certyfikatu KSeF.
    /// 7. Odfiltrowanie i zapisanie właściwego certyfikatu.
    /// Następnie cały proces odbywa się offline, bez kontaktu z KSeF:
    /// 8. Przygotowanie faktury FA(2) w formacie XML.
    /// 9. Zapisanie skrótu faktury (hash).
    /// 10. Utworzenie odnośnika (Url) do weryfikacji faktury (KOD I).
    /// 11. Utworzenie kodu QR faktury (KOD I) dla trybu offline.
    /// 12. Utworzenie odnośnika (Url) do weryfikacji certyfikatu (KOD II).
    /// 13. Utworzenie kodu QR do weryfikacji certyfikatu (KOD II) dla trybu offline.
    /// </remarks>
    [Fact]
    public async Task QrCodeOfflineE2ETest()
    {
        //Utworzenie Certificate Signing Request (csr) oraz klucz prywatny za pomocą RSA
        (string csr, string privateKey) = await CertificateUtils.GenerateCsrAndPrivateKeyAsync(KsefClient, Fixture.AccessToken, CryptographyService, RSASignaturePadding.Pkcs1);

        // Zapisanie klucza prywatnego (private key) do pamięci tylko na potrzeby testu, w rzeczywistości powinno być bezpiecznie przechowywane
        Fixture.PrivateKey = privateKey;

        //Utworzenie i wysłanie żądania wystawienia certyfikatu KSeF
        CertificateEnrollmentResponse certificateEnrollment = await CertificateUtils.SendCertificateEnrollmentAsync(KsefClient, Fixture.AccessToken, csr, CertificateType.Offline);

        //Sprawdzenie statusu żądania, oczekiwanie na zakończenie przetwarzania CSR
        CertificateEnrollmentStatusResponse enrollmentStatus = await KsefClient
            .GetCertificateEnrollmentStatusAsync(certificateEnrollment.ReferenceNumber, Fixture.AccessToken, CancellationToken.None);
        int numbersOfTriesForCertificate = 0;
        while (enrollmentStatus.Status.Code == 100 && numbersOfTriesForCertificate < 10)
        {
            await Task.Delay(1000);
            enrollmentStatus = await KsefClient
                            .GetCertificateEnrollmentStatusAsync(certificateEnrollment.ReferenceNumber, Fixture.AccessToken, CancellationToken.None);
            numbersOfTriesForCertificate++;
        }
        Assert.True(enrollmentStatus.Status.Code == 200);

        //Pobranie certyfikatu KSeF
        List<string> serialNumbers = new List<string> { enrollmentStatus.CertificateSerialNumber };
        CertificateListResponse certificateListResponse = await KsefClient
            .GetCertificateListAsync(new CertificateListRequest { CertificateSerialNumbers = serialNumbers }, Fixture.AccessToken, CancellationToken.None);
        Assert.NotNull(certificateListResponse);
        Assert.Single(certificateListResponse.Certificates);

        //Odfiltrowanie i zapisanie właściwego certyfikatu do pamięci, w rzeczywistości powinien być bezpiecznie przechowywany
        Fixture.Certificate = certificateListResponse.Certificates
            .Single(x => x.CertificateType == CertificateType.Offline &&
                        x.CertificateSerialNumber == enrollmentStatus.CertificateSerialNumber);
        Assert.NotNull(Fixture.Certificate);

        //=====Od tego momentu tryb offline bez dostępu do KSeF=====

        //Przygotowanie faktury FA(2) w formacie XML
        string path = Path.Combine(AppContext.BaseDirectory, "Templates", "invoice-template-fa-2.xml");
        string xml = File.ReadAllText(path, Encoding.UTF8);
        xml = xml.Replace("#nip#", Fixture.Nip);
        xml = xml.Replace("#invoice_number#", $"{Guid.NewGuid()}");
        
        //TODO poprawić datę w xml na poniższą
        Fixture.InvoiceDate = DateTime.Parse("2025-10-01");
        MemoryStream memoryStream = new(Encoding.UTF8.GetBytes(xml));

        //gotową fakturę należy zapisać, aby wysłać do KSeF później (zgodnie z obowiązującymi przepisami), oznaczoną jako offlineMode = true
        byte[] invoice = memoryStream.ToArray();

        FileMetadata? invoiceMetadata = CryptographyService.GetMetaData(invoice);
        //Zapisanie skrótu faktury (hash)
        Fixture.InvoiceHash = invoiceMetadata.HashSHA;

        //Utworzenie odnośnika (Url) do weryfikacji faktury (KOD I)
        string invoiceForOfflineUrl = VerificationLinkService.BuildInvoiceVerificationUrl(Fixture.Nip, Fixture.InvoiceDate.Value, Fixture.InvoiceHash);

        Assert.NotNull(invoiceForOfflineUrl);
        Assert.Contains(Base64Url.EncodeToString(Convert.FromBase64String(Fixture.InvoiceHash)), invoiceForOfflineUrl);
        Assert.Contains(Fixture.Nip, invoiceForOfflineUrl);
        Assert.Contains(Fixture.InvoiceDate.Value.ToString("dd-MM-yyyy"), invoiceForOfflineUrl);

        //Utworzenie kodu QR faktury (KOD I) dla trybu offline
        byte[]? qrOffline = QRCodeService.GenerateQrCode(invoiceForOfflineUrl);

        //Dodanie etykiety OFFLINE
        qrOffline = QRCodeService.AddLabelToQrCode(qrOffline, "OFFLINE");

        Assert.NotEmpty(qrOffline);

        //Utworzenie odnośnika (Url) do weryfikacji certyfikatu (KOD II)
        byte[] certBytes = Convert.FromBase64String(Fixture.Certificate.Certificate);
        X509Certificate2 cert = X509CertificateLoader.LoadCertificate(certBytes);

        //Dodanie klucza prywatnego do certyfikatu
        using RSA rsa = RSA.Create();
        byte[] privateKeyBytes = Convert.FromBase64String(Fixture.PrivateKey);
        rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
        X509Certificate2 certWithKey = cert.CopyWithPrivateKey(rsa);

        //Utworzenie kodu QR do weryfikacji certyfikatu (KOD II) dla trybu offline
        var qrOfflineCertificate = QRCodeService.GenerateQrCode(
            VerificationLinkService.BuildCertificateVerificationUrl(
                Fixture.Nip,
                Core.Models.QRCode.ContextIdentifierType.Nip,
                Fixture.Nip,
                Fixture.Certificate.CertificateSerialNumber,
                Fixture.InvoiceHash,
                certWithKey));

        //Dodanie etykiety CERTYFIKAT
        qrOfflineCertificate = QRCodeService.AddLabelToQrCode(qrOfflineCertificate, "CERTYFIKAT");

        Assert.NotEmpty(qrOfflineCertificate);
    }
}