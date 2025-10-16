namespace KSeF.Client.Http.Helpers;

internal static class HttpResponseMessageExtensions
{
    public static bool HasBody(this HttpResponseMessage httpResponseMessage, HttpMethod requestMethod)
    {
        if (requestMethod == HttpMethod.Head)
        {
            return false;
        }

        int statusCodeInt = (int)httpResponseMessage.StatusCode;
        if ((statusCodeInt >= 100 && statusCodeInt < 200) || statusCodeInt == 204 || statusCodeInt == 304)
        {
            return false;
        }

        if (httpResponseMessage.Content == null)
        {
            return false;
        }

        long? contentLength = httpResponseMessage.Content.Headers.ContentLength;
        if (contentLength.HasValue)
        {
            return contentLength.Value > 0;
        }

        return true;
    }
}
