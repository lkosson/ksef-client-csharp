using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.Certificates;

public interface ISendCertificateEnrollmentRequestBuilder
{
    ISendCertificateEnrollmentRequestBuilderWithCertificateName WithCertificateName(string certificateName);
}

public interface ISendCertificateEnrollmentRequestBuilderWithCertificateName
{
    ISendCertificateEnrollmentRequestBuilderWithCsr WithCsr(string csr);
    ISendCertificateEnrollmentRequestBuilderWithCertificateType WithCertificateType(CertificateType certificateType);
}

public interface ISendCertificateEnrollmentRequestBuilderWithCertificateType
{
    ISendCertificateEnrollmentRequestBuilderWithCsr WithCsr(string csr);
}

public interface ISendCertificateEnrollmentRequestBuilderWithCsr
{
    ISendCertificateEnrollmentRequestBuilderWithCsr WithValidFrom(DateTimeOffset validFrom);
    SendCertificateEnrollmentRequest Build();
}

internal class SendCertificateEnrollmentRequestBuilderImpl :
    ISendCertificateEnrollmentRequestBuilder,
    ISendCertificateEnrollmentRequestBuilderWithCertificateName,
    ISendCertificateEnrollmentRequestBuilderWithCertificateType,
    ISendCertificateEnrollmentRequestBuilderWithCsr
{
    private string _certificateName;
    private string _csr;
    private DateTimeOffset? _validFrom;
    private CertificateType _certificateType;
    private bool _certificateTypeSet;

    public static ISendCertificateEnrollmentRequestBuilder Create() =>
        new SendCertificateEnrollmentRequestBuilderImpl();

    public ISendCertificateEnrollmentRequestBuilderWithCertificateName WithCertificateName(string certificateName)
    {
        if (!RegexPatterns.CertificateName.IsMatch(certificateName))
        {
            throw new ArgumentException("Nazwa certyfikatu zawiera niedozwolone znaki", nameof(certificateName));
        }
        if (certificateName.Length < ValidValues.CertificateNameMinLength)
        {
            throw new ArgumentException($"Nazwa certyfikatu za krótka, minimalna długość: {ValidValues.CertificateNameMinLength} znaków.", nameof(certificateName));
        }
        if (certificateName.Length > ValidValues.CertificateNameMaxLength)
        {
            throw new ArgumentException($"Nazwa certyfikatu za długa, maksymalna długość: {ValidValues.CertificateNameMaxLength} znaków.", nameof(certificateName));
        }

        _certificateName = certificateName;
        return this;
    }

    public ISendCertificateEnrollmentRequestBuilderWithCertificateType WithCertificateType(CertificateType certificateType)
    {
        _certificateType = certificateType;
        _certificateTypeSet = true;
        return this;
    }

    public ISendCertificateEnrollmentRequestBuilderWithCsr WithCsr(string csr)
    {
        if (string.IsNullOrWhiteSpace(csr))
        {
            throw new ArgumentNullException(nameof(csr));
        }

        _csr = csr;
        return this;
    }

    public ISendCertificateEnrollmentRequestBuilderWithCsr WithValidFrom(DateTimeOffset validFrom)
    {
        _validFrom = validFrom;
        return this;
    }

    public SendCertificateEnrollmentRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_certificateName))
        {
            throw new InvalidOperationException("Najpierw należy wywołać WithCertificateName(...).");
        }
        if (!_certificateTypeSet)
        {
            throw new InvalidOperationException("Najpierw należy wywołać WithCertificateType(...).");
        }
        if (string.IsNullOrWhiteSpace(_csr))
        {
            throw new InvalidOperationException("Najpierw należy wywołać WithCsr(...).");
        }

        return new SendCertificateEnrollmentRequest
        {
            CertificateName = _certificateName,
            CertificateType = _certificateType,
            Csr = _csr,
            ValidFrom = _validFrom
        };
    }
}

public static class SendCertificateEnrollmentRequestBuilder
{
    public static ISendCertificateEnrollmentRequestBuilder Create() =>
        SendCertificateEnrollmentRequestBuilderImpl.Create();
}