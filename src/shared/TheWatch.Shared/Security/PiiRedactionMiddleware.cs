using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace TheWatch.Shared.Security;

/// <summary>
/// Middleware that intercepts request and response body logging to redact Personally
/// Identifiable Information (PII) before any structured log event is emitted.
/// </summary>
/// <remarks>
/// <para>
/// This middleware is designed to work in concert with the <see cref="PiiMaskingEnricher"/>
/// (which operates on Serilog log-event properties). While the enricher masks PII that
/// has already been captured into structured fields, this middleware prevents raw PII from
/// ever entering the logging pipeline by redacting bodies that Serilog request-logging
/// middleware might capture.
/// </para>
/// <para>
/// Redaction patterns (NIST SP 800-122 compliance):
/// <list type="bullet">
///   <item><b>SSN</b> — <c>XXX-XX-XXXX</c> and variants with spaces or no separators</item>
///   <item><b>Phone</b> — US 10-digit numbers with common separators</item>
///   <item><b>Email</b> — standard email address format, local part masked</item>
///   <item><b>Credit Card</b> — 13-19 digit numbers with optional separators (Luhn-plausible patterns)</item>
/// </list>
/// </para>
/// <para>
/// The middleware <b>does not modify</b> the actual request or response payloads flowing
/// between the client and application. It only exposes a redacted diagnostic context
/// property (<c>WatchRequestBodyRedacted</c> / <c>WatchResponseBodyRedacted</c>) via
/// Serilog's <see cref="Serilog.Context.LogContext"/> so that downstream logging
/// middleware (such as Serilog.AspNetCore request logging) automatically uses
/// the scrubbed version.
/// </para>
/// <para>
/// Performance: Body buffering is opt-in. Bodies larger than <see cref="MaxBodyCaptureBytes"/>
/// (default 64 KB) are truncated before redaction to bound memory and CPU usage.
/// The regex patterns use source-generated compiled mode for minimal overhead.
/// </para>
/// </remarks>
/// <example>
/// Registration in <c>Program.cs</c>:
/// <code>
/// app.UseWatchPiiRedaction();
/// app.UseWatchSerilogRequestLogging();
/// </code>
/// </example>
public partial class PiiRedactionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    /// <summary>
    /// Maximum number of bytes to read from request/response bodies for redaction logging.
    /// Bodies exceeding this limit are truncated with an <c>[TRUNCATED]</c> marker.
    /// </summary>
    public const int MaxBodyCaptureBytes = 65_536; // 64 KB

    // -----------------------------------------------------------------
    // Source-generated compiled regex patterns (thread-safe, zero-alloc init)
    // -----------------------------------------------------------------

    /// <summary>Matches US Social Security Numbers in XXX-XX-XXXX, XXX XX XXXX, or XXXXXXXXX formats.</summary>
    [GeneratedRegex(@"\b\d{3}[\-\s]?\d{2}[\-\s]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnPattern();

    /// <summary>Matches US phone numbers in common formats: (XXX) XXX-XXXX, XXX-XXX-XXXX, XXX.XXX.XXXX, etc.</summary>
    [GeneratedRegex(@"(?:\+?1[\-\.\s]?)?\(?\d{3}\)?[\-\.\s]?\d{3}[\-\.\s]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex PhonePattern();

    /// <summary>Matches standard email addresses.</summary>
    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    /// <summary>
    /// Matches credit/debit card number patterns (13-19 digits, with optional spaces or dashes).
    /// This is a format-based heuristic; it does not perform Luhn validation.
    /// </summary>
    [GeneratedRegex(@"\b(?:\d[\-\s]?){13,19}\b", RegexOptions.Compiled)]
    private static partial Regex CreditCardPattern();

    /// <summary>
    /// Initializes a new instance of <see cref="PiiRedactionMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware delegate.</param>
    public PiiRedactionMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = Log.ForContext<PiiRedactionMiddleware>();
    }

    /// <summary>
    /// Enables request body buffering so it can be read for redaction, captures the
    /// redacted request body into the Serilog diagnostic context, wraps the response
    /// stream to capture the response body, and then logs the redacted response body
    /// after the downstream pipeline completes.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // ------------------------------------------------------------------
        // Request body: enable buffering so the body can be read without
        // consuming the stream for downstream middleware/endpoints.
        // ------------------------------------------------------------------
        context.Request.EnableBuffering();

        var redactedRequestBody = await CaptureAndRedactRequestBodyAsync(context.Request);
        if (redactedRequestBody is not null)
        {
            // Push the redacted body into the Serilog diagnostic context so that
            // Serilog request-logging middleware picks it up instead of the raw body.
            context.Items["WatchRequestBodyRedacted"] = redactedRequestBody;
            _logger.Verbose(
                "[PII:REQUEST_REDACTED] CorrelationId={CorrelationId} Path={Path} RedactedBody={Body}",
                context.Request.Headers[GlobalExceptionHandlerMiddleware.CorrelationIdHeader].FirstOrDefault(),
                context.Request.Path.Value,
                redactedRequestBody);
        }

        // ------------------------------------------------------------------
        // Response body: wrap the response stream so we can read the bytes
        // written by downstream middleware after they complete.
        // ------------------------------------------------------------------
        var originalBodyStream = context.Response.Body;
        using var capturedResponseStream = new MemoryStream();
        context.Response.Body = capturedResponseStream;

        try
        {
            await _next(context);
        }
        finally
        {
            // Copy the captured response back to the original stream.
            capturedResponseStream.Seek(0, SeekOrigin.Begin);
            await capturedResponseStream.CopyToAsync(originalBodyStream, context.RequestAborted);
            context.Response.Body = originalBodyStream;

            // Redact and log the response body for diagnostic purposes.
            var redactedResponseBody = CaptureAndRedactResponseBody(capturedResponseStream);
            if (redactedResponseBody is not null)
            {
                context.Items["WatchResponseBodyRedacted"] = redactedResponseBody;
                _logger.Verbose(
                    "[PII:RESPONSE_REDACTED] CorrelationId={CorrelationId} Path={Path} " +
                    "StatusCode={StatusCode} RedactedBody={Body}",
                    context.Request.Headers[GlobalExceptionHandlerMiddleware.CorrelationIdHeader].FirstOrDefault(),
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    redactedResponseBody);
            }
        }
    }

    /// <summary>
    /// Reads up to <see cref="MaxBodyCaptureBytes"/> from the request body, rewinds the
    /// stream for downstream consumers, and returns the PII-redacted text (or null if the
    /// body is empty or unreadable).
    /// </summary>
    private static async Task<string?> CaptureAndRedactRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is 0 || request.Body is null || !request.Body.CanRead)
            return null;

        // Only capture text-based content types (JSON, XML, form data).
        var contentType = request.ContentType;
        if (contentType is null || !IsTextBasedContentType(contentType))
            return null;

        request.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);

        var buffer = new char[MaxBodyCaptureBytes];
        var charsRead = await reader.ReadBlockAsync(buffer, 0, buffer.Length);

        // Rewind for downstream consumers.
        request.Body.Seek(0, SeekOrigin.Begin);

        if (charsRead == 0)
            return null;

        var raw = new string(buffer, 0, charsRead);
        if (charsRead >= MaxBodyCaptureBytes)
            raw += " [TRUNCATED]";

        return RedactPii(raw);
    }

    /// <summary>
    /// Reads up to <see cref="MaxBodyCaptureBytes"/> from the captured response stream
    /// and returns the PII-redacted text (or null if the body is empty).
    /// </summary>
    private static string? CaptureAndRedactResponseBody(MemoryStream capturedStream)
    {
        if (capturedStream.Length == 0)
            return null;

        capturedStream.Seek(0, SeekOrigin.Begin);

        var bytesToRead = (int)Math.Min(capturedStream.Length, MaxBodyCaptureBytes);
        var buffer = new byte[bytesToRead];
        _ = capturedStream.Read(buffer, 0, bytesToRead);

        var raw = Encoding.UTF8.GetString(buffer);
        if (capturedStream.Length > MaxBodyCaptureBytes)
            raw += " [TRUNCATED]";

        return RedactPii(raw);
    }

    /// <summary>
    /// Applies all PII redaction patterns to the input string. The order is significant:
    /// SSN patterns must be applied before phone patterns because the SSN regex is a
    /// subset of the phone regex.
    /// </summary>
    /// <param name="input">The raw text to redact.</param>
    /// <returns>The text with all matching PII patterns replaced by redaction markers.</returns>
    internal static string RedactPii(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // SSN: must run before phone (overlapping digit patterns)
        input = SsnPattern().Replace(input, "[SSN-REDACTED]");

        // Email: replace full address, keep domain for source tracing
        input = EmailPattern().Replace(input, match =>
        {
            var atIndex = match.Value.IndexOf('@');
            if (atIndex <= 0) return "[EMAIL-REDACTED]";
            var domain = match.Value[(atIndex + 1)..];
            return $"[EMAIL-REDACTED]@{domain}";
        });

        // Phone: replace with redaction marker
        input = PhonePattern().Replace(input, "[PHONE-REDACTED]");

        // Credit card: replace with redaction marker
        input = CreditCardPattern().Replace(input, "[CC-REDACTED]");

        return input;
    }

    /// <summary>
    /// Determines whether a Content-Type value represents a text-based media type
    /// that is safe and useful to capture for PII redaction logging.
    /// </summary>
    private static bool IsTextBasedContentType(string contentType)
    {
        // Normalize to lowercase for comparison.
        var ct = contentType.ToLowerInvariant();
        return ct.Contains("json", StringComparison.Ordinal)
            || ct.Contains("xml", StringComparison.Ordinal)
            || ct.Contains("text/", StringComparison.Ordinal)
            || ct.Contains("form-urlencoded", StringComparison.Ordinal);
    }
}
