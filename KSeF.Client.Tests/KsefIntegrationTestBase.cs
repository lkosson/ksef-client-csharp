using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Tests.Config;
using KSeF.Client.DI;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace KSeF.Client.Tests;

[Collection("KsefIntegration")]
public abstract class KsefIntegrationTestBase : IDisposable
{
    private ServiceProvider _provider = default!;
    internal const int SleepTime = 500;

    private IServiceScope _scope = default!;

    protected IKSeFClient KsefClient => _scope.ServiceProvider.GetRequiredService<IKSeFClient>();
    protected SignatureService SignatureService => _scope.ServiceProvider.GetRequiredService<SignatureService>();
    protected PersonTokenService TokenService => _scope.ServiceProvider.GetRequiredService<PersonTokenService>();
    protected ICryptographyService CryptographyService => _scope.ServiceProvider.GetRequiredService<CryptographyService>();
    protected IQrCodeService QRCodeService => _scope.ServiceProvider.GetRequiredService<QrCodeService>();
    protected IVerificationLinkService VerificationLinkService => _scope.ServiceProvider.GetRequiredService<VerificationLinkService>();

    public KsefIntegrationTestBase()
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
        services.AddSingleton<QrCodeService>();
        services.AddSingleton<VerificationLinkService>(sp =>
        {
            return new VerificationLinkService(new KSeFClientOptions { BaseUrl = KsefEnviromentsUris.TEST });
        });
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
        _provider.GetRequiredService<CryptographyService>().WarmupAsync(CancellationToken.None).GetAwaiter().GetResult();

        _scope = _provider.CreateScope();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
