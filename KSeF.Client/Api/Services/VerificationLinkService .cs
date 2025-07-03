using KSeF.Client.Core.Interfaces;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

namespace KSeF.Client.Api.Services
{
    public class VerificationLinkService : IVerificationLinkService
    {
        private const string BaseUrl = "https://ksef.mf.gov.pl/web";

        public string BuildInvoiceVerificationUrl(string nip, DateTime issueDate, string xmlContent)
        {
            var date = issueDate.ToString("dd-MM-yyyy");
            var hash = ComputeUrlEncodedBase64Sha256(xmlContent);
            return $"{BaseUrl}/verify-invoice/{nip}/{date}/{hash}";
        }

        public string BuildCertificateVerificationUrl(
            string nip,
            Guid certificateSerial,
            string xmlContent,
            X509Certificate2 signingCertificate
        )
        {
            var hash = ComputeUrlEncodedBase64Sha256(xmlContent);
            var signedHash = ComputeUrlEncodedSignedHash(xmlContent, signingCertificate);
            return $"{BaseUrl}/verify-certificate/{nip}/{certificateSerial}/{hash}/{signedHash}";
        }

        private static string ComputeUrlEncodedBase64Sha256(string xml)
        {
            byte[] sha;
            using (var sha256 = SHA256.Create())
                sha = sha256.ComputeHash(Encoding.UTF8.GetBytes(xml));

            var b64 = Convert.ToBase64String(sha);
            return HttpUtility.UrlEncode(b64);
        }

        private static string ComputeUrlEncodedSignedHash(string xml, X509Certificate2 cert)
        {
            // 1. SHA-256
            byte[] sha;
            using (var sha256 = SHA256.Create())
                sha = sha256.ComputeHash(Encoding.UTF8.GetBytes(xml));

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
