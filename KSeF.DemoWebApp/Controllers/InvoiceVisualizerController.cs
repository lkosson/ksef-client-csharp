using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.DemoWebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class InvoiceVisualizerController : ControllerBase
    {
        private readonly IVerificationLinkService _linkSvc;
        private readonly IQrCodeService _qrSvc;

        public InvoiceVisualizerController(IVerificationLinkService linkSvc,
        IQrCodeService qrSvc
    )
        {
            _linkSvc = linkSvc;
            _qrSvc = qrSvc;

        }
        public JsonResult RenderOfflineInvoice(string nip, Guid certSerial, string xml, X509Certificate2 cert)
        {
            // a) link do faktury
            var invoiceUrl = _linkSvc.BuildInvoiceVerificationUrl(nip, DateTime.Now, xml);
            var invoiceQr = _qrSvc.GenerateQrCode(invoiceUrl);

            // b) link do weryfikacji certyfikatu
            var certUrl = _linkSvc.BuildCertificateVerificationUrl(nip, certSerial, xml, cert);
            var certQr = _qrSvc.GenerateQrCode(certUrl);

            return new JsonResult(new
            {
                InvoiceUrl = invoiceUrl,
                InvoiceQrCode = invoiceQr,
                CertificateUrl = certUrl,
                CertificateQrCode = certQr
            });
        }
    }
}
