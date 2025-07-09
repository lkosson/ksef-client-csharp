using KSeF.Client.Core.Interfaces;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

namespace KSeF.Client.Api.Services
{
    public class VerificationLinkService : IVerificationLinkService
    {
        private const string BaseUrl = "https://ksef.mf.gov.pl/client-app";

        public string BuildInvoiceVerificationUrl(string nip, DateTime issueDate, string invoicehash)
        {
            var date = issueDate.ToString("dd-MM-yyyy");
            return $"{BaseUrl}/invoice/{nip}/{date}/{invoicehash}";
        }

        public string BuildCertificateVerificationUrl(
            string nip,
            string certificateSerial,
            string invoicehash,
            X509Certificate2 signingCertificate,
            string privateKey = ""
        )
        {

            var signedHash = ComputeUrlEncodedSignedHash(invoicehash, signingCertificate, privateKey);
            return $"{BaseUrl}/certificate/{nip}/{certificateSerial}/{invoicehash}/{signedHash}";
        }



        private static string ComputeUrlEncodedSignedHash(string xml, X509Certificate2 cert, string privateKey = "")
        {
            // 1. SHA-256
            byte[] sha;
            using (var sha256 = SHA256.Create())
                sha = sha256.ComputeHash(Encoding.UTF8.GetBytes(xml));
            if (!string.IsNullOrEmpty(privateKey))
            {
                var privateKeyBytes = Convert.FromBase64String(privateKey);

                using var rsaTemp = RSA.Create();
                rsaTemp.ImportRSAPrivateKey(privateKeyBytes, out _);
                cert = cert.CopyWithPrivateKey(rsaTemp);
            }
            // 2. Sign hash
            byte[] signature;
            if (cert.GetRSAPrivateKey() is RSA rsa)
            {
                signature = rsa.SignHash(sha, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            else if (cert.GetECDsaPrivateKey() is ECDsa ecdsa)
            {
                signature = ecdsa.SignHash(sha);
            }
            else
            {
                throw new InvalidOperationException("Certyfikat nie wspiera RSA ani ECDsa.");
            }

            // 3. Base64 + URL-encode
            var b64 = Convert.ToBase64String(signature);
            return HttpUtility.UrlEncode(b64);
        }
    }
}
