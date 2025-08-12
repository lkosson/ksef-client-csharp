namespace KSeF.Client.Core.Models.Invoices.Common;
/// <summary>
/// Typ faktury (metadane).
/// </summary>
public enum InvoiceType
{
    Vat,      // Faktura podstawowa
    Zal,      // Faktura zaliczkowa
    Kor,      // Faktura korygująca
    Roz,      // Faktura rozliczeniowa
    Upr,      // Faktura uproszczona
    KorZal,   // Korygująca do faktury zaliczkowej
    KorRoz    // Korygująca do faktury rozliczeniowej
}
