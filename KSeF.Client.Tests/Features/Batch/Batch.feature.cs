using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features;

[CollectionDefinition("Batch.feature")]
[Trait("Category", "Features")]
[Trait("Features", "batch.feature")]
public class BatchTests : KsefIntegrationTestBase
{
    private const int DefaultInvoiceCount = 5;

    [Theory]
    [InlineData(SystemCodeEnum.FA2, "invoice-template-fa-2.xml")]
    [InlineData(SystemCodeEnum.FA3, "invoice-template-fa-3.xml")]
    [Trait("Scenario", "Wysłanie dokumentów w jednoczęściowej paczce (happy path)")]
    public async Task Batch_SendSinglePart_ShouldSucceed(SystemCodeEnum systemCode, string invoiceTemplatePath)
    {
        // Arrange
        var nip = MiscellaneousUtils.GetRandomNip();
        var accessToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, nip)).AccessToken.Token;

        var invoices = BatchUtils.GenerateInvoicesInMemory(DefaultInvoiceCount, nip, invoiceTemplatePath);

        var (zipBytes, zipMeta) = BatchUtils.BuildZip(invoices, CryptographyService);
        var encryption = CryptographyService.GetEncryptionData();
        var parts = BatchUtils.EncryptAndSplit(zipBytes, encryption, CryptographyService, partCount: 1);

        // Act
        var openBatchRequest = BatchUtils.BuildOpenBatchRequest(zipMeta, encryption, parts, systemCode);
        var openBatchResponse = await BatchUtils.OpenBatchAsync(KsefClient, openBatchRequest, accessToken);
        await BatchUtils.SendBatchPartsAsync(KsefClient, openBatchResponse, parts);
        await BatchUtils.CloseBatchAsync(KsefClient, openBatchResponse.ReferenceNumber, accessToken);

        // Assert
        var status = await BatchUtils.WaitForBatchStatusAsync(KsefClient, openBatchResponse.ReferenceNumber, accessToken);
        Assert.True(status.Status.Code != 150);
        Assert.Equal(DefaultInvoiceCount, status.SuccessfulInvoiceCount);

        var docs = await BatchUtils.GetSessionInvoicesAsync(KsefClient, openBatchResponse.ReferenceNumber, accessToken);
        Assert.NotNull(docs);
        Assert.NotEmpty(docs.Invoices);

        var firstInvoice = docs.Invoices.First();
        var upo = await BatchUtils.GetSessionInvoiceUpoByKsefNumberAsync(
            KsefClient, openBatchResponse.ReferenceNumber, firstInvoice.KsefNumber, accessToken);

        Assert.NotNull(upo);
    }

    [Theory]
    [InlineData(SystemCodeEnum.FA2, "invoice-template-fa-2.xml")]
    [InlineData(SystemCodeEnum.FA3, "invoice-template-fa-3.xml")]
    [Trait("Scenario", "Wysłanie dokumentów w jednoczęściowej paczce z niepoprawnym NIP w fakturach (negatywny)")]
    public async Task Batch_SendWithIncorrectNip_ShouldFail(SystemCodeEnum systemCode, string invoiceTemplatePath)
    {
        // Arrange
        var nip = MiscellaneousUtils.GetRandomNip();
        var invalidNip = MiscellaneousUtils.GetRandomNip();
        var accessToken = (await AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService, nip)).AccessToken.Token;
        
        var invalidInvoices = BatchUtils.GenerateInvoicesInMemory(DefaultInvoiceCount, invalidNip, invoiceTemplatePath);
        var (zipBytes, zipMeta) = BatchUtils.BuildZip(invalidInvoices, CryptographyService);
        var encryption = CryptographyService.GetEncryptionData();
        var parts = BatchUtils.EncryptAndSplit(zipBytes, encryption, CryptographyService, partCount: 1);

        // Act
        var openBatchRequest = BatchUtils.BuildOpenBatchRequest(zipMeta, encryption, parts, systemCode);
        var openBatchResponse = await BatchUtils.OpenBatchAsync(KsefClient, openBatchRequest, accessToken);
        await BatchUtils.SendBatchPartsAsync(KsefClient, openBatchResponse, parts);
        await BatchUtils.CloseBatchAsync(KsefClient, openBatchResponse.ReferenceNumber, accessToken);

        // Assert
        var status = await BatchUtils.WaitForBatchStatusAsync(KsefClient, openBatchResponse.ReferenceNumber, accessToken);
        Assert.True(status.Status.Code == 445);
        Assert.Equal(DefaultInvoiceCount, status.FailedInvoiceCount);
    }
}
