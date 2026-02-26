using TheWatch.P6.FirstResponder.Responders;

namespace TheWatch.P6.FirstResponder.Services;

/// <summary>
/// Responder dispatch optimization service.
/// Minimizes total response time by selecting the optimal combination of
/// available responders based on distance, type match, and capacity.
/// Uses a weighted scoring algorithm (not just nearest-N).
/// </summary>
public interface IDispatchOptimizationService
{
    /// <summary>
    /// Select the optimal set of responders for an incident.
    /// Returns a ranked, scored list of recommended responders.
    /// </summary>
    Task<DispatchPlan> OptimizeDispatchAsync(DispatchRequest request, IReadOnlyList<Responder> candidates);
}

public record DispatchRequest
{
    public Guid IncidentId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string EmergencyType { get; init; } = string.Empty;
    public int Severity { get; init; } = 3;
    public int RespondersNeeded { get; init; } = 8;
    public double MaxRadiusKm { get; init; } = 25.0;
}

public record DispatchPlan
{
    public Guid IncidentId { get; init; }
    public IReadOnlyList<ScoredResponder> RankedResponders { get; init; } = [];
    public double EstimatedResponseTimeMinutes { get; init; }
    public double CoverageRadiusKm { get; init; }
    public int TotalCandidatesEvaluated { get; init; }
    public DateTime ComputedAt { get; init; } = DateTime.UtcNow;
}

public record ScoredResponder
{
    public Guid ResponderId { get; init; }
    public string Name { get; init; } = string.Empty;
    public ResponderType Type { get; init; }
    public double DistanceKm { get; init; }
    public double EstimatedMinutes { get; init; }
    public double Score { get; init; }
    public string ScoreBreakdown { get; init; } = string.Empty;
}

public class DispatchOptimizationService : IDispatchOptimizationService
{
    private readonly ILogger<DispatchOptimizationService> _logger;

    // Scoring weights (sum = 1.0)
    private const double WeightDistance = 0.40;
    private const double WeightTypeMatch = 0.30;
    private const double WeightAvailability = 0.20;
    private const double WeightCertifications = 0.10;

    // Estimated response speed (km/h) by responder type
    private static readonly Dictionary<ResponderType, double> SpeedEstimates = new()
    {
        [ResponderType.Police] = 80,
        [ResponderType.Fire] = 60,
        [ResponderType.EMS] = 70,
        [ResponderType.SAR] = 40,
        [ResponderType.HazMat] = 50,
        [ResponderType.VolunteerMedic] = 50,
        [ResponderType.CommunityWatch] = 40,
        [ResponderType.Other] = 40
    };

    // Emergency type → preferred responder types
    private static readonly Dictionary<string, ResponderType[]> TypePreferences = new()
    {
        ["ActiveShooter"] = [ResponderType.Police, ResponderType.EMS],
        ["MedicalEmergency"] = [ResponderType.EMS, ResponderType.VolunteerMedic],
        ["Wildfire"] = [ResponderType.Fire, ResponderType.SAR],
        ["ChemicalHazard"] = [ResponderType.HazMat, ResponderType.Fire],
        ["Flood"] = [ResponderType.SAR, ResponderType.Fire],
        ["Earthquake"] = [ResponderType.SAR, ResponderType.EMS, ResponderType.Fire],
        ["TerroristThreat"] = [ResponderType.Police, ResponderType.EMS],
        ["Other"] = [ResponderType.Police, ResponderType.EMS, ResponderType.CommunityWatch]
    };

    public DispatchOptimizationService(ILogger<DispatchOptimizationService> logger)
    {
        _logger = logger;
    }

    public Task<DispatchPlan> OptimizeDispatchAsync(DispatchRequest request, IReadOnlyList<Responder> candidates)
    {
        var scored = new List<ScoredResponder>();

        var preferredTypes = TypePreferences.GetValueOrDefault(request.EmergencyType, [ResponderType.Police, ResponderType.EMS]);

        foreach (var responder in candidates)
        {
            if (responder.LastKnownLocation is null) continue;
            if (responder.Status != ResponderStatus.Available) continue;

            var distKm = HaversineDistance(
                request.Latitude, request.Longitude,
                responder.LastKnownLocation.Latitude, responder.LastKnownLocation.Longitude);

            if (distKm > request.MaxRadiusKm) continue;

            var speedKph = SpeedEstimates.GetValueOrDefault(responder.Type, 40);
            var etaMinutes = (distKm / speedKph) * 60;

            // Score components (0-1 each)
            var distScore = 1.0 - Math.Min(distKm / request.MaxRadiusKm, 1.0);
            var typeScore = preferredTypes.Contains(responder.Type) ? 1.0 :
                            Array.IndexOf(preferredTypes, responder.Type) >= 0 ? 0.7 : 0.3;
            var availScore = responder.Status == ResponderStatus.Available ? 1.0 : 0.2;
            var certScore = responder.Certifications.Count > 0 ? Math.Min(responder.Certifications.Count / 5.0, 1.0) : 0.1;

            // Severity multiplier: higher severity = more weight on distance (faster response)
            var severityBoost = request.Severity >= 4 ? 1.3 : 1.0;

            var totalScore = (distScore * WeightDistance * severityBoost +
                              typeScore * WeightTypeMatch +
                              availScore * WeightAvailability +
                              certScore * WeightCertifications);

            scored.Add(new ScoredResponder
            {
                ResponderId = responder.Id,
                Name = responder.Name,
                Type = responder.Type,
                DistanceKm = Math.Round(distKm, 2),
                EstimatedMinutes = Math.Round(etaMinutes, 1),
                Score = Math.Round(totalScore, 4),
                ScoreBreakdown = $"dist={distScore:F2}*{WeightDistance}, type={typeScore:F2}*{WeightTypeMatch}, avail={availScore:F2}*{WeightAvailability}, cert={certScore:F2}*{WeightCertifications}"
            });
        }

        var ranked = scored.OrderByDescending(s => s.Score).Take(request.RespondersNeeded).ToList();
        var plan = new DispatchPlan
        {
            IncidentId = request.IncidentId,
            RankedResponders = ranked,
            EstimatedResponseTimeMinutes = ranked.Count > 0 ? ranked.First().EstimatedMinutes : 0,
            CoverageRadiusKm = ranked.Count > 0 ? ranked.Max(r => r.DistanceKm) : 0,
            TotalCandidatesEvaluated = candidates.Count
        };

        _logger.LogInformation(
            "Dispatch optimization for incident {IncidentId}: {Selected}/{Candidates} responders selected, ETA={ETA:F1}min",
            request.IncidentId, ranked.Count, candidates.Count, plan.EstimatedResponseTimeMinutes);

        return Task.FromResult(plan);
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0; // Earth radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
