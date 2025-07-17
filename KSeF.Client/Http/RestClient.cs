using KSeF.Client.Http;
using KSeFClient.Core.Exceptions;
using KSeFClient.Core.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;


namespace KSeFClient.Http;

public class RestClient : IRestClient
{
    private readonly HttpClient httpClient;

    public const string DefaultContentType = "application/json";
    public const string XmlContentType = "application/xml";

    public RestClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<TResponse> SendAsync<TResponse, TRequest>(
        HttpMethod method,
        string url,
        TRequest requestBody = default,
        string bearerToken = null,
        string contentType = DefaultContentType,
        CancellationToken cancellationToken = default,
        Dictionary<string, string> additionalHeaders = default)
    {
        var request = new HttpRequestMessage(method, url);

        if (requestBody != null && method != HttpMethod.Get)
        {
            var requestContent = contentType == DefaultContentType ? JsonUtil.Serialize(requestBody) : requestBody.ToString();
            request.Content = new StringContent(requestContent!, Encoding.UTF8, contentType);
        }

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (additionalHeaders != null)
        {
            foreach (var header in additionalHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        Stream content = null;
        if (response.Content != null) content = await response.Content.ReadAsStreamAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new KsefApiException("Not found", response.StatusCode);
            }
            try
            {
                var error = await JsonUtil.DeserializeAsync<ApiErrorResponse>(content);
                if (error?.Exception?.ExceptionDetailList != null)
                {
                    var errorMessages = error.Exception.ExceptionDetailList.Select(detail =>
                        $"{detail.ExceptionCode}: {detail.ExceptionDescription} - {string.Join("; ", detail.Details ?? new List<string>())}");

                    var fullMessage = string.Join(" | ", errorMessages);
                    throw new KsefApiException(fullMessage, response.StatusCode, error.Exception.ServiceCode);
                }

                throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", response.StatusCode);
            }
            catch (JsonException)
            {
                throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", response.StatusCode);
            }
        }

        if (content == null || content.Length == 0)
            return default;

        var responseString = await response.Content.ReadAsStringAsync();

        if (typeof(TResponse) == typeof(string))
            return (TResponse)(object)responseString;

        return await JsonUtil.DeserializeAsync<TResponse>(content);
    }

    public Task SendAsync<TRequest>(
        HttpMethod method,
        string url,
        TRequest requestBody = default,
        string bearerToken = null,
        string contentType = DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<object, TRequest>(method, url, requestBody, bearerToken, contentType, cancellationToken);
    }

    public Task SendAsync(
        HttpMethod method,
        string url,
        string bearerToken = null,
        string contentType = DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<object>(method, url, null, bearerToken, contentType, cancellationToken);
    }
}

