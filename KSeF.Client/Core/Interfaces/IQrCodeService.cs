namespace KSeF.Client.Api.Services
{
    public interface IQrCodeService
    {
        /// <summary>
        /// Generuje kod QR jako tablicę bajtów PNG.
        /// </summary>
        /// <param name="payloadUrl">URL/link do zakodowania.</param>
        /// <param name="pixelsPerModule">Rozmiar modułu w pikselach (domyślnie 20).</param>
        byte[] GenerateQrCode(
            string payloadUrl,
            int pixelsPerModule = 20
        );
    }
}
