using System.Net;

namespace TheWatch.Contracts.Abstractions;

/// <summary>
/// Typed exception for service call failures with HTTP context.
/// </summary>
public class ServiceClientException : Exception
{
    public string ServiceName { get; }
    public HttpStatusCode? StatusCode { get; }
    public string? ResponseBody { get; }
    public string? CorrelationId { get; }

    public ServiceClientException(string serviceName, string message)
        : base(message)
    {
        ServiceName = serviceName;
    }

    public ServiceClientException(string serviceName, string message, HttpStatusCode statusCode, string? responseBody = null, string? correlationId = null)
        : base(message)
    {
        ServiceName = serviceName;
        StatusCode = statusCode;
        ResponseBody = responseBody;
        CorrelationId = correlationId;
    }

    public ServiceClientException(string serviceName, string message, Exception innerException)
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }
}
