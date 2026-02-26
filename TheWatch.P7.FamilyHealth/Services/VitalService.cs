using Microsoft.EntityFrameworkCore;
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
    private readonly IWatchRepository<VitalReading> _readings;
    private readonly IWatchRepository<MedicalAlert> _alerts;

    // Normal ranges for auto-alert generation
    private static readonly Dictionary<VitalType, (double Low, double High)> NormalRanges = new()
    {
        [VitalType.HeartRate] = (60, 100),
        [VitalType.Temperature] = (36.1, 37.2),
        [VitalType.SpO2] = (95, 100),
        [VitalType.RespiratoryRate] = (12, 20),
        [VitalType.BloodGlucose] = (70, 140),
    };

    public VitalService(IWatchRepository<VitalReading> readings, IWatchRepository<MedicalAlert> alerts)
    {
        _readings = readings;
        _alerts = alerts;
    }

    public async Task<VitalReading> RecordAsync(Guid memberId, RecordVitalRequest request)
    {
        var reading = new VitalReading
        {
            MemberId = memberId,
            Type = request.Type,
            Value = request.Value,
            Unit = request.Unit
        };

        await _readings.AddAsync(reading);

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
                await _alerts.AddAsync(alert);
            }
        }

        return reading;
    }

    public async Task<VitalHistory> GetHistoryAsync(Guid memberId, VitalType? type, int limit)
    {
        var query = _readings.Query().Where(r => r.MemberId == memberId);

        if (type.HasValue)
            query = query.Where(r => r.Type == type.Value);

        var totalCount = await query.CountAsync();
        var readings = await query
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();

        return new VitalHistory(readings, totalCount);
    }

    public async Task<List<MedicalAlert>> GetAlertsAsync(Guid? memberId, bool unacknowledgedOnly)
    {
        var query = _alerts.Query();

        if (memberId.HasValue)
            query = query.Where(a => a.MemberId == memberId.Value);
        if (unacknowledgedOnly)
            query = query.Where(a => !a.Acknowledged);

        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
    }

    public async Task<MedicalAlert?> AcknowledgeAlertAsync(Guid alertId)
    {
        var alert = await _alerts.GetByIdAsync(alertId);
        if (alert is null) return null;

        alert.Acknowledged = true;
        await _alerts.UpdateAsync(alert);
        return alert;
    }
}
