using KSeF.Client.Core.Interfaces.Rest;
using KSeF.Client.Core.Infrastructure.Rest;

namespace KSeF.Client.Clients
{
    public abstract class ClientBase(IRestClient rest, IRouteBuilder routeBuilder)
    {
        protected readonly IRestClient _rest = rest;
        protected readonly IRouteBuilder _routes = routeBuilder;

        protected virtual Task ExecuteAsync(string relativeEndpoint, HttpMethod httpMethod, CancellationToken cancellationToken)
        {
            string path = _routes.Build(relativeEndpoint);
            RestRequest req = RestRequest
                .New(path, httpMethod);

            return _rest.ExecuteAsync(req, cancellationToken);
        }

        protected virtual Task ExecuteAsync<TRequest>(string relativeEndpoint, TRequest body, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(body);

            string path = _routes.Build(relativeEndpoint);
            RestRequest<TRequest> req = RestRequest
                .New(path, HttpMethod.Post)
                .WithBody(body);

            return _rest.ExecuteAsync<object, TRequest>(req, cancellationToken);
        }

        protected virtual Task ExecuteAsync<TRequest>(string relativeEndpoint, TRequest body, string accessToken, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(body);

            string path = _routes.Build(relativeEndpoint);
            RestRequest<TRequest> req = RestRequest
                .New(path, HttpMethod.Post)
                .WithBody(body)
                .AddAccessToken(accessToken);

            return _rest.ExecuteAsync<object, TRequest>(req, cancellationToken);
        }

        protected virtual Task<TResponse> ExecuteAsync<TResponse, TRequest>(string relativeEndpoint, TRequest body, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(body);

            string path = _routes.Build(relativeEndpoint);
            RestRequest<TRequest> req = RestRequest
                .New(path, HttpMethod.Post)
                .WithBody(body);

            return _rest.ExecuteAsync<TResponse, TRequest>(req, cancellationToken);
        }

        protected virtual Task<TResponse> ExecuteAsync<TResponse>(string relativeEndpoint, HttpMethod httpMethod, CancellationToken cancellationToken)
        {
            string path = _routes.Build(relativeEndpoint);
            RestRequest req = RestRequest
                .New(path, httpMethod);

            return _rest.ExecuteAsync<TResponse>(req, cancellationToken);
        }

        protected virtual Task<TResponse> ExecuteAsync<TResponse>(string relativeEndpoint, HttpMethod httpMethod, string accessToken, CancellationToken cancellationToken)
        {
            string path = _routes.Build(relativeEndpoint);
            RestRequest req = RestRequest
                .New(path, httpMethod)
                .AddAccessToken(accessToken);

            return _rest.ExecuteAsync<TResponse>(req, cancellationToken);
        }

        protected virtual Task ExecuteAsync(string relativeEndpoint, HttpMethod httpMethod, string accessToken, CancellationToken cancellationToken)
        {
            string path = _routes.Build(relativeEndpoint);
            RestRequest req = RestRequest
                .New(path, httpMethod)
                .AddAccessToken(accessToken);

            return _rest.ExecuteAsync(req, cancellationToken);
        }
    }
}