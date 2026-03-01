using System.Net;

namespace Maui.CdvSchedule.Domain.Exceptions;

public sealed class CdvApiException : Exception
{
    public CdvApiException(string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }

    public bool IsUnauthorized =>
        StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
}
