using QRCoder;

namespace KSeF.Client.Api.Services
{
    public class QrCodeService : IQrCodeService
    {
        public byte[] GenerateQrCode(string payloadUrl, int pixelsPerModule = 20)
        {
            QRCodeGenerator.ECCLevel eccLevel = QRCodeGenerator.ECCLevel.M;
            using var qrGen = new QRCodeGenerator();
            using var data = qrGen.CreateQrCode(payloadUrl, eccLevel);
            using var qr = new PngByteQRCode(data);
            return qr.GetGraphic(pixelsPerModule);
        }
    }
}
