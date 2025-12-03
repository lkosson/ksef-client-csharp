using KSeF.Client.ClientFactory;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.ClientFactory.DI;
using Microsoft.Extensions.DependencyInjection;

namespace KSeF.Client.Tests.ClientFactory
{
    public class CertificateFetcherFactoryTests
    {
        private readonly IKSeFFactoryCertificateFetcherServices _factory;

        public CertificateFetcherFactoryTests()
        {
            ServiceCollection services = new ServiceCollection();
            services.RegisterKSeFClientFactory();
            _factory = services.BuildServiceProvider()
                .GetRequiredService<IKSeFFactoryCertificateFetcherServices>();
        }

        [Fact]
        public async Task CertificateFetcher_IsCached_PerEnvironment()
        {
            ICertificateFetcher first = await _factory.GetOrSetCertificateFetcher(Client.ClientFactory.Environment.Demo);
            ICertificateFetcher second = await _factory.GetOrSetCertificateFetcher(Client.ClientFactory.Environment.Demo);

            Assert.Same(first, second);
        }

        [Fact]
        public async Task CertificateFetcher_IsDifferent_ForDifferentEnvironments()
        {
            ICertificateFetcher demo = await _factory.GetOrSetCertificateFetcher(Client.ClientFactory.Environment.Demo);
            ICertificateFetcher prod = await _factory.GetOrSetCertificateFetcher(Client.ClientFactory.Environment.Prod);

            Assert.NotSame(demo, prod);
        }
    }
}
