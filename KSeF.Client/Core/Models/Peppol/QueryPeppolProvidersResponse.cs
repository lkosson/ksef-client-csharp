namespace KSeF.Client.Core.Models.Peppol
{
    public class QueryPeppolProvidersResponse
    {
        /// <summary>
        /// Lista dostawców usług Peppol.
        /// </summary>
        public List<PeppolProvider> PeppolProviders { get; set; } = new();

        /// <summary>
        /// Flaga informująca o dostępności kolejnej strony wyników.
        /// </summary>
        public bool HasMore { get; set; }
    }
}
