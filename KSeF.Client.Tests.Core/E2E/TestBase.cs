using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Tests.Core.Config;
using KSeF.Client.DI;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace KSeF.Client.Tests.Core.E2E;
public abstract class TestBase : IDisposable
{
    internal const int SleepTime = 2000;

    protected static readonly CancellationToken CancellationToken = CancellationToken.None;
    protected IKSeFClient KsefClient => _scope.ServiceProvider.GetRequiredService<IKSeFClient>();
    protected SignatureService SignatureService => _scope.ServiceProvider.GetRequiredService<SignatureService>();
    protected PersonTokenService TokenService => _scope.ServiceProvider.GetRequiredService<PersonTokenService>();
    protected ICryptographyService CryptographyService => _scope.ServiceProvider.GetRequiredService<CryptographyService>();

    private ServiceProvider _provider = default!;
    private IServiceScope _scope = default!;
    
    public TestBase()
    {
        ServiceCollection services = new ServiceCollection();

        ApiSettings apiSettings = TestConfig.GetApiSettings();

        var customHeadersFromSettings = TestConfig.Load()["ApiSettings:customHeaders"];
        if (!string.IsNullOrEmpty(customHeadersFromSettings))
        {
            apiSettings.CustomHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(customHeadersFromSettings);
        }

        services.AddKSeFClient(options =>
        {
            options.BaseUrl = apiSettings.BaseUrl!;
            options.CustomHeaders = apiSettings.CustomHeaders ?? new Dictionary<string, string>();
        });

        services.AddSingleton<SignatureService>();
        services.AddSingleton<PersonTokenService>();
        services.AddSingleton<CryptographyService>(sp => 
        {
            return new CryptographyService(async ct =>
            {
                using var scope = sp.CreateScope();
                var ksefCLient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
                return await ksefCLient.GetPublicCertificatesAsync(ct);
            });
        });
        services.AddHostedService<CryptographyWarmupHostedService>();

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // wymagane do pobrania certyfikatów przed uruchomieniem testów
        _provider.GetRequiredService<CryptographyService>().WarmupAsync(CancellationToken.None);

        _scope = _provider.CreateScope();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
