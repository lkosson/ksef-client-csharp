using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;
using System.Text;

namespace KSeF.Client.Tests.Core.E2E.DuplicateInvoice;

[Collection("DuplicateInvoiceScenario")]
public class DuplicateInvoiceE2ETests : TestBase
{
    // Stałe domenowe i konfiguracyjne testu
    private const int BatchTotalInvoices = 1;
    private const int PartQuantity = 1;

    private const int SuccessfulSessionStatusCode = 200;
    private const int ProcessingSessionStatusCode = 150;
    private const int DuplicateInvoiceDomainCode = 21405; // Kod duplikatu (opcjonalny)

    private const int ExpectedFailedInvoiceCount = 0;

    private const int BatchProcessingPollDelayMs = 1000;
    private const int BatchProcessingMaxAttempts = 30;

    private const int OnlineSessionClosePollDelaySeconds = 1;
    private const int OnlineSessionCloseMaxAttempts = 30;

    private const int OnlineSessionStatusPollDelaySeconds = 1;
    private const int OnlineSessionStatusMaxAttempts = 60;

    private string accessToken = string.Empty;
    private string sellerNip = string.Empty;

    public DuplicateInvoiceE2ETests()
    {
        string nip = MiscellaneousUtils.GetRandomNip();
        AuthenticationOperationStatusResponse authInfo = AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient, SignatureService, nip)
            .GetAwaiter().GetResult();

        accessToken = authInfo.AccessToken.Token;
        sellerNip = nip;
    }

    /// <summary>
    /// Scenariusz duplikatu faktury (bez polegania na wyjątku HTTP):
    /// 1. Wysyła fakturę w sesji wsadowej (batch) – przetworzona jako poprawna.
    /// 2. Otwiera sesję interaktywną (online).
    /// 3. Wysyła tę samą fakturę (ten sam numer / ta sama treść logiczna).
    /// 4. Zamyka sesję online.
    /// 5. Pobiera listę nieudanych faktur: GET /sessions/{ref}/invoices/failed.
    /// 6. Weryfikuje, że status/komunikat zawiera informację o duplikacie (kod domenowy 21405 lub tekst "duplikat").
    /// </summary>
    //[Theory]
    //[InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
    //[InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    public async Task DuplicateInvoice_EndToEnd_FailedListContainsDuplicate(SystemCode systemCode, string invoiceTemplatePath)
    {
        // Dane szyfrowania
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        // Ustalony numer faktury użyty w obu ścieżkach
        string sharedInvoiceNumber = Guid.NewGuid().ToString();

        // 1. Batch: wysłanie i przetworzenie faktury
        string batchSessionRef = await SendInvoiceInBatchSessionAsync(systemCode, invoiceTemplatePath, sharedInvoiceNumber, encryptionData);

        SessionStatusResponse batchStatus = await BatchUtils.WaitForBatchStatusAsync(
            KsefClient,
            batchSessionRef,
            accessToken,
            sleepTime: BatchProcessingPollDelayMs,
            maxAttempts: BatchProcessingMaxAttempts);

        Assert.NotNull(batchStatus);
        Assert.Equal(SuccessfulSessionStatusCode, batchStatus.Status.Code);
        Assert.Equal(BatchTotalInvoices, batchStatus.SuccessfulInvoiceCount);
        Assert.Equal(ExpectedFailedInvoiceCount, batchStatus.FailedInvoiceCount ?? 0);

        await Task.Delay(SleepTime); 

        // 2. Online: otwarcie sesji
        OpenOnlineSessionResponse onlineSession = await OnlineSessionUtils.OpenOnlineSessionAsync(
            KsefClient,
            encryptionData,
            accessToken,
            systemCode);
        Assert.False(string.IsNullOrWhiteSpace(onlineSession.ReferenceNumber));

        await Task.Delay(SleepTime);

        // 3. Online: wysłanie identycznej faktury (ten sam numer)
        string duplicateXml = DuplicateInvoiceE2ETests.GetInvoiceXml(invoiceTemplatePath, sellerNip, sharedInvoiceNumber);
        SendInvoiceResponse onlineSendResp = await OnlineSessionUtils.SendInvoiceFromXmlAsync(
            KsefClient,
            onlineSession.ReferenceNumber,
            accessToken,
            duplicateXml,
            encryptionData,
            CryptographyService);
        Assert.False(string.IsNullOrWhiteSpace(onlineSendResp.ReferenceNumber));

        // 4. Zamknięcie sesji online (poll aby mieć pewność, że zamknięta pomimo możliwych opóźnień)
        await AsyncPollingUtils.PollAsync(
            action: async () =>
            {
                await KsefClient.CloseOnlineSessionAsync(onlineSession.ReferenceNumber, accessToken);
                return true;
            },
            condition: closed => closed,
            delay: TimeSpan.FromSeconds(OnlineSessionClosePollDelaySeconds),
            maxAttempts: OnlineSessionCloseMaxAttempts,
            shouldRetryOnException: _ => true,
            cancellationToken: CancellationToken);

        // (Opcjonalnie) odczekanie aż status sesji nie będzie w trakcie przetwarzania (kod 150) lub pojawi się FailedInvoiceCount
        SessionStatusResponse onlineFinalStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.GetSessionStatusAsync(onlineSession.ReferenceNumber, accessToken),
            condition: s => s.Status.Code != ProcessingSessionStatusCode || (s.FailedInvoiceCount ?? 0) > 0,
            delay: TimeSpan.FromSeconds(OnlineSessionStatusPollDelaySeconds),
            maxAttempts: OnlineSessionStatusMaxAttempts,
            cancellationToken: CancellationToken);

        // 5. Pobranie nieudanych faktur /invoices/failed
        SessionInvoicesResponse failedInvoices = await KsefClient.GetSessionFailedInvoicesAsync(
            onlineSession.ReferenceNumber,
            accessToken,
            null,
            continuationToken: null,
            CancellationToken);

        Assert.NotNull(failedInvoices);
        Assert.NotNull(failedInvoices.Invoices);
        Assert.NotEmpty(failedInvoices.Invoices);

        SessionInvoice failed = failedInvoices.Invoices.First();
        Assert.NotNull(failed.Status);

        bool hasDuplicateText =
            (failed.Status.Description?.Contains("duplikat", StringComparison.OrdinalIgnoreCase) ?? false) ||
            (failed.Status.Details?.Any(d => d.Contains("duplikat", StringComparison.OrdinalIgnoreCase)) ?? false);

        bool indicatesDuplicate = (failed.Status.Code == DuplicateInvoiceDomainCode) || hasDuplicateText;

        Assert.True(indicatesDuplicate, "Brak informacji o duplikacie w statusie nieudanej faktury (Description/Details lub kod domenowy)." );
    }

    private async Task<string> SendInvoiceInBatchSessionAsync(
        SystemCode systemCode,
        string invoiceTemplatePath,
        string invoiceNumber,
        EncryptionData encryptionData)
    {
        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            count: BatchTotalInvoices,
            nip: sellerNip,
            templatePath: invoiceTemplatePath,
            invoiceNumberFactory: () => invoiceNumber);

        (byte[] zipBytes, FileMetadata zipMeta) = BatchUtils.BuildZip(invoices, CryptographyService);
        List<BatchPartSendingInfo> encryptedParts = BatchUtils.EncryptAndSplit(zipBytes, encryptionData, CryptographyService, PartQuantity);
        OpenBatchSessionRequest openBatchRequest = BatchUtils.BuildOpenBatchRequest(zipMeta, encryptionData, encryptedParts, systemCode);
        OpenBatchSessionResponse openBatchResponse = await BatchUtils.OpenBatchAsync(KsefClient, openBatchRequest, accessToken);
        await KsefClient.SendBatchPartsAsync(openBatchResponse, encryptedParts);

        await BatchUtils.CloseBatchAsync(KsefClient, openBatchResponse.ReferenceNumber, accessToken);
        return openBatchResponse.ReferenceNumber;
    }

    /// <summary>
    /// Buduje XML faktury na podstawie szablonu z podmianą NIP i numeru faktury.
    /// </summary>
    private static string GetInvoiceXml(string templatePath, string nip, string invoiceNumber)
    {
        string invoiceFilePath = Path.Combine(AppContext.BaseDirectory, "Templates", templatePath);
        if (!File.Exists(invoiceFilePath))
        {
            throw new FileNotFoundException($"Szablon nie został znaleziony: {invoiceFilePath}");
        }

        string xml = File.ReadAllText(invoiceFilePath, Encoding.UTF8)
            .Replace("#nip#", nip)
            .Replace("#invoice_number#", invoiceNumber);
        return xml;
    }
}
