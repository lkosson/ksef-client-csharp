namespace KSeF.Client.Core.Models.QRCode
{
    public class QrCodeResult
    {

        public QrCodeResult(string url, byte[] qrBytes, byte[] labeledQrBytes)
        {
            this.Url = url;
            this.QrCode= qrBytes;
            this.labeledQrCode= labeledQrBytes;
        }

        public string Url { get; set; }
        public byte[] QrCode { get; set; }
        public byte[] labeledQrCode { get; set; }
    }
}