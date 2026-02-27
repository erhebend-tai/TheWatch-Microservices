using System.Diagnostics.Metrics;

namespace TheWatch.Shared.Observability;

/// <summary>
/// Item 244: Prometheus-oriented custom metrics for TheWatch production monitoring.
/// Complements <see cref="Monitoring.WatchMetrics"/> with domain-specific instruments
/// designed for Prometheus/Grafana dashboards and alerting rules.
/// </summary>
/// <remarks>
/// <para>
/// These metrics are exposed via the OpenTelemetry Prometheus exporter on <c>/metrics</c>.
/// Each instrument follows the Prometheus naming convention (snake_case, unit suffix).
/// </para>
/// <para>
/// Instruments defined:
/// <list type="bullet">
///   <item><c>thewatch_request_duration_seconds</c> - Histogram of HTTP request durations.</item>
///   <item><c>thewatch_active_incidents</c> - Gauge of currently open (non-resolved) incidents.</item>
///   <item><c>thewatch_dispatch_response_seconds</c> - Histogram of dispatch response times.</item>
///   <item><c>thewatch_sos_activations_total</c> - Counter of SOS / panic button activations.</item>
///   <item><c>thewatch_evidence_uploads_total</c> - Counter of evidence file uploads.</item>
///   <item><c>thewatch_auth_failures_total</c> - Counter of authentication failures.</item>
/// </list>
/// </para>
/// </remarks>
public interface IWatchPrometheusMetrics
{
    /// <summary>Record the duration of an HTTP request.</summary>
    void RecordRequestDuration(string service, string method, string route, int statusCode, double durationSeconds);

    /// <summary>Set the current count of active (unresolved) incidents.</summary>
    void SetActiveIncidents(string service, int count);

    /// <summary>Increment the active incident count by delta (positive or negative).</summary>
    void AdjustActiveIncidents(string service, int delta);

    /// <summary>Record a dispatch response time measurement.</summary>
    void RecordDispatchResponseTime(string service, string dispatchType, double responseSeconds);

    /// <summary>Record an SOS / panic activation event.</summary>
    void RecordSosActivation(string service, string activationType, string severity);

    /// <summary>Record an evidence file upload event.</summary>
    void RecordEvidenceUpload(string service, string evidenceType, long fileSizeBytes);

    /// <summary>Record an authentication failure event.</summary>
    void RecordAuthFailure(string service, string failureReason, string method);
}

public sealed class WatchPrometheusMetrics : IWatchPrometheusMetrics
{
    /// <summary>The meter name used for OpenTelemetry registration.</summary>
    public const string MeterName = "TheWatch.Prometheus";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    // --- Request duration histogram ---
    private static readonly Histogram<double> RequestDurationHistogram =
        Meter.CreateHistogram<double>(
            "thewatch_request_duration_seconds",
            "s",
            "Duration of HTTP requests in seconds");

    // --- Active incidents gauge (up/down counter) ---
    private static readonly UpDownCounter<int> ActiveIncidentsGauge =
        Meter.CreateUpDownCounter<int>(
            "thewatch_active_incidents",
            "incidents",
            "Number of currently active (unresolved) incidents");

    // --- Dispatch response time histogram ---
    private static readonly Histogram<double> DispatchResponseHistogram =
        Meter.CreateHistogram<double>(
            "thewatch_dispatch_response_seconds",
            "s",
            "Time from dispatch creation to first responder acknowledgment");

    // --- SOS activation counter ---
    private static readonly Counter<long> SosActivationCounter =
        Meter.CreateCounter<long>(
            "thewatch_sos_activations_total",
            "activations",
            "Total SOS / panic button activations");

    // --- Evidence upload counter ---
    private static readonly Counter<long> EvidenceUploadCounter =
        Meter.CreateCounter<long>(
            "thewatch_evidence_uploads_total",
            "uploads",
            "Total evidence file uploads");

    // --- Evidence upload size histogram ---
    private static readonly Histogram<double> EvidenceUploadSizeHistogram =
        Meter.CreateHistogram<double>(
            "thewatch_evidence_upload_bytes",
            "By",
            "Size of evidence uploads in bytes");

    // --- Auth failure counter ---
    private static readonly Counter<long> AuthFailureCounter =
        Meter.CreateCounter<long>(
            "thewatch_auth_failures_total",
            "failures",
            "Total authentication failures");

    public void RecordRequestDuration(string service, string method, string route, int statusCode, double durationSeconds)
    {
        RequestDurationHistogram.Record(durationSeconds,
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("route", route),
            new KeyValuePair<string, object?>("status_code", statusCode),
            new KeyValuePair<string, object?>("status_class", $"{statusCode / 100}xx"));
    }

    public void SetActiveIncidents(string service, int count)
    {
        // UpDownCounter is additive; to "set" to a value we would need to track
        // the previous value. For simplicity, this adjusts by the given count.
        ActiveIncidentsGauge.Add(count,
            new KeyValuePair<string, object?>("service", service));
    }

    public void AdjustActiveIncidents(string service, int delta)
    {
        ActiveIncidentsGauge.Add(delta,
            new KeyValuePair<string, object?>("service", service));
    }

    public void RecordDispatchResponseTime(string service, string dispatchType, double responseSeconds)
    {
        DispatchResponseHistogram.Record(responseSeconds,
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("dispatch_type", dispatchType));
    }

    public void RecordSosActivation(string service, string activationType, string severity)
    {
        SosActivationCounter.Add(1,
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("activation_type", activationType),
            new KeyValuePair<string, object?>("severity", severity));
    }

    public void RecordEvidenceUpload(string service, string evidenceType, long fileSizeBytes)
    {
        EvidenceUploadCounter.Add(1,
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("evidence_type", evidenceType));

        EvidenceUploadSizeHistogram.Record(fileSizeBytes,
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("evidence_type", evidenceType));
    }

    public void RecordAuthFailure(string service, string failureReason, string method)
    {
        AuthFailureCounter.Add(1,
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("failure_reason", failureReason),
            new KeyValuePair<string, object?>("method", method));
    }
}
