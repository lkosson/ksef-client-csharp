using System.Net.Http.Headers;
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
namespace KSeF.Client.DI;

/// <summary>
/// Extension methods do rejestracji KSeF SDK w kontenerze DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje wszystkie potrzebne serwisy do korzystania z KSeF
    /// </summary>
    public static IServiceCollection AddKSeFClient(this IServiceCollection services, Action<KSeFClientOptions> configure)
    {
        var options = new KSeFClientOptions();
        configure(options);
        if (string.IsNullOrEmpty(options.BaseUrl))
            throw new ArgumentException("BaseUrl musi być poprawnym URL.", nameof(options.BaseUrl));

        services.AddSingleton(options);

        services
            .AddHttpClient<IRestClient, RestClient>(http =>
            {
                http.BaseAddress = new Uri(options.BaseUrl);
                if (options.CustomHeaders.Any())
                {
                    foreach (var header in options.CustomHeaders)
                        http.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
                http.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (options.WebProxy != null)
                {
                    handler.Proxy = options.WebProxy;
                    handler.UseProxy = true;
                }
                return handler;
            });

        services.AddScoped<IKSeFClient, Http.KSeFClient>();
        services.AddScoped<IAuthCoordinator, AuthCoordinator>();
        services.AddHostedService<CryptographyWarmupHostedService>();
        services.AddSingleton<ICryptographyService, CryptographyService>(sp =>
        {

            return new CryptographyService(async ct =>
            {
                using var scope = sp.CreateScope(); // krótki scope do utworzenia IKSeFClient
                var ksefCLient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
                return await ksefCLient.GetPublicCertificatesAsync(ct);
            });
        });
        
        
        services.AddScoped<ISignatureService, SignatureService>();
        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<IVerificationLinkService, VerificationLinkService>();

        services.AddLocalization(options =>
        {
            options.ResourcesPath = "Resources";
        });
        services.Configure<RequestLocalizationOptions>(opts =>
        {
            opts.SetDefaultCulture("pl-PL")
                .AddSupportedCultures("pl-PL", "en-US")
                .AddSupportedUICultures("pl-PL", "en-US");
        });

        return services;
    }
}
