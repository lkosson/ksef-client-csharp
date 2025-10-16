using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Http.Helpers;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KSeF.Client.Http;

/// <summary>
/// Generyczny klient REST obsługujący żądania GET, POST i DELETE z opcjonalną autoryzacją,
/// serializacją/deserializacją treści oraz strukturalną obsługą błędów.
/// </summary>
public sealed class RestClient : IRestClient
{
    private readonly HttpClient httpClient;
    
    public const string DefaultContentType = "application/json";
    public const string XmlContentType = "application/xml";

    public RestClient(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Wysyła żądanie HTTP i zwraca odpowiedź w postaci obiektu typu TResponse.
    /// </summary>
    public async Task<TResponse> SendAsync<TResponse, TRequest>(
        HttpMethod method,
        string url,
        TRequest requestBody = default,
        string bearerToken = null,
        string contentType = RestContentTypeExtensions.DefaultContentType,
        CancellationToken cancellationToken = default,
        Dictionary<string, string> additionalHeaders = null)
    {
        using HttpRequestMessage httpRequest = new HttpRequestMessage(method, url);

        if (!Equals(requestBody, default(TRequest)) && method != HttpMethod.Get)
        {
            string requestContent = RestContentTypeExtensions.IsDefaultType(contentType)
                ? JsonUtil.Serialize(requestBody)
                : requestBody != null ? requestBody.ToString() : null;
            if (requestContent != null)
            {
                httpRequest.Content = new StringContent(requestContent, Encoding.UTF8, contentType);
            }
        }

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (additionalHeaders != null)
        {
            foreach (KeyValuePair<string, string> header in additionalHeaders)
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        TResponse response = await SendCoreAsync<TResponse>(httpClient, httpRequest, cancellationToken).ConfigureAwait(false);
        return response;
    }

    /// <summary>
    /// Wysyła żądanie HTTP z podanym HttpContent, bez serializacji obiektów.
    /// Przeznaczone do ręcznego przesyłania danych binarnych (np. plików, strumieni).
    /// Nie modyfikuje zawartości ani nie zmienia nagłówków.
    /// </summary>
    public async Task SendAsync(
        HttpMethod method,
        string url,
        HttpContent content,
        IDictionary<string, string> additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        if (method == null) throw new ArgumentNullException(nameof(method));
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL cannot be null or empty.", nameof(url));
        if (content == null) throw new ArgumentNullException(nameof(content));

        using var request = new HttpRequestMessage(method, url)
        {
            Content = content
        };

        if (additionalHeaders != null)
        {
            foreach (var header in additionalHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        _ = await SendCoreAsync<object>(httpClient, request, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SendAsync<TRequest>(HttpMethod method, string url, TRequest requestBody = default, string token = null, string contentType = RestContentTypeExtensions.DefaultContentType, CancellationToken cancellationToken = default)
    {
        _ = await SendAsync<object, TRequest>(method, url, requestBody, token, contentType, cancellationToken, additionalHeaders: null).ConfigureAwait(false);
    }

    public async Task SendAsync(HttpMethod method, string url, string token = null, string contentType = RestContentTypeExtensions.DefaultContentType, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new HttpRequestMessage(method, url);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        _ = contentType;

        string responseText = await SendCoreAsync<string>(httpClient, request, cancellationToken).ConfigureAwait(false);
        _ = responseText;
    }

    // ================== RestRequest overloads ==================
    public async Task<TResponse> SendAsync<TResponse>(RestRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        using (HttpRequestMessage httpRequest = request.ToHttpRequestMessage(httpClient))
        using (CancellationTokenSource CancelationTokenSource = CreateTimeoutCancelationTokenSource(request.Timeout, cancellationToken))
        {
            TResponse response = await SendCoreAsync<TResponse>(httpClient, httpRequest, CancelationTokenSource.Token).ConfigureAwait(false);
            return response;
        }
    }

    /// <summary>
    /// Wysyła żądanie zdefiniowane przez <see cref="RestRequest"/> bez deserializacji zwróconego obiektu.
    /// </summary>
    public Task SendAsync(RestRequest request, CancellationToken cancellationToken = default)
        => SendAsync<object>(request, cancellationToken);

    public Task<TResponse> ExecuteAsync<TResponse>(RestRequest request, CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(request, cancellationToken);

    public Task ExecuteAsync(RestRequest request, CancellationToken cancellationToken = default)
        => SendAsync(request, cancellationToken);

    public Task<TResponse> ExecuteAsync<TResponse, TRequest>(RestRequest<TRequest> request, CancellationToken cancellationToken = default)
        => SendAsync<TResponse, TRequest>(request, cancellationToken);

    public async Task<TResponse> SendAsync<TResponse, TRequest>(RestRequest<TRequest> request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        using HttpRequestMessage httpRequest = request.ToHttpRequestMessage(httpClient, DefaultContentType);
        using CancellationTokenSource CancelationTokenSource = CreateTimeoutCancelationTokenSource(request.Timeout, cancellationToken);
        TResponse response = await SendCoreAsync<TResponse>(httpClient, httpRequest, CancelationTokenSource.Token).ConfigureAwait(false);
        return response;
    }

    public Task SendAsync<TRequest>(RestRequest<TRequest> request, CancellationToken cancellationToken = default)
        => SendAsync<object, TRequest>(request, cancellationToken);

    // ================== Core ==================
    private static async Task<T> SendCoreAsync<T>(HttpClient httpClient, HttpRequestMessage httpRequest, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient
            .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        bool hasContent = response.HasBody(httpRequest.Method);

        if (response.IsSuccessStatusCode)
        {
            if (!hasContent)
            {
                return default(T);
            }

            if (typeof(T) == typeof(object))
            {
                return default(T);
            }

            if (typeof(T) == typeof(string))
            {
                string okText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return (T)(object)(okText ?? string.Empty);
            }

            return await JsonUtil.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync());
        }
        else 
        {
            await HandleInvalidStatusCode(response, cancellationToken).ConfigureAwait(false);
            throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", response.StatusCode);
        }
    }

    private static async Task HandleInvalidStatusCode(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KsefApiException("Not found", response.StatusCode);
        }

        // Obsługa HTTP 429 "Too Many Requests" z dedykowanym wyjątkiem
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            string retryAfterHeader = null;

            // Próba pobrania "Retry-After" z różnych możliwych źródeł
            if (response.Headers.RetryAfter?.Delta != null)
            {
                retryAfterHeader = ((int)response.Headers.RetryAfter.Delta.Value.TotalSeconds).ToString();
            }
            else if (response.Headers.RetryAfter?.Date != null)
            {
                retryAfterHeader = response.Headers.RetryAfter.Date.Value.ToString("R");
            }
            else if (response.Headers.Contains("Retry-After"))
            {
                retryAfterHeader = response.Headers.GetValues("Retry-After").FirstOrDefault();
            }

            var message = "Przekroczono limit częstotliwości (HTTP 429)";

            try
            {
                string responseText = response.Content != null
                    ? await response.Content.ReadAsStringAsync(cancellationToken)
                    : null;

                if (!string.IsNullOrEmpty(responseText))
                {
                    var error = JsonUtil.Deserialize<ApiErrorResponse>(responseText);
                    if (error?.Exception?.ExceptionDetailList?.Any() == true)
                    {
                        var errorMessages = error
                            .Exception
                            .ExceptionDetailList
                            .Select(detail =>
                                $"{detail.ExceptionCode}: {detail.ExceptionDescription} - {string.Join("; ", detail.Details ?? new List<string>())}");
                        message = string.Join(" | ", errorMessages);
                    }

                    throw KsefRateLimitException.FromRetryAfterHeader(
                        message, retryAfterHeader, error?.Exception?.ServiceCode, error);
                }
            }
            catch (JsonException)
            {
                // Wycofanie do domyślnej obsługi
            }

            throw KsefRateLimitException.FromRetryAfterHeader(message, retryAfterHeader);
        }

        try
        {
            string responseText = response.Content != null
                ? await response.Content.ReadAsStringAsync(cancellationToken)
                : null;

            if (string.IsNullOrEmpty(responseText))
            {
                throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", response.StatusCode);
            }
            var error = JsonUtil.Deserialize<ApiErrorResponse>(responseText);
            string fullMessage = string.Empty;
            if (error?.Exception?.ExceptionDetailList?.Any() == true)
            {
                var errorMessages = error
                    .Exception
                    .ExceptionDetailList
                    .Select(detail =>
                        $"{detail.ExceptionCode}: {detail.ExceptionDescription} - {string.Join("; ", detail.Details ?? new List<string>())}");
                fullMessage = string.Join(" | ", errorMessages);
            }
            throw new KsefApiException(fullMessage, response.StatusCode, error?.Exception?.ServiceCode, error);
        }
        catch (JsonException e)
        {
            throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}, AdditionalInfo: {e.Message}", response.StatusCode);
        }
    }

    private static CancellationTokenSource CreateTimeoutCancelationTokenSource(TimeSpan? perRequestTimeout, CancellationToken cancellationToken)
    {
        if (perRequestTimeout is null)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        CancellationTokenSource CancelationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancelationTokenSource.CancelAfter(perRequestTimeout.Value);
        return CancelationTokenSource;
    }
}
