namespace KSeF.Client.Tests.Core.Utils.RateLimit;

/// <summary>
/// Definiuje typy adresów URL - KSeF API
/// </summary>
public enum KsefApiEndpoint
{
    InvoiceQueryMetadata,
    InvoiceExport,
    InvoiceGetByNumber,
    SessionBatchOpen,
    SessionBatchClose,
    SessionOnlineOpen,
    SessionOnlineSendInvoice,
    SessionOnlineClose,
    SessionInvoiceStatus,
    Other
}
