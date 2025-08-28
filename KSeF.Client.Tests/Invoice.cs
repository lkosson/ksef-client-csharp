using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Tests.Utils;
using KSeFClient.Core.Exceptions;

namespace KSeF.Client.Tests;

public class InvoicesScenarioFixture
{
    public string? ReferenceNumber { get; set; }
    public string? InvoiceReferenceNumber { get; set; }
    public string? UpoDocument { get; set; }
    public string? UpoSession { get; set; }
    public string? AccessToken { get; set; }
    public string? UpoReferenceNumber { get; set; }

    public string? KsefNumber { get; set; }    
}

[CollectionDefinition("InvoicesScenario")]
public class InvoicesScenarioCollection : ICollectionFixture<InvoicesScenarioFixture> { }

[Collection("InvoicesScenario")]
public class Invoice : TestBase
{
    private readonly InvoicesScenarioFixture _fixture;

    public Invoice(InvoicesScenarioFixture fixture)
    {
        _fixture = fixture;
        var authInfo = AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService).GetAwaiter().GetResult();
        _fixture.AccessToken = authInfo.AccessToken.Token;
    }

   // [Fact] //TODO
    public async Task Invoice_QueryAsync_ReturnsNull() 
    {
        // Start query session
        var filters = new AsyncQueryInvoiceRequest
        {
            Encryption = new EncryptionInfo
            {
                EncryptedSymmetricKey = "encrypted-key",
                InitializationVector = "initialization-vector"
            },
            SubjectType = SubjectType.Subject1,
            DateRange = new DateRange
            {
                From = DateTime.UtcNow.AddDays(-30),
                To = DateTime.UtcNow,
                DateType = DateType.Issue
            },
        };
        var request = await ksefClient.AsyncQueryInvoicesAsync(filters, _fixture.AccessToken, CancellationToken.None);
        await Task.Delay(2000);

        AsyncQueryInvoiceStatusResponse? result = null;
        do
        {
            try
            {
                result = await ksefClient.GetAsyncQueryInvoicesStatusAsync
                    (
                        request.OperationReferenceNumber,
                        _fixture.AccessToken,
                        CancellationToken.None
                    );
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }

        } while (result?.Status?.Code == 100);

        // use referenceNumber to check query status


        Assert.Null(result);
        Assert.NotNull(request.OperationReferenceNumber);
    }

    [Fact]
    public async Task Invoice_GetInvoioceMetadataAsyncWithInvalidPageSize_ThrowsExceptionPageSizeIsNotInCorrectRange()
    {
        var invoiceMetadataQueryRequest = new InvoiceMetadataQueryRequest
        {
            SubjectType = SubjectType.Subject1,
            DateRange = new DateRange
            {
                From = DateTime.UtcNow.AddDays(-30),
                To = DateTime.UtcNow,
                DateType = DateType.Issue
            }
        };

        await Assert.ThrowsAnyAsync<KsefApiException>(() => ksefClient.QueryInvoiceMetadataAsync(
            requestPayload: invoiceMetadataQueryRequest,
            accessToken: _fixture.AccessToken,
            cancellationToken: CancellationToken.None,
            pageOffset: 0,
            pageSize: 5)); // <====== pageSize must be between 10 and 100
    }

    [Fact]
    public async Task Invoice_GetInvoiceMetadataAsync_ReturnsMetadata()
    {
        var invoiceMetadataQueryRequest = new InvoiceMetadataQueryRequest
        {
            SubjectType = SubjectType.Subject1,
            DateRange = new DateRange
            {
                From = DateTime.UtcNow.AddDays(-30),
                To = DateTime.UtcNow,
                DateType = DateType.Issue
            }
        };

        var metadata = await ksefClient.QueryInvoiceMetadataAsync(
            requestPayload: invoiceMetadataQueryRequest,
            accessToken: _fixture.AccessToken,
            cancellationToken: CancellationToken.None,
            pageOffset: 0,
            pageSize: 10);

        Assert.NotNull(metadata);
    }
}
