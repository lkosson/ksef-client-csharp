using KSeF.Client.ClientFactory;
using KSeF.Client.ClientFactory.DI;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace KSeF.Client.Tests.ClientFactory
{
    public class KSeFClientFactoryTests
    {
        [Fact]
        public async Task GivenKSefClientFromFacotry_WhenWeCallForAuthChallenge_ThenShouldReturn200()
        {
            // Arrange
            ServiceCollection services = new ServiceCollection();
            services.RegisterKSeFClientFactory();

            ServiceProvider provider = services.BuildServiceProvider();
            IKSeFClientFactory factory = provider.GetRequiredService<IKSeFClientFactory>();

            IKSeFClient testClient = factory.KSeFClient(Client.ClientFactory.Environment.Test);
            IKSeFClient demoClient = factory.KSeFClient(Client.ClientFactory.Environment.Demo);

            // Act
            AuthenticationChallengeResponse challengeOnTestEnvironmet = await testClient.GetAuthChallengeAsync();
            AuthenticationChallengeResponse challengeOnDemoEnvironmet = await demoClient.GetAuthChallengeAsync();

            // Assert
            Assert.NotNull(challengeOnTestEnvironmet);
            Assert.NotNull(challengeOnDemoEnvironmet);
        }
    }
}
