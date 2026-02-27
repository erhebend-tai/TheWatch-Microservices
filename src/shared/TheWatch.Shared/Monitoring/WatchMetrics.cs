using System.Diagnostics.Metrics;

namespace TheWatch.Shared.Monitoring;

/// <summary>
/// Centralized metrics definitions for all TheWatch services.
/// Each service registers its own meter; these are collected by Prometheus via OpenTelemetry.
/// Usage: inject IWatchMetrics and call the appropriate method from endpoint/service code.
/// </summary>
public interface IWatchMetrics
{
    /// <summary>Record an API request (auto-labels by service, endpoint, status).</summary>
    void RecordRequest(string service, string endpoint, int statusCode, double durationMs);

    /// <summary>Record an incident lifecycle event.</summary>
    void RecordIncident(string service, string incidentType, string action);

    /// <summary>Record a dispatch event with response time.</summary>
    void RecordDispatch(string service, double responseTimeMin, double distanceKm);

    /// <summary>Record a Kafka event published or consumed.</summary>
    void RecordKafkaEvent(string service, string topic, string direction);

    /// <summary>Record a health data point (vital reading, wearable heartbeat, etc.).</summary>
    void RecordHealthData(string service, string vitalType);

    /// <summary>Record an authentication event.</summary>
    void RecordAuthEvent(string service, string action, bool success);

    /// <summary>Record a mesh network message.</summary>
    void RecordMeshMessage(string service, string messageType, int hopCount);

    /// <summary>Record a compliance operation (HIPAA access, GDPR erasure, etc.).</summary>
    void RecordComplianceEvent(string service, string framework, string action);

    /// <summary>Set the current count of active connections (SignalR, WebSocket).</summary>
    void SetActiveConnections(string service, int count);

    /// <summary>Record database operation latency.</summary>
    void RecordDbOperation(string service, string operation, double durationMs);
}

public class WatchMetrics : IWatchMetrics
{
    private static readonly Meter Meter = new("TheWatch.Services", "1.0.0");

    // Request metrics
    private static readonly Counter<long> RequestCounter =
        Meter.CreateCounter<long>("thewatch_http_requests_total", "requests", "Total HTTP requests");
    private static readonly Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>("thewatch_http_request_duration_ms", "ms", "HTTP request duration in milliseconds");

    // Incident metrics
    private static readonly Counter<long> IncidentCounter =
        Meter.CreateCounter<long>("thewatch_incidents_total", "incidents", "Total incident events");

    // Dispatch metrics
    private static readonly Counter<long> DispatchCounter =
        Meter.CreateCounter<long>("thewatch_dispatches_total", "dispatches", "Total dispatch events");
    private static readonly Histogram<double> DispatchResponseTime =
        Meter.CreateHistogram<double>("thewatch_dispatch_response_time_min", "min", "Dispatch response time in minutes");
    private static readonly Histogram<double> DispatchDistance =
        Meter.CreateHistogram<double>("thewatch_dispatch_distance_km", "km", "Dispatch distance in kilometers");

    // Kafka metrics
    private static readonly Counter<long> KafkaEventCounter =
        Meter.CreateCounter<long>("thewatch_kafka_events_total", "events", "Total Kafka events");

    // Health data metrics
    private static readonly Counter<long> HealthDataCounter =
        Meter.CreateCounter<long>("thewatch_health_readings_total", "readings", "Total health data readings");

    // Auth metrics
    private static readonly Counter<long> AuthEventCounter =
        Meter.CreateCounter<long>("thewatch_auth_events_total", "events", "Total authentication events");

    // Mesh metrics
    private static readonly Counter<long> MeshMessageCounter =
        Meter.CreateCounter<long>("thewatch_mesh_messages_total", "messages", "Total mesh messages");
    private static readonly Histogram<double> MeshHopCount =
        Meter.CreateHistogram<double>("thewatch_mesh_hop_count", "hops", "Mesh message hop count");

    // Compliance metrics
    private static readonly Counter<long> ComplianceCounter =
        Meter.CreateCounter<long>("thewatch_compliance_events_total", "events", "Total compliance events");

    // Connection gauge
    private static readonly UpDownCounter<int> ActiveConnections =
        Meter.CreateUpDownCounter<int>("thewatch_active_connections", "connections", "Active real-time connections");

    // DB metrics
    private static readonly Histogram<double> DbOperationDuration =
        Meter.CreateHistogram<double>("thewatch_db_operation_duration_ms", "ms", "Database operation duration");

    public void RecordRequest(string service, string endpoint, int statusCode, double durationMs)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("service", service),
            new("endpoint", endpoint),
            new("status_code", statusCode),
            new("status_class", $"{statusCode / 100}xx")
        };
        RequestCounter.Add(1, tags);
        RequestDuration.Record(durationMs, tags);
    }

    public void RecordIncident(string service, string incidentType, string action)
    {
        IncidentCounter.Add(1,
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("incident_type", incidentType),
            new KeyValuePair<string, object?>("action", action));
    }

    public void RecordDispatch(string service, double responseTimeMin, double distanceKm)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("service", service)
        };
        DispatchCounter.Add(1, tags);
        DispatchResponseTime.Record(responseTimeMin, tags);
        DispatchDistance.Record(distanceKm, tags);
    }

    public void RecordKafkaEvent(string service, string topic, string direction)
    {
        KafkaEventCounter.Add(1,
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("topic", topic),
            new KeyValuePair<string, object?>("direction", direction));
    }

    public void RecordHealthData(string service, string vitalType)
    {
        HealthDataCounter.Add(1,
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("vital_type", vitalType));
    }

    public void RecordAuthEvent(string service, string action, bool success)
    {
        AuthEventCounter.Add(1,
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("action", action),
            new KeyValuePair<string, object?>("success", success));
    }

    public void RecordMeshMessage(string service, string messageType, int hopCount)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("service", service),
            new("message_type", messageType)
        };
        MeshMessageCounter.Add(1, tags);
        MeshHopCount.Record(hopCount, tags);
    }

    public void RecordComplianceEvent(string service, string framework, string action)
    {
        ComplianceCounter.Add(1,
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("framework", framework),
            new KeyValuePair<string, object?>("action", action));
    }

    public void SetActiveConnections(string service, int count)
    {
        ActiveConnections.Add(count,
            new KeyValuePair<string, object?>("service", service));
    }

    public void RecordDbOperation(string service, string operation, double durationMs)
    {
        DbOperationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("service", service),
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <summary>Get the static Meter instance for OpenTelemetry registration.</summary>
    public static string MeterName => Meter.Name;
}
