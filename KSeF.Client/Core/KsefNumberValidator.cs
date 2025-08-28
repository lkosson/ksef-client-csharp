using System.Text;

namespace KSeF.Client.Core;

public static class KsefNumberValidator
{
    private const byte Polynomial = 0x07;
    private const byte InitValue = 0x00;

    private const int ExpectedLength = 35;
    private const int DataLength = 32;
    private const int ChecksumLength = 2;

    /// <summary>
    /// Validates a KSeF number by verifying its checksum and format.
    /// </summary>
    /// <param name="ksefNumber">The KSeF number to validate.</param>
    /// <param name="errorMessage">Outputs an error message if the number is invalid.</param>
    public static bool IsValid(string ksefNumber, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(ksefNumber))
        {
            errorMessage = "Numer KSeF jest pusty.";
            return false;
        }

        if (ksefNumber.Length != ExpectedLength)
        {
            errorMessage = $"Numer KSeF ma nieprawidłową długość: {ksefNumber.Length}. Oczekiwana długość to {ExpectedLength}.";
            return false;
        }

        string data = ksefNumber[..DataLength];
        string checksum = ksefNumber[^ChecksumLength..];

        string calculated = ComputeChecksum(Encoding.UTF8.GetBytes(data));

        return calculated == checksum;
    }

    private static string ComputeChecksum(byte[] data)
    {
        byte crc = InitValue;

        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 0x80) != 0
                    ? (byte)((crc << 1) ^ Polynomial)
                    : (byte)(crc << 1);
            }
        }

        return crc.ToString("X2"); // always 2-char hex
    }
}
