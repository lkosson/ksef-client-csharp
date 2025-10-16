namespace KSeF.Client.Core.Models
{
    public class OperationResponse
    {
        public string ReferenceNumber { get; set; }
        [System.Obsolete("OperationReferenceNumber jest przestarzały i zostanie usunięty. Zamiast tego użyj ReferenceNumber.", error: false)]
        public string OperationReferenceNumber { get; set; }
    }
}
