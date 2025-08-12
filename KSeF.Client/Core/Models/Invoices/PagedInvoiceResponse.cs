
namespace KSeF.Client.Core.Models.Invoices;


public class PagedInvoiceResponse
{
    public int TotalCount { get; set; }
    public ICollection<InvoiceSummary> Invoices { get; set; }
}
