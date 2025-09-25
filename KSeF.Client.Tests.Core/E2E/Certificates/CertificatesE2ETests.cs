using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Api.Builders.Certificates;

namespace KSeF.Client.Tests.Core.E2E.Certificates;

[Collection("CertificateScenarioE2ECollection")]
public class CertificatesE2ETests : TestBase
{
    private const string TestCertificateName = "E2E Test Cert";
    private const int CertificateValidityDays = 1;
    private const int StatusRetreivalDelayInMilliseconds = 1000;
    private const int MaxStatusRetreivalCounter = 5;
    private const int StatusPendingCode = 100;
    private const int StatusCompletedCode = 200;
    private readonly CertificatesScenarioE2EFixture TestFixture;
    private string accessToken = string.Empty;
    private int statusRetreivalRetryCounter = 0;

    public CertificatesE2ETests()
    {
        TestFixture = new CertificatesScenarioE2EFixture();
        AuthOperationStatusResponse authOperationStatusResponse =
            AuthenticationUtils.AuthenticateAsync(KsefClient, SignatureService).GetAwaiter().GetResult();
        accessToken = authOperationStatusResponse.AccessToken.Token;
    }

    /// <summary>
    /// Pobiera listę oraz limity certyfikatów, 
    /// wysyła żądanie wystawienia certyfikatu, 
    /// pobiera wystawiony certyfikat, 
    /// odwołuje go 
    /// oraz sprawdza że certyfikat został poprawnie usunięty.
    /// </summary>
    [Fact]
    public async Task Certificates_FullIntegrationFlow_ReturnsExpectedResults()
    {
        #region Pobierz limity certyfikatów
        // Act
        CertificateLimitResponse certificateLimitsResponse = await GetCertificateLimitsAsync();
        TestFixture.Limits = certificateLimitsResponse;

        // Assert
        Assert.NotNull(TestFixture.Limits);
        Assert.True(TestFixture.Limits.CanRequest);
        #endregion

        #region Pobierz informacje o zarejstrowanych certyfikatach
        // Act
        CertificateEnrollmentsInfoResponse certificateEnrollmentsInfoResponse = await GetCertificateEnrollmentDataAsync();
        TestFixture.EnrollmentInfo = certificateEnrollmentsInfoResponse;
        
        // Assert
        Assert.NotNull(TestFixture.EnrollmentInfo);
        Assert.NotEmpty(TestFixture.EnrollmentInfo.SerialNumber);
        #endregion

        #region Wyślij zgłoszenie nowe
        // Arrange
        (string csr, string key) = CryptographyService.GenerateCsrWithRSA(TestFixture.EnrollmentInfo, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        SendCertificateEnrollmentRequest sendCertificateEnrollmentRequest = SendCertificateEnrollmentRequestBuilder
            .Create()
            .WithCertificateName(TestCertificateName)
            .WithCertificateType(CertificateType.Authentication)
            .WithCsr(csr)
            .WithValidFrom(DateTimeOffset.UtcNow.AddDays(CertificateValidityDays))
            .Build();

        // Act
        CertificateEnrollmentResponse certificateEnrollmentResponse = await SendCertificateEnrollmentAsync(csr, sendCertificateEnrollmentRequest);
        TestFixture.EnrollmentReference = certificateEnrollmentResponse.ReferenceNumber;

        // Assert
        Assert.NotNull(TestFixture.EnrollmentReference);
        Assert.False(string.IsNullOrWhiteSpace(TestFixture.EnrollmentReference));
        #endregion

        #region Sprawdź status rejestracji
        // Act
        CertificateEnrollmentStatusResponse certificateEnrollmentStatusResponse;
        int maxRetry = 10;
        int currentRetry = 0;

        do
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            certificateEnrollmentStatusResponse = await GetCertificateEnrollmentStatusAsync();
            currentRetry++;
        }
        while (certificateEnrollmentStatusResponse.Status.Code <= 100 && currentRetry < maxRetry);

        TestFixture.EnrollmentStatus = certificateEnrollmentStatusResponse;
        
        // Assert
        Assert.Equal(StatusCompletedCode, TestFixture.EnrollmentStatus.Status.Code);
        #endregion

        #region Pobierz zarejstrowany certyfikat
        // Arrange
        TestFixture.SerialNumbers = new List<string> { TestFixture.EnrollmentStatus.CertificateSerialNumber };
        CertificateListRequest certificateListRequest = new CertificateListRequest { CertificateSerialNumbers = TestFixture.SerialNumbers };

        // Act
        CertificateListResponse certificateListResponse = await GetCertificateListAsync(certificateListRequest);
        TestFixture.RetrievedCertificates = certificateListResponse;

        // Assert
        Assert.NotNull(TestFixture.RetrievedCertificates);
        Assert.Single(TestFixture.RetrievedCertificates.Certificates);
        #endregion

        #region Cofnij certyfikat
        // Arrange
        string certificateSerialNumber = TestFixture.RetrievedCertificates.Certificates.ToList().First().CertificateSerialNumber;
        CertificateRevokeRequest certificateRevokeRequest = RevokeCertificateRequestBuilder
            .Create()
            .WithRevocationReason(CertificateRevocationReason.KeyCompromise)
            .Build();

        // Act && Assert
        var exception = await Record.ExceptionAsync(async () =>
            await RevokeCertificateAsync(certificateRevokeRequest, certificateSerialNumber, accessToken)
        );
        Assert.Null(exception);
        #endregion

        #region Pobierz listę metadanych zarejestrowanych certyfikatów
        CertificateMetadataListResponse certificateMetadataListResponse = await GetCertificateMetadataListAsync(accessToken);
        TestFixture.MetadataList = certificateMetadataListResponse;

        // Assert
        Assert.NotNull(TestFixture.MetadataList);
        Assert.Contains(TestFixture.MetadataList.Certificates.ToList(), m => TestFixture.SerialNumbers.Contains(m.CertificateSerialNumber));
        #endregion
    }

    /// <summary>
    /// Pobiera limity certyfikatów dla uwierzytelnionego użytkownika.
    /// </summary>
    /// <returns>Informacje o liczbie wystawionych wniosków oraz certyfikatów.</returns>
    private async Task<CertificateLimitResponse> GetCertificateLimitsAsync()
    {
        CertificateLimitResponse certificateLimitResponse = await KsefClient
            .GetCertificateLimitsAsync(accessToken, CancellationToken);

        return certificateLimitResponse;
    }

    /// <summary>
    /// Pobiera dane niezbędne do wygenerowania CSR.
    /// </summary>
    /// <returns>Dane niezbędne do wygenerowania CSR.</returns>
    private async Task<CertificateEnrollmentsInfoResponse> GetCertificateEnrollmentDataAsync()
    {
        CertificateEnrollmentsInfoResponse certificateEnrollmentsInfoResponse =
            await KsefClient.GetCertificateEnrollmentDataAsync(accessToken, CancellationToken);

        return certificateEnrollmentsInfoResponse;
    }

    /// <summary>
    /// Wysyła żądanie wystawienia certyfikatu.
    /// </summary>
    /// <param name="csr"></param>
    /// <param name="sendCertificateEnrollmentRequest"></param>
    /// <returns>Zwraca numer referencyjny oraz datę i godzinę operacji.</returns>
    private async Task<CertificateEnrollmentResponse> SendCertificateEnrollmentAsync(string csr, SendCertificateEnrollmentRequest sendCertificateEnrollmentRequest)
    {
        CertificateEnrollmentResponse certificateEnrollmentResponse = await KsefClient
            .SendCertificateEnrollmentAsync(sendCertificateEnrollmentRequest, accessToken, CancellationToken);

        return certificateEnrollmentResponse;
    }

    /// <summary>
    /// Pobiera status wystawienia certyfikatu.
    /// </summary>
    /// <returns>Status wystawienia certyfikatu z jego numerem lub powodem odrzucenia żądania.</returns>
    private async Task<CertificateEnrollmentStatusResponse> GetCertificateEnrollmentStatusAsync()
    {
        CertificateEnrollmentStatusResponse certificateEnrollmentStatusResponse = await KsefClient
            .GetCertificateEnrollmentStatusAsync(TestFixture.EnrollmentReference, accessToken, CancellationToken);

        int tryCounter = statusRetreivalRetryCounter;
        do
        {
            tryCounter++;

            await Task.Delay(StatusRetreivalDelayInMilliseconds);
            certificateEnrollmentStatusResponse = await KsefClient
                .GetCertificateEnrollmentStatusAsync(TestFixture.EnrollmentReference, accessToken, CancellationToken);
        }
        while (certificateEnrollmentStatusResponse.Status.Code == StatusPendingCode && tryCounter < MaxStatusRetreivalCounter);

        return certificateEnrollmentStatusResponse;
    }

    /// <summary>
    /// Pobiera wystawione certyfikaty na podstawie podanych numerów seryjnych.
    /// </summary>
    /// <param name="certificateListRequest"></param>
    /// <returns>Listę wystawionych certyfikatów.</returns>
    private async Task<CertificateListResponse> GetCertificateListAsync(CertificateListRequest certificateListRequest)
    {
        CertificateListResponse certificateListResponse = await KsefClient
            .GetCertificateListAsync(certificateListRequest, accessToken, CancellationToken);

        return certificateListResponse;
    }

    /// <summary>
    /// Odwołuje certyfikat na podstawie podanego numeru seryjnego.
    /// </summary>
    /// <param name="certificateRevokeRequest"></param>
    /// <param name="certificateSerialNumber"></param>
    /// <param name="AccessToken"></param>
    private async Task RevokeCertificateAsync(CertificateRevokeRequest certificateRevokeRequest, string certificateSerialNumber, string accessToken)
    {
        await KsefClient
            .RevokeCertificateAsync(certificateRevokeRequest, certificateSerialNumber, accessToken, CancellationToken);
    }

    /// <summary>
    /// Pobiera metadane wystawionych certyfikatów.
    /// </summary>
    /// <param name="AccessToken"></param>
    /// <returns>Listę metadanych wystawionych certyfikatów.</returns>
    private async Task<CertificateMetadataListResponse> GetCertificateMetadataListAsync(string accessToken, CertificateMetadataListRequest? requestPayload = null, int pageSize = 10, int pageOffset = 0)
    {
        CertificateMetadataListResponse certificateMetadataListResponse = await KsefClient
            .GetCertificateMetadataListAsync(accessToken, requestPayload, pageSize, pageOffset, CancellationToken);

        return certificateMetadataListResponse;
    }
}