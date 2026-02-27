using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TheWatch.Shared.Security;

/// <summary>
/// RFC 9457 Problem Details middleware. Maps unhandled exceptions to standardized
/// <c>application/problem+json</c> responses. In production, no stack traces, type names,
/// or connection strings are ever exposed. Each response includes a <c>traceId</c> for
/// correlation with server-side logs.
/// </summary>
/// <remarks>
/// Register early in the pipeline so it catches exceptions from all downstream middleware:
/// <code>app.UseMiddleware&lt;WatchProblemDetailsMiddleware&gt;();</code>
/// </remarks>
public class WatchProblemDetailsMiddleware(
    RequestDelegate next,
    IHostEnvironment env,
    ILogger<WatchProblemDetailsMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Invokes the next middleware and catches any unhandled exception, converting it
    /// to an RFC 9457 Problem Details JSON response.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var isDevelopment = env.IsDevelopment();

        var (statusCode, title, type) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest,
                "One or more validation errors occurred.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1"),

            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,
                "Authentication is required to access this resource.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.2"),

            KeyNotFoundException => (StatusCodes.Status404NotFound,
                "The requested resource was not found.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"),

            InvalidOperationException => (StatusCodes.Status409Conflict,
                "The request conflicts with the current state of the resource.",
                "https://tools.ietf.org/html/rfc9110#section-15.5.10"),

            _ => (StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };

        // Always log the full exception server-side for diagnostics
        logger.LogError(exception,
            "[SEC:PROBLEM_DETAILS] {StatusCode} {ExceptionType} TraceId={TraceId} Path={Path}",
            statusCode, exception.GetType().Name, traceId, context.Request.Path);

        var problemDetails = new Dictionary<string, object?>
        {
            ["type"] = type,
            ["title"] = title,
            ["status"] = statusCode,
            ["traceId"] = traceId,
            ["instance"] = context.Request.Path.Value
        };

        // Add validation errors for FluentValidation exceptions
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
            problemDetails["errors"] = errors;
        }

        // In Development mode only, include exception details for debugging
        if (isDevelopment)
        {
            problemDetails["detail"] = exception.Message;
            problemDetails["exceptionType"] = exception.GetType().FullName;
            problemDetails["stackTrace"] = exception.StackTrace;
        }
        else
        {
            // Production: generic detail with no internal information
            problemDetails["detail"] = statusCode == StatusCodes.Status500InternalServerError
                ? "An internal error occurred. Please reference the traceId when contacting support."
                : exception.Message;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails, JsonOptions);
    }
}
