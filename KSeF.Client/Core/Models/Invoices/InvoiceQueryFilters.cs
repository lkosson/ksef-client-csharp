
using System.ComponentModel.DataAnnotations;
using KSeF.Client.Core.Models.Invoices.Common;
using KSeF.Client.Core.Models.Invoices.Query;

namespace KSeF.Client.Core.Models.Invoices;

public class InvoiceQueryFilters
{
    [Required]
    public SubjectType SubjectType { get; set; }
    [Required]
    public DateRange DateRange { get; set; }
    public string KsefNumber { get; set; }
    public string InvoiceNumber { get; set; }
    public AmountFilter Amount { get; set; }
    public Seller Seller { get; set; }
    public Query.Buyer Buyer { get; set; }
    public List<CurrencyCode> CurrencyCodes { get; set; }
    public InvoicingMode? InvoicingMode { get; set; }
    public bool IsSelfInvoicing { get; set; }
    public FormType FormType { get; set; }
    public ICollection<InvoiceType> InvoiceTypes { get; set; }
    public bool HasAttachment { get; set; }
}
