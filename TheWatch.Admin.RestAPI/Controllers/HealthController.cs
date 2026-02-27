using Microsoft.AspNetCore.Mvc;
using TheWatch.Contracts.CoreGateway;
using TheWatch.Contracts.VoiceEmergency;
using TheWatch.Contracts.MeshNetwork;
using TheWatch.Contracts.Wearable;
using TheWatch.Contracts.AuthSecurity;
using TheWatch.Contracts.FirstResponder;
using TheWatch.Contracts.FamilyHealth;
using TheWatch.Contracts.DisasterRelief;
using TheWatch.Contracts.DoctorServices;
using TheWatch.Contracts.Gamification;
using TheWatch.Contracts.Geospatial;
using TheWatch.Contracts.Surveillance;
using TheWatch.Contracts.Notifications;

namespace TheWatch.Admin.RestAPI.Controllers;

/// <summary>
/// Aggregated health check across all downstream services.
/// </summary>
[ApiController]
[Route("api/admin/health")]
public class HealthController(
    ICoreGatewayClient coreGateway,
    IVoiceEmergencyClient voiceEmergency,
    IMeshNetworkClient meshNetwork,
    IWearableClient wearable,
    IAuthSecurityClient authSecurity,
    IFirstResponderClient firstResponder,
    IFamilyHealthClient familyHealth,
    IDisasterReliefClient disasterRelief,
    IDoctorServicesClient doctorServices,
    IGamificationClient gamification,
    IGeospatialClient geospatial,
    ISurveillanceClient surveillance,
    INotificationsClient notifications,
    ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAggregatedHealth(CancellationToken ct)
    {
        var services = new Dictionary<string, string>();
        var checks = new (string Name, Func<Task> Check)[]
        {
            ("CoreGateway", async () => await coreGateway.GetServiceHealthAsync(ct)),
            ("VoiceEmergency", async () => await voiceEmergency.ListIncidentsAsync(1, 1, ct: ct)),
            ("MeshNetwork", async () => await meshNetwork.ListNodesAsync(ct)),
            ("Wearable", async () => await wearable.ListDevicesAsync(ct: ct)),
            ("AuthSecurity", async () => await authSecurity.GetUserInfoAsync(Guid.Empty, ct)),
            ("FirstResponder", async () => await firstResponder.ListRespondersAsync(1, 1, ct: ct)),
            ("FamilyHealth", async () => await familyHealth.ListGroupsAsync(ct)),
            ("DisasterRelief", async () => await disasterRelief.ListEventsAsync(1, 1, ct: ct)),
            ("DoctorServices", async () => await doctorServices.ListDoctorsAsync(1, 1, ct)),
            ("Gamification", async () => await gamification.GetLeaderboardAsync(1, ct)),
            ("Geospatial", async () => await geospatial.FindNearestRespondersAsync(0, 0, 1, ct: ct)),
            ("Surveillance", async () => await surveillance.ListCamerasAsync(1, 1, ct: ct)),
            ("Notifications", async () => await notifications.GetStatsAsync(ct))
        };

        await Parallel.ForEachAsync(checks, ct, async (check, token) =>
        {
            try
            {
                await check.Check();
                lock (services) services[check.Name] = "Healthy";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Health check failed for {Service}", check.Name);
                lock (services) services[check.Name] = "Unhealthy";
            }
        });

        var healthy = services.Count(s => s.Value == "Healthy");
        var unhealthy = services.Count(s => s.Value == "Unhealthy");

        return Ok(new
        {
            status = unhealthy == 0 ? "Healthy" : "Degraded",
            services,
            healthy,
            unhealthy,
            timestamp = DateTime.UtcNow
        });
    }
}
