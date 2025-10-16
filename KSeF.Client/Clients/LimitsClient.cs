using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Tests;
using KSeF.Client.Core.Interfaces.Rest;

namespace KSeF.Client.Clients
{
    /// <inheritdoc />
    public sealed class LimitsClient: ClientBase, ILimitsClient
    {
        public LimitsClient(IRestClient rest, IRouteBuilder routeBuilder) : base(rest, routeBuilder)
        {
        }

        /// <inheritdoc />
        public Task<SessionLimitsInCurrentContextResponse> GetLimitsForCurrentContextAsync(string accessToken, CancellationToken cancellationToken = default) => 
            ExecuteAsync<SessionLimitsInCurrentContextResponse>(Routes.Limits.CurrentContext, HttpMethod.Get, accessToken, cancellationToken);

        /// <inheritdoc />
        public Task<CertificatesLimitInCurrentSubjectResponse> GetLimitsForCurrentSubjectAsync(string accessToken, CancellationToken cancellationToken = default) => 
            ExecuteAsync<CertificatesLimitInCurrentSubjectResponse>(Routes.Limits.CurrentSubject, HttpMethod.Get, accessToken, cancellationToken);
    }
}
