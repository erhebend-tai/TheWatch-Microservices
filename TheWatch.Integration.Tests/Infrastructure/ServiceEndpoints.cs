namespace TheWatch.Integration.Tests.Infrastructure;

/// <summary>
/// Service endpoint URLs resolved from environment or docker-compose defaults.
/// Set environment variables to override for different environments.
/// </summary>
public static class ServiceEndpoints
{
    public static string AuthSecurity =>
        Environment.GetEnvironmentVariable("WATCH_AUTH_URL") ?? "http://localhost:5050";

    public static string VoiceEmergency =>
        Environment.GetEnvironmentVariable("WATCH_VOICE_URL") ?? "http://localhost:5020";

    public static string FirstResponder =>
        Environment.GetEnvironmentVariable("WATCH_RESPONDER_URL") ?? "http://localhost:5060";

    public static string FamilyHealth =>
        Environment.GetEnvironmentVariable("WATCH_FAMILY_URL") ?? "http://localhost:5070";

    public static string DoctorServices =>
        Environment.GetEnvironmentVariable("WATCH_DOCTOR_URL") ?? "http://localhost:5090";

    public static string DisasterRelief =>
        Environment.GetEnvironmentVariable("WATCH_DISASTER_URL") ?? "http://localhost:5080";

    public static string MeshNetwork =>
        Environment.GetEnvironmentVariable("WATCH_MESH_URL") ?? "http://localhost:5030";

    public static string Surveillance =>
        Environment.GetEnvironmentVariable("WATCH_SURVEILLANCE_URL") ?? "http://localhost:5110";

    public static string Geospatial =>
        Environment.GetEnvironmentVariable("WATCH_GEO_URL") ?? "http://localhost:5100";

    public static string CoreGateway =>
        Environment.GetEnvironmentVariable("WATCH_CORE_URL") ?? "http://localhost:5010";
}
