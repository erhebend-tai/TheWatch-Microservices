using System.Diagnostics;
using System.Security;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace TheWatch.Shared.Security;

/// <summary>
/// Production-grade global exception handler middleware for all TheWatch microservices.
/// Catches every unhandled exception and returns a structured RFC 9457 Problem Details
/// response with <c>application/problem+json</c> content type.
/// </summary>
/// <remarks>
/// <para>
/// This middleware must be registered <b>first</b> in the pipeline (before authentication,
/// routing, and all other middleware) so it can catch exceptions from any downstream component.
/// </para>
/// <para>
/// Exception-to-status-code mapping:
/// <list type="bullet">
///   <item><see cref="ArgumentException"/> / <see cref="ArgumentNullException"/> / <see cref="FormatException"/> — 400 Bad Request</item>
///   <item><see cref="UnauthorizedAccessException"/> / <see cref="SecurityException"/> — 401 Unauthorized</item>
///   <item><see cref="KeyNotFoundException"/> / <see cref="FileNotFoundException"/> — 404 Not Found</item>
///   <item><see cref="InvalidOperationException"/> — 409 Conflict</item>
///   <item><see cref="NotSupportedException"/> / <see cref="NotImplementedException"/> — 405 Method Not Allowed</item>
///   <item><see cref="TimeoutException"/> — 504 Gateway Timeout</item>
///   <item><see cref="OperationCanceledException"/> — 499 Client Closed Request</item>
///   <item>All other exceptions — 500 Internal Server Error</item>
/// </list>
/// </para>
/// <para>
/// Correlation ID: The middleware reads <c>X-Correlation-Id</c> from the incoming request.
/// If the header is absent, a new deterministic GUID is generated. The correlation ID is
/// always included in the response header and in the Problem Details payload so operators
/// can trace the error across distributed services.
/// </para>
/// <para>
/// Environment behavior:
/// <list type="bullet">
///   <item><b>Development</b> — full exception message, type name, and stack trace are included
///     in the response body for debugging convenience.</item>
///   <item><b>Production</b> — internal details are suppressed. For 5xx errors the detail
///     field contains only a generic message directing the caller to reference the correlation ID.</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Registration in <c>Program.cs</c>:
/// <code>
/// app.UseWatchExceptionHandler(); // must be first
/// app.UseAuthentication();
/// app.UseAuthorization();
/// </code>
/// </example>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;
    private readonly ILogger _logger;

    /// <summary>Correlation ID header name propagated across all TheWatch services.</summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of <see cref="GlobalExceptionHandlerMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="env">Hosting environment used to control detail exposure.</param>
    public GlobalExceptionHandlerMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _logger = Log.ForContext<GlobalExceptionHandlerMiddleware>();
    }

    /// <summary>
    /// Invokes the next middleware delegate. If an exception propagates, it is caught and
    /// converted to an RFC 9457 Problem Details JSON response.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Ensure a correlation ID is available for the entire request lifetime.
        var correlationId = EnsureCorrelationId(context);

        try
        {
            await _next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await HandleExceptionAsync(context, ex, correlationId);
        }
        catch (Exception ex) when (context.Response.HasStarted)
        {
            // The response stream has already been flushed to the client. We cannot modify
            // headers or body at this point. Log the error for server-side diagnostics only.
            _logger.Error(ex,
                "[EXCEPTION:RESPONSE_STARTED] Response already started. " +
                "CorrelationId={CorrelationId} Path={Path} Method={Method}",
                correlationId, context.Request.Path.Value, context.Request.Method);
        }
    }

    /// <summary>
    /// Reads or generates a correlation ID for the current request and ensures it is present
    /// in both the request and response headers.
    /// </summary>
    private static string EnsureCorrelationId(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existing)
            || string.IsNullOrWhiteSpace(existing))
        {
            existing = Guid.NewGuid().ToString("D");
            context.Request.Headers[CorrelationIdHeader] = existing!;
        }

        // Always echo the correlation ID back to the caller.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = existing!;
            return Task.CompletedTask;
        });

        return existing!;
    }

    /// <summary>
    /// Maps the caught exception to an HTTP status code, logs the full exception server-side,
    /// and writes an RFC 9457 Problem Details JSON response to the client.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        var (statusCode, title, typeUri) = MapException(exception);
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var isDevelopment = _env.IsDevelopment();

        // ------------------------------------------------------------------
        // Server-side logging: always log full details regardless of environment.
        // ------------------------------------------------------------------
        if (statusCode >= 500)
        {
            _logger.Error(exception,
                "[EXCEPTION:UNHANDLED] {StatusCode} {ExceptionType} " +
                "CorrelationId={CorrelationId} TraceId={TraceId} " +
                "Method={Method} Path={Path} Query={Query}",
                statusCode,
                exception.GetType().FullName,
                correlationId,
                traceId,
                context.Request.Method,
                context.Request.Path.Value,
                context.Request.QueryString.Value);
        }
        else
        {
            _logger.Warning(exception,
                "[EXCEPTION:HANDLED] {StatusCode} {ExceptionType} " +
                "CorrelationId={CorrelationId} TraceId={TraceId} " +
                "Method={Method} Path={Path}",
                statusCode,
                exception.GetType().FullName,
                correlationId,
                traceId,
                context.Request.Method,
                context.Request.Path.Value);
        }

        // ------------------------------------------------------------------
        // Build RFC 9457 Problem Details payload.
        // ------------------------------------------------------------------
        var problemDetails = new Dictionary<string, object?>
        {
            ["type"] = typeUri,
            ["title"] = title,
            ["status"] = statusCode,
            ["instance"] = context.Request.Path.Value,
            ["traceId"] = traceId,
            ["correlationId"] = correlationId
        };

        if (isDevelopment)
        {
            // Development: expose full details for developer convenience.
            problemDetails["detail"] = exception.Message;
            problemDetails["exceptionType"] = exception.GetType().FullName;
            problemDetails["stackTrace"] = exception.StackTrace;

            if (exception.InnerException is not null)
            {
                problemDetails["innerException"] = new Dictionary<string, object?>
                {
                    ["type"] = exception.InnerException.GetType().FullName,
                    ["message"] = exception.InnerException.Message,
                    ["stackTrace"] = exception.InnerException.StackTrace
                };
            }
        }
        else
        {
            // Production: suppress internal details for 5xx. For 4xx, the exception
            // message is typically safe because it describes a client error.
            problemDetails["detail"] = statusCode >= 500
                ? "An internal error occurred. Please reference the correlationId when contacting support."
                : exception.Message;
        }

        // ------------------------------------------------------------------
        // Write the response.
        // ------------------------------------------------------------------
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            problemDetails,
            JsonOptions,
            context.RequestAborted);
    }

    /// <summary>
    /// Maps a .NET exception type to an HTTP status code, human-readable title, and
    /// RFC 9110 type URI. The ordering matters: more specific exception types are matched
    /// before their base classes.
    /// </summary>
    private static (int StatusCode, string Title, string TypeUri) MapException(Exception exception)
    {
        return exception switch
        {
            // 400 Bad Request — client supplied invalid input
            ArgumentNullException =>
                (StatusCodes.Status400BadRequest,
                 "A required argument was not provided.",
                 "https://tools.ietf.org/html/rfc9110#section-15.5.1"),

            ArgumentOutOfRangeException =>
                (StatusCodes.Status400BadRequest,
                 "An argument was outside the allowable range.",
                 "https://tools.ietf.org/html/rfc9110#section-15.5.1"),

            ArgumentException =>
                (StatusCodes.Status400BadRequest,
                 "The request contains invalid arguments.",
                 "https://tools.ietf.org/html/rfc9110#section-15.5.1"),

            FormatException =>
                (StatusCodes.Status400BadRequest,
                 "The request contains a value in an invalid format.",
                 "https://tools.ietf.org/html/rfc9110#section-15.5.1"),

            // 401 Unauthorized — caller not authenticated
            UnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized,
                 "Authentication is required to access this resource.",
                 "https://tools.ietf.org/html/rfc9110#section-15.5.2"),

            SecurityException =>
                (StatusCodes.Status401Unauthorized,
                 "Authentication is required to access this resource.",
                 "https://tools.ietf.org/html/rfc9110#section-15.5.2"),

            // 403 Forbidden — authenticated but insufficient permissions
            // (captured here for completeness; authorization middleware typically handles this)

            // 404 Not Found — requested entity does not exist
            KeyNotFoundException =>
                (StatusCodes.Status404NotFound,
                 "The requested resource was not found.",
                 "https://tools.ietf.org/html/rfc9110#section-15.5.5"),

            FileNotFoundException =>
                (StatusCodes.Status404NotFound,
                 "The requested resource was not found.",
                 "https://tools.ietf.org/html/rfc9110#section-15.5.5"),

            // 405 Method Not Allowed — operation not supported
            NotSupportedException =>
                (StatusCodes.Status405MethodNotAllowed,
                 "The requested operation is not supported.",
                 "https://tools.ietf.org/html/rfc9110#section-15.5.6"),

            NotImplementedException =>
                (StatusCodes.Status405MethodNotAllowed,
                 "The requested operation is not yet implemented.",
                 "https://tools.ietf.org/html/rfc9110#section-15.5.6"),

            // 409 Conflict — request conflicts with current resource state
            InvalidOperationException =>
                (StatusCodes.Status409Conflict,
                 "The request conflicts with the current state of the resource.",
                 "https://tools.ietf.org/html/rfc9110#section-15.5.10"),

            // 499 Client Closed Request — caller cancelled
            OperationCanceledException =>
                (499, // nginx-style "Client Closed Request"
                 "The request was cancelled by the client.",
                 "https://tools.ietf.org/html/rfc9110#section-15.6.1"),

            // 504 Gateway Timeout — upstream dependency timed out
            TimeoutException =>
                (StatusCodes.Status504GatewayTimeout,
                 "A downstream service did not respond in time.",
                 "https://tools.ietf.org/html/rfc9110#section-15.6.5"),

            // 500 Internal Server Error — catch-all
            _ =>
                (StatusCodes.Status500InternalServerError,
                 "An unexpected error occurred.",
                 "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };
    }
}
