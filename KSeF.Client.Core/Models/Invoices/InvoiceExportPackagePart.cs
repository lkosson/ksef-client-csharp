using System;

namespace KSeF.Client.Core.Models.Invoices
{
    /// <summary>
    /// Część paczki eksportu faktur do pobrania.
    /// </summary>
    public class InvoiceExportPackagePart
{
    /// <summary>
    /// Numer porządkowy części.
    /// </summary>
    public int OrdinalNumber { get; set; }

    /// <summary>
    /// Nazwa części paczki.
    /// </summary>
    public string PartName { get; set; }

    /// <summary>
    /// Metoda HTTP do pobrania części (GET).
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    /// URL do pobrania części paczki.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Rozmiar niezaszyfrowanej części w bajtach.
    /// </summary>
    public long PartSize { get; set; }

    /// <summary>
    /// Hash niezaszyfrowanej części (Base64).
    /// </summary>
    public string PartHash { get; set; }

    /// <summary>
    /// Rozmiar zaszyfrowanej części w bajtach.
    /// </summary>
    public long EncryptedPartSize { get; set; }

    /// <summary>
    /// Hash zaszyfrowanej części (Base64).
    /// </summary>
    public string EncryptedPartHash { get; set; }

    /// <summary>
    /// Data wygaśnięcia URL do pobrania.
    /// </summary>
    public DateTimeOffset ExpirationDate { get; set; }
}
}
