using System.Net;

namespace CleanArchitecture.Application.Common.Exceptions;

public class PaymentException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public PaymentException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public PaymentException(string message, Exception innerException, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
