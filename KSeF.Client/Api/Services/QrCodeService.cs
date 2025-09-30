#if QR
using KSeF.Client.Core.Interfaces.Services;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;
using QRCoder;
using SkiaSharp;
#endif

namespace KSeF.Client.Api.Services;

public class QrCodeService : IQrCodeService
{
#if QR
    public byte[] GenerateQrCode(string payloadUrl, int pixelsPerModule = 20, int qrCodeSize = 300)
    {
        using QRCodeGenerator gen = new QRCodeGenerator();
        using QRCodeData qrData = gen.CreateQrCode(payloadUrl, QRCodeGenerator.ECCLevel.Default);

        int modules = qrData.ModuleMatrix.Count;
        float cellSize = qrCodeSize / (float)modules;

        SKImageInfo info = new SKImageInfo(qrCodeSize, qrCodeSize);
        using SKSurface surface = SKSurface.Create(info);
        SKCanvas skCanvas = surface.Canvas;

        SkiaCanvas canvas = new SkiaCanvas();
        canvas.Canvas = skCanvas;
        canvas.SetDisplayScale(1f);

        canvas.FillColor = Colors.White;
        canvas.FillRectangle(0, 0, qrCodeSize, qrCodeSize);

        canvas.FillColor = Colors.Black;
        for (int y = 0; y < modules; y++)
            for (int x = 0; x < modules; x++)
                if (qrData.ModuleMatrix[y][x])
                    canvas.FillRectangle(x * cellSize, y * cellSize, cellSize, cellSize);

        // Eksport PNG
        using SKImage img = surface.Snapshot();
        using SKData data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public byte[] ResizePng(byte[] pngBytes, int targetWidth, int targetHeight)
    {
        using SKBitmap skBitmap = SKBitmap.Decode(pngBytes);
        SKImageInfo info = new SKImageInfo(targetWidth, targetHeight);
        using SKSurface surface = SKSurface.Create(info);
        SkiaCanvas canvas = new SkiaCanvas() { Canvas = surface.Canvas };
        canvas.SetDisplayScale(1f);

        IImage image = new SkiaImage(skBitmap);
        canvas.DrawImage(image, 0, 0, targetWidth, targetHeight);

        using SKImage snap = surface.Snapshot();
        using SKData encoded = snap.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    public byte[] AddLabelToQrCode(byte[] qrPng, string label, int fontSizePx = 14)
    {
        using SKBitmap skBitmap = SKBitmap.Decode(qrPng);
        IImage qrImage = new SkiaImage(skBitmap);
        int width = skBitmap.Width;
        int height = skBitmap.Height;

        Font font = new Font("Arial", fontSizePx);

        // Pomiar tekstu
        SkiaCanvas measureCanvas = new SkiaCanvas() { Canvas = SKSurface.Create(new SKImageInfo(1, 1)).Canvas };
        measureCanvas.SetDisplayScale(1f);
        measureCanvas.Font = font;
        measureCanvas.FontSize = fontSizePx;
        SizeF textSize = measureCanvas.GetStringSize(label, font, fontSizePx);
        float labelHeight = textSize.Height + 4;

        // Nowa powierzchnia dla połączonego obrazu
        SKImageInfo info = new SKImageInfo(width, height + (int)labelHeight);
        using SKSurface surface = SKSurface.Create(info);
        SkiaCanvas canvas = new SkiaCanvas() { Canvas = surface.Canvas };
        canvas.SetDisplayScale(1f);

        // Tło
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(0, 0, width, height + labelHeight);

        // Kod QR
        canvas.DrawImage(qrImage, 0, 0, width, height);

        // Rysuj etykietę
        canvas.Font = font;
        canvas.FontSize = fontSizePx;
        canvas.FontColor = Colors.Black;
        RectF rect = new RectF(0, height, width, labelHeight);
        canvas.DrawString(label, rect, HorizontalAlignment.Center, VerticalAlignment.Center);

        // Eksport PNG
        using SKImage snap2 = surface.Snapshot();
        using SKData pngData = snap2.Encode(SKEncodedImageFormat.Png, 100);
        return pngData.ToArray();
    }
#else
#endif
	public byte[] AddLabelToQrCode(byte[] qrCodePng, string label, int fontSizePx = 14) => throw new NotImplementedException();
	public byte[] GenerateQrCode(string payloadUrl, int pixelsPerModule = 20, int qrCodeResolutionInPx = 300) => throw new NotImplementedException();
}
