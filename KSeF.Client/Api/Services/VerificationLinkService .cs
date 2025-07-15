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
            string invoiceHash,
            X509Certificate2 signingCertificate,
            string privateKey = ""
        )
        {
            var signedHash = ComputeUrlEncodedSignedHash(invoiceHash, signingCertificate, privateKey);
            return $"{BaseUrl}/certificate/{nip}/{certificateSerial}/{invoiceHash}/{signedHash}";
        }



        private static string ComputeUrlEncodedSignedHash(string invoiceHash, X509Certificate2 cert, string privateKey = "")
        {
            // 1. SHA-256
            byte[] sha = Convert.FromBase64String(invoiceHash);

            if (!string.IsNullOrEmpty(privateKey))
            {                
                if (privateKey.StartsWith("-----"))
                {
                    privateKey = string.Concat(
                        privateKey
                            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(l => !l.StartsWith("-----"))
                    );
                }

                byte[] privateKeyBytes = Convert.FromBase64String(privateKey);

                // 1.1 Importujemy tylko, gdy certyfikat nie ma klucza prywatnego
                if (!cert.HasPrivateKey)
                {
                    if (cert.GetRSAPrivateKey() != null)
                    {
                        using var rsaTemp = RSA.Create();
                        rsaTemp.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                        cert = cert.CopyWithPrivateKey(rsaTemp);
                    }
                    else if (cert.GetECDsaPrivateKey() != null)
                    {
                        using var ecdsaTemp = ECDsa.Create();
                        ecdsaTemp.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                        cert = cert.CopyWithPrivateKey(ecdsaTemp);
                    }
                    else
                    {
                        throw new InvalidOperationException("Certyfikat nie wspiera RSA ani ECDSA.");
                    }
                }
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
