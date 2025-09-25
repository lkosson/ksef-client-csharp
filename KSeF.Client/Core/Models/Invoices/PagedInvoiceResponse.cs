
namespace KSeF.Client.Core.Models.Invoices;


public class PagedInvoiceResponse
{
    public bool HasMore { get; set; }
    public ICollection<InvoiceSummary> Invoices { get; set; }
}
