using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace KSeF.Client.Api.Services
{
    public class QrCodeService : IQrCodeService
    {
        public byte[] GenerateQrCode(string payloadUrl, int pixelsPerModule = 20, int qrCodeWidthAndHeight = 300)
        {
            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(payloadUrl, QRCodeGenerator.ECCLevel.Default);
            using var qr = new PngByteQRCode(data);            
            var graphic = qr.GetGraphic(pixelsPerModule);

            return ResizePng(graphic, qrCodeWidthAndHeight, qrCodeWidthAndHeight);
        }

        public byte[] ResizePng(byte[] pngBytes, int targetWidth, int targetHeight)
        {
            using var input = new MemoryStream(pngBytes);
            using var original = Image.FromStream(input);
            using var resized = new Bitmap(original, new Size(targetWidth, targetHeight));
            using var ms = new MemoryStream();
            resized.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        public byte[] AddLabelToQrCode(byte[] qrPng, string label, int fontSizePx = 14)
        {
            // 1. Bitmapa QR z bajtów
            using var msIn = new MemoryStream(qrPng);
            using var qrBmp = (Bitmap)System.Drawing.Image.FromStream(msIn);

            // 2. Font + wysokość napisu
            using var font = new System.Drawing.Font("Arial", fontSizePx, FontStyle.Bold, GraphicsUnit.Pixel);
            int labelHeight = (int)(font.GetHeight() + 4);

            // 3. Nowa bitmapa = QR + pasek na tekst
            using var canvas = new Bitmap(qrBmp.Width, qrBmp.Height + labelHeight);
            using var g = Graphics.FromImage(canvas);
            g.Clear(Color.White);
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            // 4. Rysuj QR
            g.DrawImage(qrBmp, 0, 0);

            // 5. Rysuj tekst wyśrodkowany
            var rect = new RectangleF(0, qrBmp.Height, qrBmp.Width, labelHeight);
            var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(label, font, Brushes.Black, rect, fmt);

            // 6. PNG → bajt[]
            using var msOut = new MemoryStream();
            canvas.Save(msOut, ImageFormat.Png);
            return msOut.ToArray();
        }
    }
}
