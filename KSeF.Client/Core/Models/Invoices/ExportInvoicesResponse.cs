namespace KSeF.Client.Core.Models.Invoices;

public class ExportInvoicesResponse
{
    /// <summary>
    /// Numer referencyjny operacji eksportu, potrzebny do sprawdzenia statusu.
    /// </summary>
    public string OperationReferenceNumber { get; set; }

    /// <summary>
    /// Status inicjacji operacji (np. ACCEPTED).
    /// </summary>
    public string Status { get; set; }
}
