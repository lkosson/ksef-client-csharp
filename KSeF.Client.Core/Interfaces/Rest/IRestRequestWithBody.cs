namespace KSeF.Client.Core.Infrastructure.Rest
{
    public interface IRestRequestWithBody<T> : IRestRequest
    {
        T Body { get; }
    }
}
