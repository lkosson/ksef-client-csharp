using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Extensions
{
    public static class X509CertificateLoaderExtensions
    {
        public static X509Certificate2 LoadCertificate(this byte[] certBytes)
        {
#if NET9_0_OR_GREATER
            X509Certificate2 certWithKey = X509CertificateLoader.LoadCertificate(certBytes);
#else
            // Explicitly specify the type of null to resolve ambiguity
            X509Certificate2 certWithKey = new X509Certificate2(
                certBytes,
                (string)null,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
#endif
            return certWithKey;
        }


        public static X509Certificate2 LoadPkcs12(this byte[] certBytes)
        {
#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadPkcs12(certBytes, password: string.Empty,
                                                          X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
#else
            // Explicitly specify the type of null to resolve ambiguity
            return new X509Certificate2(
                certBytes,
                string.Empty,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
#endif
        }
    }
}