using KSeF.Client.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KSeF.Client.Api.Services;

public sealed class CryptographyWarmupHostedService : IHostedService
{
    private readonly ICryptographyService _crypto;
    private readonly ILogger<CryptographyWarmupHostedService> _logger;

    public CryptographyWarmupHostedService(
        ICryptographyService crypto,
        ILogger<CryptographyWarmupHostedService> logger)
    {
        _crypto = crypto;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Jeśli warm-up nie wyjdzie, nie uruchamiamy aplikacji.
        _logger.LogInformation("Cryptography warm-up: start");
        await _crypto.WarmupAsync(cancellationToken);
        _logger.LogInformation("Cryptography warm-up: gotowy");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
