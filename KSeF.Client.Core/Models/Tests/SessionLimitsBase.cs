namespace KSeF.Client.Core.Models.Tests
{
    public class SessionLimitsBase
    {
        public int MaxInvoiceSizeInMib { get; set; } = 0;
        public int MaxInvoiceSizeInMB { get; set; } = 0;
        public int MaxInvoiceWithAttachmentSizeInMib { get; set; } = 0;
        public int MaxInvoiceWithAttachmentSizeInMB { get; set; } = 0;
        public int MaxInvoices { get; set; } = 0;
    }
}
