namespace KSeF.Client.Core.Models.QRCode
{
    public class QrCodeResult
    {

        public QrCodeResult(string url, string qrCode)
        {
            this.Url = url;
        }

        public string Url { get; set; }
        public string QrCode { get; set; }
    }
}