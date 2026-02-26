using System.Collections.Concurrent;
using TheWatch.P7.FamilyHealth.Family;

namespace TheWatch.P7.FamilyHealth.Services;

public interface IVitalService
{
    Task<VitalReading> RecordAsync(Guid memberId, RecordVitalRequest request);
    Task<VitalHistory> GetHistoryAsync(Guid memberId, VitalType? type = null, int limit = 100);
    Task<List<MedicalAlert>> GetAlertsAsync(Guid? memberId = null, bool unacknowledgedOnly = false);
    Task<MedicalAlert?> AcknowledgeAlertAsync(Guid alertId);
}

public class VitalService : IVitalService
{
    private readonly ConcurrentDictionary<Guid, VitalReading> _readings = new();
    private readonly ConcurrentDictionary<Guid, MedicalAlert> _alerts = new();

    // Normal ranges for auto-alert generation
    private static readonly Dictionary<VitalType, (double Low, double High)> NormalRanges = new()
    {
        [VitalType.HeartRate] = (60, 100),
        [VitalType.Temperature] = (36.1, 37.2),
        [VitalType.SpO2] = (95, 100),
        [VitalType.RespiratoryRate] = (12, 20),
        [VitalType.BloodGlucose] = (70, 140),
    };

    public Task<VitalReading> RecordAsync(Guid memberId, RecordVitalRequest request)
    {
        var reading = new VitalReading
        {
            MemberId = memberId,
            Type = request.Type,
            Value = request.Value,
            Unit = request.Unit
        };

        _readings[reading.Id] = reading;

        // Auto-generate alert if out of normal range
        if (NormalRanges.TryGetValue(request.Type, out var range))
        {
            if (request.Value < range.Low || request.Value > range.High)
            {
                var severity = request.Value < range.Low * 0.8 || request.Value > range.High * 1.2
                    ? AlertSeverity.Critical
                    : AlertSeverity.Warning;

                var alert = new MedicalAlert
                {
                    MemberId = memberId,
                    AlertType = $"Abnormal{request.Type}",
                    Description = $"{request.Type} reading of {request.Value} is outside normal range ({range.Low}-{range.High})",
                    Severity = severity
                };
                _alerts[alert.Id] = alert;
            }
        }

        return Task.FromResult(reading);
    }

    public Task<VitalHistory> GetHistoryAsync(Guid memberId, VitalType? type, int limit)
    {
        var query = _readings.Values.Where(r => r.MemberId == memberId);

        if (type.HasValue)
            query = query.Where(r => r.Type == type.Value);

        var all = query.OrderByDescending(r => r.Timestamp).ToList();
        var readings = all.Take(limit).ToList();

        return Task.FromResult(new VitalHistory(readings, all.Count));
    }

    public Task<List<MedicalAlert>> GetAlertsAsync(Guid? memberId, bool unacknowledgedOnly)
    {
        var query = _alerts.Values.AsEnumerable();

        if (memberId.HasValue)
            query = query.Where(a => a.MemberId == memberId.Value);
        if (unacknowledgedOnly)
            query = query.Where(a => !a.Acknowledged);

        return Task.FromResult(query.OrderByDescending(a => a.CreatedAt).ToList());
    }

    public Task<MedicalAlert?> AcknowledgeAlertAsync(Guid alertId)
    {
        if (!_alerts.TryGetValue(alertId, out var alert))
            return Task.FromResult<MedicalAlert?>(null);

        alert.Acknowledged = true;
        return Task.FromResult<MedicalAlert?>(alert);
    }
}
