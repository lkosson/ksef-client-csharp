using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features
{
    [CollectionDefinition("Batch.feature")]
    [Trait("Category", "Features")]
    [Trait("Features", "batch.feature")]
    public class BatchTests : TestBase
    {
        private const int DefaultInvoiceCount = 5;

        [Theory]
        [InlineData("FA (2)", "Invoices/faktura-template.xml")]
        //[InlineData("FA (3)", "")]
        [Trait("Scenario", "Wysłanie dokumentów w jednoczęściowej paczce (happy path)")]
        public async Task Batch_SendSinglePart_ShouldSucceed(string systemCode, string invoiceTemplatePath)
        {
            // Arrange
            var nip = MiscellaneousUtils.GetRandomNip();
            var accessToken = (await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService, nip)).AccessToken.Token;
            var crypto = new CryptographyService(ksefClient) as ICryptographyService;

            var invoices = BatchUtils.GenerateInvoicesInMemory(DefaultInvoiceCount, nip, invoiceTemplatePath);

            var (zipBytes, zipMeta) = BatchUtils.BuildZip(invoices, crypto);
            var encryption = crypto.GetEncryptionData();
            var parts = BatchUtils.EncryptAndSplit(zipBytes, encryption, crypto, partCount: 1);

            // Act
            var openBatchRequest = BatchUtils.BuildOpenBatchRequest(zipMeta, encryption, parts, systemCode);
            var openBatchResponse = await BatchUtils.OpenBatchAsync(ksefClient, openBatchRequest, accessToken);
            await BatchUtils.SendBatchPartsAsync(ksefClient, openBatchResponse, parts);
            await BatchUtils.CloseBatchAsync(ksefClient, openBatchResponse.ReferenceNumber, accessToken);

            // Assert
            var status = await BatchUtils.WaitForBatchStatusAsync(ksefClient, openBatchResponse.ReferenceNumber, accessToken);
            Assert.True(status.Status.Code != 150);
            Assert.Equal(DefaultInvoiceCount, status.SuccessfulInvoiceCount);

            var docs = await BatchUtils.GetSessionInvoicesAsync(ksefClient, openBatchResponse.ReferenceNumber, accessToken);
            Assert.NotNull(docs);
            Assert.NotEmpty(docs.Invoices);

            var firstInvoice = docs.Invoices.First();
            var upo = await BatchUtils.GetSessionInvoiceUpoByKsefNumberAsync(
                ksefClient, openBatchResponse.ReferenceNumber, firstInvoice.KsefNumber, accessToken);

            Assert.NotNull(upo);
        }

        [Theory]
        [InlineData("FA (2)", "Invoices/faktura-template.xml")]
        //[InlineData("FA (3)", "")]
        [Trait("Scenario", "Wysłanie dokumentów w jednoczęściowej paczce z niepoprawnym NIP w fakturach (negatywny)")]
        public async Task Batch_SendWithIncorrectNip_ShouldFail(string systemCode, string invoiceTemplatePath)
        {
            // Arrange
            var nip = MiscellaneousUtils.GetRandomNip();
            var invalidNip = MiscellaneousUtils.GetRandomNip();
            var accessToken = (await AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService, nip)).AccessToken.Token;
            var crypto = new CryptographyService(ksefClient) as ICryptographyService;

            var invalidInvoices = BatchUtils.GenerateInvoicesInMemory(DefaultInvoiceCount, invalidNip, invoiceTemplatePath);
            var (zipBytes, zipMeta) = BatchUtils.BuildZip(invalidInvoices, crypto);
            var encryption = crypto.GetEncryptionData();
            var parts = BatchUtils.EncryptAndSplit(zipBytes, encryption, crypto, partCount: 1);

            // Act
            var openBatchRequest = BatchUtils.BuildOpenBatchRequest(zipMeta, encryption, parts, systemCode);
            var openBatchResponse = await BatchUtils.OpenBatchAsync(ksefClient, openBatchRequest, accessToken);
            await BatchUtils.SendBatchPartsAsync(ksefClient, openBatchResponse, parts);
            await BatchUtils.CloseBatchAsync(ksefClient, openBatchResponse.ReferenceNumber, accessToken);

            // Assert
            var status = await BatchUtils.WaitForBatchStatusAsync(ksefClient, openBatchResponse.ReferenceNumber, accessToken);
            Assert.True(status.Status.Code == 445);
            Assert.Equal(DefaultInvoiceCount, status.FailedInvoiceCount);
        }
    }
}
