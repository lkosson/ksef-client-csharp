using KSeF.Client.Core.Models.Tests;
using System.Threading;
using System.Threading.Tasks;

namespace KSeF.Client.Core.Interfaces.Clients
{
    /// <summary>
    /// Operacje sekcji „Dane testowe” + pomocnicze „Query Grants” (weryfikacja efektów).
    /// </summary>
    public interface ILimitsClient
    {
        /// <summary>GET /api/v2/limits/context — pobierz aktualne limity dla bieżącego kontekstu.</summary>
        Task<SessionLimitsInCurrentContextResponse> GetLimitsForCurrentContextAsync(string accessToken, CancellationToken cancellationToken = default);

        /// <summary>GET /api/v2/limits/subject — pobierz aktualne limity dla bieżącego podmiotu.</summary>
        Task<CertificatesLimitInCurrentSubjectResponse> GetLimitsForCurrentSubjectAsync(string accessToken, CancellationToken cancellationToken = default);
    }
}
