using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeFClient;
using System.IO.Compression;
using System.Text;

namespace KSeF.Client.Tests.Utils
{
    internal static class BatchUtils
    {
        private const string DefaultSystemCode = "FA (2)";
        private const string DefaultSchemaVersion = "1-0E";
        private const string DefaultValue = "FA";
        private const int DefaultSleepTimeMs = 1000;
        private const int DefaultMaxAttempts = 60;
        private const int DefaultPageOffset = 0;
        private const int DefaultPageSize = 10;

        internal static async Task<SessionInvoicesResponse> GetSessionInvoicesAsync(
            IKSeFClient ksefClient, 
            string referenceNumber, 
            string accessToken, 
            int pageOffset = DefaultPageOffset, int pageSize = DefaultPageSize)
            => await ksefClient.GetSessionInvoicesAsync(referenceNumber, accessToken, pageSize);

        internal static async Task<string> GetSessionInvoiceUpoByKsefNumberAsync(
            IKSeFClient ksefClient, 
            string referenceNumber, 
            string ksefNumber, 
            string accessToken)
            => await ksefClient.GetSessionInvoiceUpoByKsefNumberAsync(referenceNumber, ksefNumber, accessToken);

        internal static async Task<OpenBatchSessionResponse> OpenBatchAsync(
            IKSeFClient client,
            OpenBatchSessionRequest openReq,
            string accessToken)
            => await client.OpenBatchSessionAsync(openReq, accessToken);

        internal static async Task SendBatchPartsAsync(
            IKSeFClient client,
            OpenBatchSessionResponse openResp,
            ICollection<BatchPartSendingInfo> parts)
            => await client.SendBatchPartsAsync(openResp, parts);

        internal static async Task CloseBatchAsync(
            IKSeFClient client,
            string referenceNumber,
            string accessToken)
            => await client.CloseBatchSessionAsync(referenceNumber, accessToken);

        /// <summary>
        /// Buduje ZIP w pamięci z podanych plików i zwraca bajty + metadane (przed szyfrowaniem).
        /// </summary>
        internal static (byte[] ZipBytes, FileMetadata Meta) BuildZip(
            IEnumerable<(string FileName, byte[] Content)> files,
            ICryptographyService crypto)
        {
            using var zipStream = new MemoryStream();

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var f in files)
                {
                    var entry = archive.CreateEntry(f.FileName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    entryStream.Write(f.Content, 0, f.Content.Length);
                }
            }

            var zipBytes = zipStream.ToArray();
            var meta = crypto.GetMetaData(zipBytes);

            return (zipBytes, meta);
        }

        /// <summary>
            /// Dzieli bufor na <paramref name="partCount"/> części o (z grubsza) równym rozmiarze.
        /// </summary>
        internal static List<byte[]> Split(byte[] input, int partCount)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(partCount);

            var result = new List<byte[]>(partCount);
            var partSize = (int)Math.Ceiling((double)input.Length / partCount);

            for (int i = 0; i < partCount; i++)
            {
                var start = i * partSize;
                var size = Math.Min(partSize, input.Length - start);
                if (size <= 0) break;

                var part = new byte[size];
                Array.Copy(input, start, part, 0, size);
                result.Add(part);
            }

            return result;
        }

        /// <summary>
            /// Szyfruje i pakuje do struktur partów (1..N). Gdy <paramref name="partCount"/> == 1, nie dzieli.
        /// </summary>
        internal static List<BatchPartSendingInfo> EncryptAndSplit(
            byte[] zipBytes,
            EncryptionData encryption,
            ICryptographyService crypto,
            int partCount = 1)
        {
            ArgumentNullException.ThrowIfNull(zipBytes);
            ArgumentNullException.ThrowIfNull(encryption);
            ArgumentNullException.ThrowIfNull(crypto);

            var rawParts = partCount <= 1
                ? new List<byte[]> { zipBytes }
                : Split(zipBytes, partCount);

            var result = new List<BatchPartSendingInfo>(rawParts.Count);

            for (int i = 0; i < rawParts.Count; i++)
            {
                var encrypted = crypto.EncryptBytesWithAES256(rawParts[i], encryption.CipherKey, encryption.CipherIv);
                var meta = crypto.GetMetaData(encrypted);

                result.Add(new BatchPartSendingInfo
                {
                    Data = encrypted,
                    OrdinalNumber = i + 1,
                    Metadata = meta
                });
            }

            return result;
        }

        /// <summary>
            /// Buduje OpenBatchSessionRequest z FormCode i listą zaszyfrowanych partów.
        /// </summary>
        internal static OpenBatchSessionRequest BuildOpenBatchRequest(
            FileMetadata zipMeta,
            EncryptionData encryption,
            IEnumerable<BatchPartSendingInfo> encryptedParts,
            string systemCode = DefaultSystemCode,
            string schemaVersion = DefaultSchemaVersion,
            string value = DefaultValue)
        {
            var builder = OpenBatchSessionRequestBuilder
                .Create()
                .WithFormCode(systemCode: systemCode, schemaVersion: schemaVersion, value: value)
                .WithBatchFile(fileSize: zipMeta.FileSize, fileHash: zipMeta.HashSHA);

            foreach (var p in encryptedParts)
            {
                builder = builder.AddBatchFilePart(
                    ordinalNumber: p.OrdinalNumber,
                    fileName: $"part_{p.OrdinalNumber}.zip.aes",
                    fileSize: p.Metadata.FileSize,
                    fileHash: p.Metadata.HashSHA);
            }

            return builder
                .EndBatchFile()
                .WithEncryption(
                    encryptedSymmetricKey: encryption.EncryptionInfo.EncryptedSymmetricKey,
                    initializationVector: encryption.EncryptionInfo.InitializationVector)
                .Build();
        }

        /// <summary>
        // Pollinguje status sesji aż do zakończenia (successful/rejected/failed) lub przekroczenia limitu prób.
        /// </summary>
        internal static async Task<SessionStatusResponse> WaitForBatchStatusAsync(
            IKSeFClient client,
            string sessionRef,
            string accessToken,
            int sleepTime = DefaultSleepTimeMs,
            int maxAttempts = DefaultMaxAttempts)
        {
            SessionStatusResponse sessionStatus = null!;
            for (int i = 0; i < maxAttempts; i++)
            {
                sessionStatus = await client.GetSessionStatusAsync(sessionRef, accessToken);

                if (sessionStatus.Status.Code != 150) // W trakcie przetwrzania
                {
                    return sessionStatus;
                }

                await Task.Delay(sleepTime);
            }

            return sessionStatus;
        }

        /// <summary>
        /// Pomocnicze: generuje dokumenty XML w pamięci na podstawie template i NIP.
        /// </summary>
        internal static List<(string FileName, byte[] Content)> GenerateInvoicesInMemory(
            int count,
            string nip,
            string templatePath,
            Func<string>? invoiceNumberFactory = null)
        {
            List<(string FileName, byte[] Content)> list = new(count); ;
            var template = File.ReadAllText(templatePath, Encoding.UTF8);

            for (int i = 0; i < count; i++)
            {
                var xml = template
                    .Replace("#nip#", nip)
                    .Replace("#invoice_number#", (invoiceNumberFactory?.Invoke() ?? Guid.NewGuid().ToString()));
                list.Add(($"faktura_{i + 1}.xml", Encoding.UTF8.GetBytes(xml)));
            }
            return list;
        }
    }
}
