using System.Text;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeFClient;

namespace KSeF.Client.Tests.Utils;
public static class OnlineSessionUtils
{
    public static async Task<OpenOnlineSessionResponse> OpenOnlineSessionAsync(IKSeFClient ksefClient,
        EncryptionData encryptionData,
        string accessToken)
    {
        var openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
      .Create()
      .WithFormCode(systemCode: "FA (2)", schemaVersion: "1-0E", value: "FA")
      .WithEncryption(
          encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
          initializationVector: encryptionData.EncryptionInfo.InitializationVector)
      .Build();

        return await ksefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, accessToken);
    }

    public static async Task<SendInvoiceResponse> SendInvoiceAsync(IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string accessToken,
        string nip,
        EncryptionData encryptionData,
        ICryptographyService cryptographyService)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "invoices", "faktura-online.xml");
        var xml = File.ReadAllText(path, Encoding.UTF8);
        xml = xml.Replace("{{TEST_NIP}}", nip);
        xml = xml.Replace("{{SEED_TEST_NIP_MONTH_YEAR}}", $"{Guid.NewGuid().ToString()}");
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var invoice = memoryStream.ToArray();

        var encryptedInvoice = cryptographyService.EncryptBytesWithAES256(invoice, encryptionData.CipherKey, encryptionData.CipherIv);
        var invoiceMetadata = cryptographyService.GetMetaData(invoice);
        var encryptedInvoiceMetadata = cryptographyService.GetMetaData(encryptedInvoice);

        var sendOnlineInvoiceRequest = SendInvoiceOnlineSessionRequestBuilder
            .Create()
            .WithInvoiceHash(invoiceMetadata.HashSHA, invoiceMetadata.FileSize)
            .WithEncryptedDocumentHash(
               encryptedInvoiceMetadata.HashSHA, encryptedInvoiceMetadata.FileSize)
            .WithEncryptedDocumentContent(Convert.ToBase64String(encryptedInvoice))
            .Build();

        return await ksefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, sessionReferenceNumber, accessToken);
    }

    public static async Task<SessionStatusResponse> GetOnlineSessionStatusAsync(
        IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string accessToken,
        int sleepTime = 1000,
        int maxAttempts = 10)
    {
        SessionStatusResponse statusResponse = null;
        int attempt = 0;

        do
        {
            statusResponse = await ksefClient.GetSessionStatusAsync(sessionReferenceNumber, accessToken);

            if (statusResponse.Status is not null && statusResponse.Status.Code > 0)
            {
                return statusResponse; // Session is ready
            }

            if (attempt >= maxAttempts)
            {
                break;
            }

            attempt++;
            await Task.Delay(sleepTime); // Wait for the status to update
        } while (statusResponse.Status is null);

        return statusResponse;
    }

    public static async Task CloseOnlineSessionAsync(IKSeFClient kSeFClient, string referenceNumber, string accessToken)
    {
        await kSeFClient.CloseOnlineSessionAsync(referenceNumber, accessToken);
    }

    public static async Task<SessionInvoicesResponse> GetSessionInvoicesMetadataAsync(IKSeFClient kSeFClient, string sessionReferenceNumber, string accessToken)
    {
        return await kSeFClient.GetSessionInvoicesAsync(sessionReferenceNumber, accessToken);
    }

    public static async Task<string> GetSessionInvoiceUpoAsync(IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string ksefInvoiceNumber,
        string accessToken)
    {
        var upoResponse = await ksefClient.GetSessionInvoiceUpoByKsefNumberAsync(sessionReferenceNumber, ksefInvoiceNumber, accessToken, CancellationToken.None);
        return upoResponse;
    }

    public static async Task<string> GetSessionUpoAsync(IKSeFClient ksefClient,
        string sessionReferenceNumber,
        string upoReferenceNumber,
        string accessToken)
    {
        var upoResponse = await ksefClient.GetSessionUpoAsync(sessionReferenceNumber, upoReferenceNumber, accessToken, CancellationToken.None);
        return upoResponse;
    }
}