using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.QRCode;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class QrCodeController(
    IVerificationLinkService linkSvc,
    IQrCodeService qrSvc) : ControllerBase
{
    private readonly IVerificationLinkService linkSvc = linkSvc ?? throw new ArgumentNullException(nameof(linkSvc));
    private readonly IQrCodeService qrSvc = qrSvc ?? throw new ArgumentNullException(nameof(qrSvc));

    // 1. Faktura z numerem KSeF (online)
    [HttpGet("invoice/ksef")]
    public ActionResult<QrCodeResult> GetInvoiceQrWithKsef(
        string nip,
        DateTime issueDate,
        string xml,
        string ksefNumber)                       // numer KSeF z API
    {
        var url = linkSvc.BuildInvoiceVerificationUrl(nip, issueDate, xml);
        var qrBytes = qrSvc.GenerateQrCode(url);
        var labeled = qrSvc.AddLabelToQrCode(qrBytes, ksefNumber);

        return Ok(new QrCodeResult(url, qrBytes, labeled));
    }

    // 2. Faktura offline (przed wysyłką)
    [HttpGet("invoice/offline")]
    public ActionResult<QrCodeResult> GetInvoiceQrOffline(
        string nip,
        DateTime issueDate,
        string xml)
    {
        var url = linkSvc.BuildInvoiceVerificationUrl(nip, issueDate, xml);
        var qrBytes = qrSvc.GenerateQrCode(url);
        var labeled = qrSvc.AddLabelToQrCode(qrBytes, "OFFLINE");

        return Ok(new QrCodeResult(url, qrBytes, labeled));
    }

    // 3. Weryfikacja certyfikatu (Kod II)
    [HttpGet("certificate")]
    public ActionResult<QrCodeResult> GetCertificateQr(
        string nip,
        Guid certSerial,
        string xml,
        X509Certificate2 cert)
    {
        var url = linkSvc.BuildCertificateVerificationUrl(nip, certSerial, xml, cert);
        var qrBytes = qrSvc.GenerateQrCode(url);
        var labeled = qrSvc.AddLabelToQrCode(qrBytes, "CERTYFIKAT");

        return Ok(new QrCodeResult(url, qrBytes, labeled));
    }
}
