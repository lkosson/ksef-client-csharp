using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Tests.config;
using KSeFClient;
using KSeFClient.Http;
using Microsoft.Extensions.Configuration;


namespace KSeF.Client.Tests;

public class TestBase
{
    internal IKSeFClient ksefClient { get; private set; }

    internal string env = TestConfig.Load()["ApiSettings:BaseUrl"] ?? KSeFClient.DI.KsefEnviromentsUris.TEST;
    internal Dictionary<string, string> customHeaders = TestConfig.Load()
                .GetSection("ApiSettings:customHeaders")
                .Get<Dictionary<string, string>>()
              ?? new Dictionary<string, string>();

    internal ISignatureService signatureService { get; private set; }
    internal readonly HttpClient httpClientBase;
    internal readonly RestClient restClient;
    internal const int sleepTime = 500;
    internal TestBase()
    {
        signatureService = new SignatureService();
        HttpClientHandler handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        httpClientBase = new HttpClient(handler) { BaseAddress = new Uri(env) };

        if (customHeaders.Keys.Any())
        {
            foreach (var customHeader in customHeaders)
            {
                httpClientBase.DefaultRequestHeaders.Add(customHeader.Key, customHeader.Value);
            }
        }
        restClient = new RestClient(httpClientBase);

        ksefClient = new KSeFClient.Http.KSeFClient(
            restClient
        );
    }
}
