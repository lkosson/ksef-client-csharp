using System;
using System.Collections.Generic;
using System.Net.Http;

namespace KSeF.Client.Core.Infrastructure.Rest
{
    public sealed class RestRequest : IRestRequest
    {
        public string Path { get; private set; }
        public HttpMethod Method { get; private set; }
        public string AccessToken { get; private set; }
        public string ContentType { get; private set; } 
        public string Accept { get; private set; }
        public IDictionary<string, string> Headers { get; private set; }
        public IDictionary<string, string> Query { get; private set; }        
        public TimeSpan? Timeout { get; private set; }
        public string ApiVersion { get; private set; }

        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(100);

        public static RestRequest New(string path, HttpMethod method) => new RestRequest(path, method);

        private RestRequest(string path, HttpMethod method)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (method == null) throw new ArgumentNullException(nameof(method));

            Path = path;
            Method = method;
            ContentType = RestContentType.Json.ToMime();
            Accept = null;
            AccessToken = null;
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Timeout = DefaultTimeout;
            ApiVersion = null;
        }

        // --- Fluent API (mutujące – C# 7.3) ---
        public RestRequest AddAccessToken(string accessToken) { AccessToken = accessToken; return this; }
        public RestRequest AddHeader(string name, string value)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(Headers, StringComparer.OrdinalIgnoreCase);
            d[name] = value; Headers = d; return this;
        }
        public RestRequest AddQueryParameter(string name, string value)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(Query, StringComparer.OrdinalIgnoreCase);
            d[name] = value; Query = d; return this;
        }
        public RestRequest WithAccept(string accept) { Accept = accept; return this; }
        public RestRequest WithTimeout(TimeSpan timeout) { Timeout = timeout; return this; }
        public RestRequest WithContentType(string contentType) { ContentType = contentType; return this; }
        public RestRequest WithApiVersion(string apiVersion) { ApiVersion = apiVersion; return this; }

        public RestRequest<TBody> WithBody<TBody>(TBody body, RestContentType contentType = RestContentType.Json)
        {
            if (object.Equals(body, null)) throw new ArgumentNullException(nameof(body));

            RestRequest<TBody> req = RestRequest<TBody>.New(Path, Method, body, contentType);

            if (!string.IsNullOrEmpty(AccessToken)) req.AddAccessToken(AccessToken);
            if (!string.IsNullOrEmpty(Accept)) req.WithAccept(Accept);
            if (!string.IsNullOrEmpty(ApiVersion)) req.WithApiVersion(ApiVersion);
            if (Timeout.HasValue) req.WithTimeout(Timeout.Value);

            foreach (KeyValuePair<string, string> kv in Headers) req.AddHeader(kv.Key, kv.Value);
            foreach (KeyValuePair<string, string> kv in Query) req.AddQueryParameter(kv.Key, kv.Value);

            return req;
        }
    }
}
