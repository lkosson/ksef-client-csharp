
using KSeF.Client.Core.Models.Sessions;

namespace KSeF.Client.Core.Models.Invoices;

public class AsyncQueryInvoiceRequest : InvoiceMetadataQueryRequest
{
   public EncryptionInfo Encryption { get; set; }
}
