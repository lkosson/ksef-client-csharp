namespace KSeF.Client.Tests.Utils;

internal static class MiscellaneousUtils
{
    private static readonly Random Random = new();

    /// <summary>
    /// Generates a random valid NIP (10 digits).
    /// Regex used for validation:
    /// [1-9]((\d[1-9])|([1-9]\d))\d{7}
    /// </summary>
    internal static string GetRandomNip(string prefixTwoNumbers = "")
    {        
        int first;
        int second;

        if (prefixTwoNumbers.Length == 1)
        {
            first = int.Parse(prefixTwoNumbers[0].ToString());
            if (first == 0)
            {
                throw new ArgumentException("Value must be greater than 0");
            }
            second = Random.Next(0, 10);
        }
        else if (prefixTwoNumbers.Length == 2)
        {
            first = int.Parse(prefixTwoNumbers[0].ToString());
            second = int.Parse(prefixTwoNumbers[1].ToString());
        }
        else if (prefixTwoNumbers.Length == 0)
        {     
            first = Random.Next(1, 10);
            second = Random.Next(0, 10);
        }
        else
        {
            throw new ArgumentException("Prefix must be 0, 1 or 2 digits long.");
        }
        
        int third = Random.Next(0, 10);

        // regex wymaga, aby druga/trzecia cyfra nie tworzyły pary "00"
        if (second == 0 && third == 0)
        {
            third = Random.Next(1, 10);
        }

        string prefix = $"{first}{second}{third}";
        string rest = Random.Next(1000000, 9999999).ToString("D7");
        return prefix + rest;
    }

    /// <summary>
    /// Generates a random valid PESEL (11 digits, with date part in YYMMDD).
    /// Regex used for validation:
    /// ^\d{2}(?:0[1-9]|1[0-2]|2[1-9]|3[0-2]|4[1-9]|5[0-2]|6[1-9]|7[0-2]|8[1-9]|9[0-2])\d{7}$
    /// </summary>
    internal static string GetRandomPesel()
    {
        int year = Random.Next(1900, 2299); // zakres zgodny z systemem PESEL
        int month = Random.Next(1, 12);
        int day = Random.Next(1, DateTime.DaysInMonth(year, month));

        // zakodowanie stulecia w miesiącu
        int encodedMonth = month + (year / 100 - 19) * 20;

        string datePart = $"{year % 100:D2}{encodedMonth:D2}{day:D2}";
        string serial = Random.Next(1000, 9999).ToString("D4");

        // checksum
        string basePesel = datePart + serial;
        int[] weights = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };
        int sum = basePesel.Select((c, i) => (c - '0') * weights[i]).Sum();
        int control = (10 - (sum % 10)) % 10;

        return basePesel + control;
    }

    /// <summary>
    /// Generates a random valid NIP-VAT EU identifier.
    /// Regex used for validation:
    /// ^(?&lt;nip&gt;[1-9](\d[1-9]|[1-9]\d)\d{7})-(?&lt;vat&gt;((AT)(U\d{8})|(BE)([01]{1}\d{9})|(BG)(\d{9,10})|(CY)(\d{8}[A-Z])|(CZ)(\d{8,10})|(DE)(\d{9})|(DK)(\d{8})|(EE)(\d{9})|(EL)(\d{9})|(ES)([A-Z]\d{8}|\d{8}[A-Z]|[A-Z]\d{7}[A-Z])|(FI)(\d{8})|(FR)[A-Z0-9]{2}\d{9}|(HR)(\d{11})|(HU)(\d{8})|(IE)(\d{7}[A-Z]{2}|\d[A-Z0-9+*]\d{5}[A-Z])|(IT)(\d{11})|(LT)(\d{9}|\d{12})|(LU)(\d{8})|(LV)(\d{11})|(MT)(\d{8})|(NL)([A-Z0-9+*]{12})|(PT)(\d{9})|(RO)(\d{2,10})|(SE)(\d{12})|(SI)(\d{8})|(SK)(\d{10})|(XI)((\d{9}|(\d{12}))|(GD|HA)(\d{3}))))$
    /// </summary>
    internal static string GetRandomNipVatEU(string countryCode = "ES")
    {
        string nip = GetRandomNip();

        string vatPart = countryCode switch
        {
            "AT" => "U" + Random.Next(10000000, 99999999),
            "BE" => $"{Random.Next(0, 2)}{Random.Next(100000000, 999999999)}",
            "BG" => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            "CY" => $"{Random.Next(10000000, 99999999)}{(char)('A' + Random.Next(26))}",
            "CZ" => Random.Next(0, 2) == 0
                ? $"{Random.Next(10000000, 99999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            "DE" => $"{Random.Next(100000000, 999999999)}",
            "DK" => $"{Random.Next(10000000, 99999999)}",
            "EE" => $"{Random.Next(100000000, 999999999)}",
            "EL" => $"{Random.Next(100000000, 999999999)}",
            "ES" => GenerateEsVat(),
            "FI" => $"{Random.Next(10000000, 99999999)}",
            "FR" => $"{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 2)}{Random.Next(100000000, 999999999)}",
            "HR" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "HU" => $"{Random.Next(10000000, 99999999)}",
            "IE" => GenerateIeVat(),
            "IT" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "LT" => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(100000000000, 999999999999)}",
            "LU" => $"{Random.Next(10000000, 99999999)}",
            "LV" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "MT" => $"{Random.Next(10000000, 99999999)}",
            "NL" => RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+*", 12),
            "PT" => $"{Random.Next(100000000, 999999999)}",
            "RO" => $"{Random.NextInt64(10, 9999999999)}",
            "SE" => $"{Random.NextInt64(100000000000, 999999999999)}",
            "SI" => $"{Random.Next(10000000, 99999999)}",
            "SK" => $"{Random.NextInt64(1000000000, 9999999999)}",
            "XI" => GenerateXiVat(),
            _ => throw new ArgumentException($"Unsupported country code {countryCode}")
        };

        return $"{nip}-{countryCode}{vatPart}";
    }

    /// <summary>
    /// Generates a random valid NIP-VAT EU identifier.
    /// Regex used for validation:
    /// ^(?&lt;nip&gt;[1-9](\d[1-9]|[1-9]\d)\d{7})-(?&lt;vat&gt;((AT)(U\d{8})|(BE)([01]{1}\d{9})|(BG)(\d{9,10})|(CY)(\d{8}[A-Z])|(CZ)(\d{8,10})|(DE)(\d{9})|(DK)(\d{8})|(EE)(\d{9})|(EL)(\d{9})|(ES)([A-Z]\d{8}|\d{8}[A-Z]|[A-Z]\d{7}[A-Z])|(FI)(\d{8})|(FR)[A-Z0-9]{2}\d{9}|(HR)(\d{11})|(HU)(\d{8})|(IE)(\d{7}[A-Z]{2}|\d[A-Z0-9+*]\d{5}[A-Z])|(IT)(\d{11})|(LT)(\d{9}|\d{12})|(LU)(\d{8})|(LV)(\d{11})|(MT)(\d{8})|(NL)([A-Z0-9+*]{12})|(PT)(\d{9})|(RO)(\d{2,10})|(SE)(\d{12})|(SI)(\d{8})|(SK)(\d{10})|(XI)((\d{9}|(\d{12}))|(GD|HA)(\d{3}))))$
    /// </summary>
    internal static string GetRandomNipVatEU(string nip, string countryCode = "ES")
    {
        string vatPart = countryCode switch
        {
            "AT" => "U" + Random.Next(10000000, 99999999),
            "BE" => $"{Random.Next(0, 2)}{Random.Next(100000000, 999999999)}",
            "BG" => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            "CY" => $"{Random.Next(10000000, 99999999)}{(char)('A' + Random.Next(26))}",
            "CZ" => Random.Next(0, 2) == 0
                ? $"{Random.Next(10000000, 99999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            "DE" => $"{Random.Next(100000000, 999999999)}",
            "DK" => $"{Random.Next(10000000, 99999999)}",
            "EE" => $"{Random.Next(100000000, 999999999)}",
            "EL" => $"{Random.Next(100000000, 999999999)}",
            "ES" => GenerateEsVat(),
            "FI" => $"{Random.Next(10000000, 99999999)}",
            "FR" => $"{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 2)}{Random.Next(100000000, 999999999)}",
            "HR" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "HU" => $"{Random.Next(10000000, 99999999)}",
            "IE" => GenerateIeVat(),
            "IT" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "LT" => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(100000000000, 999999999999)}",
            "LU" => $"{Random.Next(10000000, 99999999)}",
            "LV" => $"{Random.NextInt64(10000000000, 99999999999)}",
            "MT" => $"{Random.Next(10000000, 99999999)}",
            "NL" => RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+*", 12),
            "PT" => $"{Random.Next(100000000, 999999999)}",
            "RO" => $"{Random.NextInt64(10, 9999999999)}",
            "SE" => $"{Random.NextInt64(100000000000, 999999999999)}",
            "SI" => $"{Random.Next(10000000, 99999999)}",
            "SK" => $"{Random.NextInt64(1000000000, 9999999999)}",
            "XI" => GenerateXiVat(),
            _ => throw new ArgumentException($"Unsupported country code {countryCode}")
        };

        return $"{nip}-{countryCode}{vatPart}";
    }

    // --- helpers ---
    private static string RandomString(string chars, int length) =>
        new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());

    private static string GenerateEsVat()
    {
        int choice = Random.Next(3);
        return choice switch
        {
            0 => $"{(char)('A' + Random.Next(26))}{Random.Next(10000000, 99999999)}",
            1 => $"{Random.Next(10000000, 99999999)}{(char)('A' + Random.Next(26))}",
            _ => $"{(char)('A' + Random.Next(26))}{Random.Next(1000000, 9999999)}{(char)('A' + Random.Next(26))}"
        };
    }

    private static string GenerateIeVat()
    {
        return Random.Next(2) == 0
            ? $"{Random.Next(1000000, 9999999)}{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 2)}"
            : $"{Random.Next(0, 9)}{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+*", 1)}{Random.Next(10000, 99999)}{(char)('A' + Random.Next(26))}";
    }

    private static string GenerateXiVat()
    {
        int choice = Random.Next(3);
        return choice switch
        {
            0 => $"{Random.Next(100000000, 999999999)}",
            1 => $"{Random.NextInt64(100000000000, 999999999999)}",
            _ => $"{(Random.Next(2) == 0 ? "GD" : "HA")}{Random.Next(100, 999)}"
        };
    }

}
