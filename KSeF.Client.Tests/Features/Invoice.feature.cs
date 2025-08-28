using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features;


[CollectionDefinition("Invoice.feature")]
[Trait("Category", "Features")]
[Trait("Features", "Invoice.feature")]
public class InvoiceTests : TestBase
{
    private string authToken { get; set; }
    private string nip { get; set; }


    public InvoiceTests()
    {
        nip = MiscellaneousUtils.GetRandomNip();
        var authInfo = AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService,nip).GetAwaiter().GetResult();
        authToken = authInfo.AccessToken.Token;
    }

    [Fact]
    [Trait("Scenario", "Posiadając upranienie właścicielskie wysyłamy fakturę")]
    [Trait("Scenario", "Posiadając uprawnienie właścicielskie pytamy o fakturę wysłaną")]
    public async Task GivenNewInvoice_SendedToKsef_ThenReturnsNewKsefNumber()
    {
        // authenticated in constructor
        Assert.NotNull(authToken);

        // proceed with invoice online session
        var cryptographyService = new CryptographyService(ksefClient) as ICryptographyService;
        var encryptionData = cryptographyService.GetEncryptionData();

        var openSessionRequest = await OnlineSessionUtils.OpenOnlineSessionAsync(ksefClient,
            encryptionData,
            authToken);
        Assert.NotNull(openSessionRequest);
        Assert.NotNull(openSessionRequest.ReferenceNumber);

        var sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            authToken,
            nip,
            encryptionData,
            cryptographyService);
        Assert.NotNull(sendInvoiceResponse);
        Assert.NotNull(sendInvoiceResponse.ReferenceNumber);

        var sendedSessionStatus = await OnlineSessionUtils.GetOnlineSessionStatusAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            authToken);
        Assert.NotNull(sendedSessionStatus);
        Assert.True(sendedSessionStatus.Status.Code > 0); // ???
        Assert.NotNull(sendedSessionStatus.InvoiceCount);

        await OnlineSessionUtils.CloseOnlineSessionAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            authToken);

        sendedSessionStatus = await OnlineSessionUtils.GetOnlineSessionStatusAsync(ksefClient,
                    openSessionRequest.ReferenceNumber,
                    authToken);
        Assert.NotNull(sendedSessionStatus);
        Assert.True(sendedSessionStatus.Status.Code == 170);

        do
        {
            await Task.Delay(3000);
            sendedSessionStatus = await OnlineSessionUtils.GetOnlineSessionStatusAsync(ksefClient,
                    openSessionRequest.ReferenceNumber,
                    authToken);
        } while (sendedSessionStatus.Status.Code == 170);


        var metadata = await OnlineSessionUtils.GetSessionInvoicesMetadataAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            authToken);
        Assert.NotNull(metadata);
        Assert.NotEmpty(metadata.Invoices);

        foreach (var item in metadata.Invoices)
        {
            var invoiceMetadata = await OnlineSessionUtils.GetSessionInvoiceUpoAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            item.KsefNumber,
            authToken);
            Assert.NotNull(invoiceMetadata);

            await Task.Delay(60000);
            
            var invoice = await ksefClient.GetInvoiceAsync(item.KsefNumber, authToken);
            Assert.NotNull(invoice);
        }
    }


    [Fact]
    [Trait("Scenario", "Posiadając upranienie właścicielskie wysyłamy szyfrowaną fakturę z nieprawidłowym numerem NIP sprzedawcy")]
    public async Task GivenUnvalidNewInvoice_SendedToKsef_ThenReturnsErrorUnvalidKsefNumber()
    {
        var wrongNIP = "22243";
        // authenticated in constructor
        Assert.NotNull(authToken);

        // proceed with invoice online session
        var cryptographyService = new CryptographyService(ksefClient) as ICryptographyService;
        var encryptionData = cryptographyService.GetEncryptionData();

        var openSessionRequest = await OnlineSessionUtils.OpenOnlineSessionAsync(ksefClient,
            encryptionData,
            authToken);
        Assert.NotNull(openSessionRequest);
        Assert.NotNull(openSessionRequest.ReferenceNumber);

        var sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            authToken,
            wrongNIP,
            encryptionData,
            cryptographyService);
        Assert.NotNull(sendInvoiceResponse);
        Assert.NotNull(sendInvoiceResponse.ReferenceNumber);

        var sendInvoiceStatus = await OnlineSessionUtils.GetOnlineSessionStatusAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            authToken);
        Assert.NotNull(sendInvoiceStatus);
        Assert.True(sendInvoiceStatus.Status.Code > 0); // ???
        Assert.NotNull(sendInvoiceStatus.InvoiceCount);

        await OnlineSessionUtils.CloseOnlineSessionAsync(ksefClient,
            openSessionRequest.ReferenceNumber,
            authToken);

        sendInvoiceStatus = await OnlineSessionUtils.GetOnlineSessionStatusAsync(ksefClient,
                    openSessionRequest.ReferenceNumber,
                    authToken);
        Assert.NotNull(sendInvoiceStatus);
        Assert.True(sendInvoiceStatus.Status.Code == 170);

        do
        {
            await Task.Delay(3000);
            sendInvoiceStatus = await OnlineSessionUtils.GetOnlineSessionStatusAsync(ksefClient,
                    openSessionRequest.ReferenceNumber,
                    authToken);
        } while (sendInvoiceStatus.Status.Code == 170);


        Assert.NotNull(sendInvoiceStatus);
        Assert.True(sendInvoiceStatus.Status.Code == 445);  // CODE 445 Verification error, no valid invoices available  
    }
}