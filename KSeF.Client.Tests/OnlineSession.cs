using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Tests.Utils;
using System.Text;

namespace KSeF.Client.Tests;

public class OnlineSessionScenarioFixture
{
    public string? NIP { get; set; }

    public string? ReferenceNumber { get; set; }
    public string? InvoiceReferenceNumber { get; set; }
    public string? UpoDocument { get; set; }
    public string? UpoSession { get; set; }
    public string? AccessToken { get; set; }
    public string? UpoReferenceNumber { get; set; }

    public string? KsefNumber { get; set; }
}

[CollectionDefinition("OnlineSessionScenario")]
public class OnlineSessionScenarioCollection : ICollectionFixture<OnlineSessionScenarioFixture> { }

[Collection("OnlineSessionScenario")]
public class OnlineSession : TestBase
{
    private readonly OnlineSessionScenarioFixture _fixture;

    public OnlineSession(OnlineSessionScenarioFixture fixture)
    {
        _fixture = fixture;
        _fixture.NIP = MiscellaneousUtils.GetRandomNip();
        var authInfo = AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService, _fixture.NIP).GetAwaiter().GetResult();
        _fixture.AccessToken = authInfo.AccessToken.Token;
    }

    [Fact]
    public async Task SendInvoiceInOnlineSession_E2E_ShouldReturnUpo()
    {

        // authenticated in constructor
        Assert.NotNull(_fixture.AccessToken);
        
        // proceed with invoice online session
        var cryptographyService = new CryptographyService(ksefClient) as ICryptographyService;
        var encryptionData = cryptographyService.GetEncryptionData();

        var openSessionRequest = await OnlineSessionUtils.OpenOnlineSessionAsync(ksefClient,
          encryptionData,
          _fixture.AccessToken);
        Assert.NotNull(openSessionRequest);
        Assert.NotNull(openSessionRequest.ReferenceNumber);

        var sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            _fixture.AccessToken,
            _fixture.NIP,
            encryptionData,
            cryptographyService);
        Assert.NotNull(sendInvoiceResponse);
        Assert.NotNull(sendInvoiceResponse.ReferenceNumber);

        var sendedSessionStatus = await OnlineSessionUtils.GetOnlineSessionStatusAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            _fixture.AccessToken);
        Assert.NotNull(sendedSessionStatus);
        Assert.True(sendedSessionStatus.Status.Code > 0);
        Assert.NotNull(sendedSessionStatus.InvoiceCount);

        await OnlineSessionUtils.CloseOnlineSessionAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            _fixture.AccessToken);


        do
        {
            await Task.Delay(3000);
            sendedSessionStatus = await OnlineSessionUtils.GetOnlineSessionStatusAsync(ksefClient,
                    openSessionRequest.ReferenceNumber,
                    _fixture.AccessToken);
        } while (sendedSessionStatus.Status.Code == 170);

        Assert.False(sendedSessionStatus.Status.Code == 445);

        var metadata = await OnlineSessionUtils.GetSessionInvoicesMetadataAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            _fixture.AccessToken);
        Assert.NotNull(metadata);
        Assert.NotEmpty(metadata.Invoices);

        foreach (var item in metadata.Invoices)
        {
            var invoiceMetadata = await OnlineSessionUtils.GetSessionInvoiceUpoAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            item.KsefNumber,
            _fixture.AccessToken);
            Assert.NotNull(invoiceMetadata);
        }
    }

    [Fact]
    public async Task OnlineSession_E2E_WorksCorrectly()
    {
        var cryptographyService = new CryptographyService(ksefClient) as ICryptographyService;
        var encryptionData = cryptographyService.GetEncryptionData();
        // Step 1: Open session
        await Step1_OpenOnlineSession_ReturnsReference(encryptionData);
        await Task.Delay(sleepTime); 
        // Step 2: Send invoice
        await Step2_SendInvoiceOnlineSessionAsync_ReturnsReferenceNumber(encryptionData, cryptographyService);
        Thread.Sleep(1000);
        // Step 3: Check status        
        await Step3_GetOnlineSessionStatusByAsync_ReturnsStatus();        
        await Task.Delay(sleepTime);
        // Step 4: Close session
        await Step4_CloseOnlineSessionAsync_ClosesSessionSuccessfully();
        Thread.Sleep(2000);
        // Step 5: Get documents
        await Step5_GetOnlineSessionDocumentsAync_ReturnsDocuments();
        // Step 6: Get status after  close
        await Step6_GetOnlineSessionStatusByAsync_ReturnsStatus();
        // Step 7: Get UPO
        await Step7_GetOnlineSessionInvoiceUpoAsync_ReturnsUpo();
        // Step 8: Get session UPO
        await Step8_GetOnlineSessionUpoAsync_ReturnsSessionUpo();
    }

    
    private async Task Step1_OpenOnlineSession_ReturnsReference(EncryptionData encryptionData)
    {
        var openOnlineSessionRequest = OpenOnlineSessionRequestBuilder
       .Create()
       .WithFormCode(systemCode: "FA (2)", schemaVersion: "1-0E", value: "FA")
       .WithEncryption(
           encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
           initializationVector: encryptionData.EncryptionInfo.InitializationVector)
       .Build();

        var openOnlineSessionResponse = await ksefClient.OpenOnlineSessionAsync(openOnlineSessionRequest, _fixture.AccessToken);

        Assert.NotNull(openOnlineSessionResponse);
        Assert.NotNull(openOnlineSessionResponse.ReferenceNumber);
        _fixture.ReferenceNumber = openOnlineSessionResponse.ReferenceNumber;
    }
        
    private async Task Step2_SendInvoiceOnlineSessionAsync_ReturnsReferenceNumber(EncryptionData encryptionData , ICryptographyService cryptographyService)
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));

        
        //numer faktury unikalny rosnący
        //nip podmienić w fakturze
        var path = Path.Combine(AppContext.BaseDirectory, "invoices", "faktura-online.xml");
        var xml = File.ReadAllText(path, Encoding.UTF8);
        xml= xml.Replace("{{TEST_NIP}}",_fixture.NIP);
        xml = xml.Replace("{{SEED_TEST_NIP_MONTH_YEAR}}",$"{Guid.NewGuid().ToString()}");
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

        var sendInvoiceResponse = await ksefClient.SendOnlineSessionInvoiceAsync(sendOnlineInvoiceRequest, _fixture.ReferenceNumber, _fixture.AccessToken);
                
        Assert.NotNull(sendInvoiceResponse);
        Assert.False(string.IsNullOrWhiteSpace(sendInvoiceResponse.ReferenceNumber));
        _fixture.InvoiceReferenceNumber = sendInvoiceResponse.ReferenceNumber;
    }

    private async Task Step3_GetOnlineSessionStatusByAsync_ReturnsStatus()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));
        SessionStatusResponse statusResponse = null;
        do {
            statusResponse = await ksefClient.GetSessionStatusAsync(_fixture.ReferenceNumber, _fixture.AccessToken);
            await Task.Delay(sleepTime); // Wait for the status to update
        } while (statusResponse.SuccessfulInvoiceCount is null); 
        Assert.NotNull(statusResponse);

        //if ok GetSessionInvoicesAsync
        // if failed GetSessionFailedInvoicesAsync
        Assert.True(statusResponse.SuccessfulInvoiceCount is not null);
        Assert.True(statusResponse.SuccessfulInvoiceCount == 1);
        Assert.True(statusResponse.FailedInvoiceCount is null);
        Assert.Null(statusResponse.Upo);
        //sesja otwarta
        Assert.True(statusResponse.Status.Code == 100);
    }

    private async Task Step7_GetOnlineSessionInvoiceUpoAsync_ReturnsUpo()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));

        var upoResponse = await ksefClient.GetSessionInvoiceUpoByKsefNumberAsync(_fixture.ReferenceNumber, _fixture.KsefNumber ,_fixture.AccessToken, CancellationToken.None);

        Assert.NotNull(upoResponse);
        Assert.False(string.IsNullOrWhiteSpace(upoResponse));
        _fixture.UpoDocument = upoResponse;
    }

    private async Task Step4_CloseOnlineSessionAsync_ClosesSessionSuccessfully()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber)); // Wymuś wykonie kroku 1

        await ksefClient.CloseOnlineSessionAsync(_fixture.ReferenceNumber, _fixture.AccessToken);
    }


    private async Task Step6_GetOnlineSessionStatusByAsync_ReturnsStatus()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));

        var statusResponse = await ksefClient.GetSessionStatusAsync(_fixture.ReferenceNumber, _fixture.AccessToken);

        Assert.NotNull(statusResponse);
        _fixture.UpoReferenceNumber = statusResponse.Upo.Pages.First().ReferenceNumber;
        Assert.True(statusResponse.Status.Code == 200);
    }

    private async Task Step5_GetOnlineSessionDocumentsAync_ReturnsDocuments()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));
        var documents = await ksefClient.GetSessionInvoicesAsync(_fixture.ReferenceNumber, _fixture.AccessToken);
        Assert.NotNull(documents);
        Assert.NotEmpty(documents.Invoices);
        Assert.Single(documents.Invoices);

        _fixture.KsefNumber = documents.Invoices.First().KsefNumber;
    }
    private async Task Step8_GetOnlineSessionUpoAsync_ReturnsSessionUpo()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fixture.ReferenceNumber));
        var upoResponse = await ksefClient.GetSessionUpoAsync(_fixture.ReferenceNumber, _fixture.UpoReferenceNumber, _fixture.AccessToken, CancellationToken.None);
        Assert.NotNull(upoResponse);
        Assert.False(string.IsNullOrWhiteSpace(upoResponse));
        _fixture.UpoSession = upoResponse;
    }

}
