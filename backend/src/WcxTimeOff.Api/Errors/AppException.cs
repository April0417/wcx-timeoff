using System.Net;

namespace WcxTimeOff.Api.Errors;

public sealed class AppException : Exception
{
    public AppException(HttpStatusCode statusCode, string error, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Error = error;
    }

    public HttpStatusCode StatusCode { get; }
    public string Error { get; }
}
