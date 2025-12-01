using KSeF.Client.Core.Interfaces.Services;

namespace KSeF.Client.Api.Services;

public class QrCodeService : IQrCodeService
{
	public byte[] AddLabelToQrCode(byte[] qrCodePng, string label, int fontSizePx = 14) => throw new NotImplementedException();
	public byte[] GenerateQrCode(string payloadUrl, int pixelsPerModule = 20, int qrCodeResolutionInPx = 300) => throw new NotImplementedException();
}
