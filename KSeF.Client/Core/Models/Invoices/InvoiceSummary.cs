using KSeF.Client.Core.Models.Invoices.Common;
using KSeF.Client.Core.Models.Sessions;

namespace KSeF.Client.Core.Models.Invoices;

public class InvoiceSummary
{
    public string KsefNumber { get; set; }
    public string InvoiceNumber { get; set; }
    public DateTimeOffset InvoiceDate { get; set; }

    public DateTimeOffset IssueDate { get; set; }

    public DateTimeOffset InvoicingDate { get; set; }

    public DateTimeOffset AcquisitionDate { get; set; }
    public DateTimeOffset PermanentStorageDate { get; set; }
    public PartyInfo Seller { get; set; }
    public Buyer Buyer { get; set; }
    public decimal NetAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal VatAmount { get; set; }
    public string Currency { get; set; }
    public InvoicingMode InvoicingMode { get; set; }
    public InvoiceType InvoiceType { get; set; }
    public FormCode FormCode { get; set; }
    public bool IsSelfInvoicing { get; set; }
    public bool HasAttachment { get; set; }
}