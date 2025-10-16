using KSeF.Client.Core.Infrastructure.Rest;

namespace KSeF.Client.Core.Interfaces.Rest
{
    public interface IRouteBuilder
    {
        string Build(string endpoint, string apiVersion = null);
        string Resolve(RestRequest request, string relativeEndpoint);
    }
}