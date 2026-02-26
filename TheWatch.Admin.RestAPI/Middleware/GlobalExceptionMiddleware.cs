using System.Net;
using System.Text.Json;
using TheWatch.Contracts.Abstractions;

namespace TheWatch.Admin.RestAPI.Middleware;

/// <summary>
/// Error normalization — Security+ Domain 2.2 (Injection Prevention), Domain 3.2 (Fail Secure).
/// Returns generic errors to clients. No stack traces, no internal service names, no implementation details.
/// ServiceClientExceptions get translated to appropriate status codes.
/// </summary>
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ServiceClientException ex)
        {
            var correlationId = context.Items.TryGetValue("CorrelationId", out var cid) ? cid as string : null;

            // Log full details server-side (Security+ 4.1 — Logging)
            logger.LogError(ex, "[SEC:SERVICE_CALL_FAILED] Service={Service} StatusCode={StatusCode} CorrelationId={CorrelationId} Path={Path}",
                ex.ServiceName, ex.StatusCode, correlationId, context.Request.Path);

            var statusCode = ex.StatusCode switch
            {
                HttpStatusCode.NotFound => StatusCodes.Status404NotFound,
                HttpStatusCode.BadRequest => StatusCodes.Status400BadRequest,
                HttpStatusCode.Unauthorized => StatusCodes.Status401Unauthorized,
                HttpStatusCode.Forbidden => StatusCodes.Status403Forbidden,
                HttpStatusCode.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
                HttpStatusCode.TooManyRequests => StatusCodes.Status429TooManyRequests,
                _ => StatusCodes.Status502BadGateway
            };

            // Security+ 2.2 — never leak internal service names to client
            var clientMessage = statusCode switch
            {
                StatusCodes.Status404NotFound => "The requested resource was not found.",
                StatusCodes.Status400BadRequest => "The request was invalid.",
                StatusCodes.Status401Unauthorized => "Authentication required.",
                StatusCodes.Status403Forbidden => "Access denied.",
                StatusCodes.Status503ServiceUnavailable => "Service temporarily unavailable. Please retry later.",
                StatusCodes.Status429TooManyRequests => "Too many requests. Please slow down.",
                _ => "A downstream error occurred. Please try again later."
            };

            // Security+ 2.3 — Retry-After on 429
            if (statusCode == StatusCodes.Status429TooManyRequests)
            {
                context.Response.Headers["Retry-After"] = "60";
            }

            await WriteErrorResponse(context, statusCode, clientMessage, correlationId);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("[SEC:CLIENT_DISCONNECT] Path={Path}", context.Request.Path);
        }
        catch (BadHttpRequestException ex)
        {
            // Security+ 2.1 — request size exceeded, malformed requests
            var correlationId = context.Items.TryGetValue("CorrelationId", out var cid) ? cid as string : null;
            logger.LogWarning(ex, "[SEC:BAD_REQUEST] Path={Path} CorrelationId={CorrelationId}", context.Request.Path, correlationId);

            await WriteErrorResponse(context, StatusCodes.Status400BadRequest,
                "The request was malformed or exceeded size limits.", correlationId);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items.TryGetValue("CorrelationId", out var cid) ? cid as string : null;

            // Security+ 4.1 — log full exception server-side for incident response
            logger.LogError(ex, "[SEC:UNHANDLED_EXCEPTION] Method={Method} Path={Path} CorrelationId={CorrelationId}",
                context.Request.Method, context.Request.Path, correlationId);

            // Security+ 3.2 — fail secure, generic message only
            await WriteErrorResponse(context, StatusCodes.Status500InternalServerError,
                "An internal error occurred. Please try again later.", correlationId);
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string message, string? correlationId)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            message,
            correlationId,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
