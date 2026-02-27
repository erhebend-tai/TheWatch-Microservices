using Hangfire;
using Hangfire.InMemory;
using Hangfire.Batches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using TheWatch.P11.Surveillance;
using TheWatch.P11.Surveillance.Surveillance;
using TheWatch.P11.Surveillance.Services;
using TheWatch.Shared.Contracts;
using TheWatch.Shared.Events;
using TheWatch.Shared.ML;
using TheWatch.Shared.Security;
using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.CoreGateway;
using TheWatch.Contracts.VoiceEmergency;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.AddWatchKafka();

// CORS (SignalR — requires AllowCredentials)
builder.Services.AddWatchCors(builder.Configuration, requiresSignalR: true);

// SignalR real-time hubs (CameraHub, DetectionHub, FootageSubmissionHub)
builder.Services.AddWatchSignalR();

// Hangfire with InMemory storage + Pro batches
builder.Services.AddHangfire(config =>
    config
        .UseInMemoryStorage()
        .UseBatches());
builder.Services.AddHangfireServer();

// Security (JWT validation + rate limiting + audit from SecurityGenerator)
builder.AddWatchSecurity();

// ML.NET Object Detector (singleton — loads ONNX model once)
var localDetector = new OnnxObjectDetector(builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger<OnnxObjectDetector>());

// Multi-cloud object detection with fallback (Local ONNX → Azure → GCP → AWS)
builder.Services.Configure<CloudObjectDetectorOptions>(
    builder.Configuration.GetSection(CloudObjectDetectorOptions.SectionName));
builder.Services.AddSingleton<IObjectDetector>(sp =>
    new ResilientObjectDetector(
        localDetector,
        sp.GetServices<ICloudObjectDetector>(),
        sp.GetRequiredService<IOptions<CloudObjectDetectorOptions>>(),
        sp.GetRequiredService<ILogger<ResilientObjectDetector>>()));

// Domain Services
builder.Services.AddScoped<ICameraService, CameraService>();
builder.Services.AddScoped<IFootageService, FootageService>();
builder.Services.AddScoped<ICrimeLocationService, CrimeLocationService>();
builder.Services.AddScoped<IVideoAnalysisService, VideoAnalysisService>();
builder.Services.AddScoped<IObjectTrackingService, ObjectTrackingService>();
builder.AddWatchControllers();
builder.Services.AddScoped<IWatchDataSeeder, TheWatch.P11.Surveillance.Data.Seeders.SurveillanceSeeder>();

// Item 216: Contract client wiring — typed inter-service clients with Polly resilience
builder.Services.AddWatchClientHandlers();

// ICoreGatewayClient — user profile lookups for camera owner verification
builder.Services.AddCoreGatewayClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:CoreGateway"] ?? "https+http://p1-coregateway");

// IVoiceEmergencyClient — link surveillance footage to active incidents
builder.Services.AddVoiceEmergencyClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:VoiceEmergency"] ?? "https+http://p2-voiceemergency");

builder.AddWatchControllers();

var app = builder.Build();
await app.UseWatchMigrations();

app.UseCors();
app.UseWatchSecurity();
app.UseWatchSerilogRequestLogging();
app.UseWatchOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = [new HangfireDashboardAuthFilter()],
    IsReadOnlyFunc = _ => true
});
app.MapWatchControllers();

// Register recurring Hangfire jobs
RecurringJob.AddOrUpdate<IVideoAnalysisService>(
    "footage-analysis-pipeline",
    svc => svc.ProcessQueuedFootageAsync(),
    "* * * * *"); // Every minute

RecurringJob.AddOrUpdate<ICameraService>(
    "camera-health-check",
    svc => svc.HealthCheckCamerasAsync(),
    "*/30 * * * *"); // Every 30 minutes

RecurringJob.AddOrUpdate<IFootageService>(
    "stale-footage-cleanup",
    svc => svc.ArchiveAnalyzedFootageAsync(TimeSpan.FromDays(30)),
    "0 3 * * *"); // Daily at 3 AM

// Health endpoint
app.MapGet("/health", () => new HealthResponse(
    "TheWatch.P11.Surveillance",
    "P11",
    "Healthy",
    DateTime.UtcNow));

// Service info
app.MapGet("/info", () => new
{
    Service = "TheWatch.P11.Surveillance",
    Program = "P11",
    Name = "Surveillance",
    Description = "Crime CCTV surveillance, object recognition, community footage network",
    Icon = "surveillance",
    Version = "0.1.0"
});

// === Camera Endpoints ===

app.MapPost("/api/cameras", async (RegisterCameraRequest request, ICameraService svc, IEventPublisher events) =>
{
    var camera = await svc.RegisterCameraAsync(request);
    return Results.Created($"/api/cameras/{camera.Id}", camera);
});

app.MapGet("/api/cameras", async (
    ICameraService svc,
    int? page,
    int? pageSize,
    CameraStatus? status) =>
{
    var result = await svc.ListCamerasAsync(page ?? 1, pageSize ?? 20, status);
    return Results.Ok(result);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/cameras/{id:guid}", async (Guid id, ICameraService svc) =>
{
    var camera = await svc.GetCameraAsync(id);
    return camera is not null ? Results.Ok(camera) : Results.NotFound();
}).RequireAuthorization("Authenticated");

app.MapPut("/api/cameras/{id:guid}/verify", async (Guid id, ICameraService svc) =>
{
    var camera = await svc.VerifyCameraAsync(id);
    return camera is not null ? Results.Ok(camera) : Results.NotFound();
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/cameras/{id:guid}", async (Guid id, ICameraService svc) =>
{
    var result = await svc.DeactivateCameraAsync(id);
    return result ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization("AdminOnly");

// === Footage Endpoints ===

app.MapPost("/api/footage", async (SubmitFootageRequest request, IFootageService svc, IEventPublisher events) =>
{
    var footage = await svc.SubmitFootageAsync(request);

    // Publish FootageSubmitted event to Kafka for analysis pipeline
    await events.PublishAsync(WatchTopics.FootageSubmitted, footage.Id.ToString(), new FootageSubmittedEvent
    {
        SourceService = "TheWatch.P11.Surveillance",
        FootageId = footage.Id,
        CameraId = footage.CameraId,
        GpsLatitude = footage.GpsLatitude,
        GpsLongitude = footage.GpsLongitude,
        SubmitterId = footage.SubmitterId
    });

    return Results.Created($"/api/footage/{footage.Id}", footage);
});

app.MapGet("/api/footage", async (
    IFootageService svc,
    int? page,
    int? pageSize,
    FootageStatus? status) =>
{
    var result = await svc.ListFootageAsync(page ?? 1, pageSize ?? 20, status);
    return Results.Ok(result);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/footage/{id:guid}", async (Guid id, IFootageService svc) =>
{
    var footage = await svc.GetFootageAsync(id);
    return footage is not null ? Results.Ok(footage) : Results.NotFound();
}).RequireAuthorization("Authenticated");

app.MapGet("/api/footage/{id:guid}/detections", async (Guid id, IFootageService svc, int? page, int? pageSize) =>
{
    var result = await svc.GetDetectionsForFootageAsync(id, page ?? 1, pageSize ?? 50);
    return Results.Ok(result);
}).RequireAuthorization("ResponderAccess");

// === Crime Location Endpoints ===

app.MapPost("/api/crime-locations", async (ReportCrimeLocationRequest request, ICrimeLocationService svc, IEventPublisher events) =>
{
    var crimeLocation = await svc.ReportCrimeLocationAsync(request);

    // Publish CrimeLocationReported event to Kafka
    await events.PublishAsync(WatchTopics.CrimeLocationReported, crimeLocation.Id.ToString(), new CrimeLocationReportedEvent
    {
        SourceService = "TheWatch.P11.Surveillance",
        CrimeLocationId = crimeLocation.Id,
        Latitude = crimeLocation.Latitude,
        Longitude = crimeLocation.Longitude,
        CrimeType = crimeLocation.CrimeType,
        ReporterId = crimeLocation.ReporterId
    });

    return Results.Created($"/api/crime-locations/{crimeLocation.Id}", crimeLocation);
});

app.MapGet("/api/crime-locations", async (
    ICrimeLocationService svc,
    int? page,
    int? pageSize,
    bool? activeOnly) =>
{
    var result = await svc.ListCrimeLocationsAsync(page ?? 1, pageSize ?? 20, activeOnly ?? true);
    return Results.Ok(result);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/crime-locations/{id:guid}", async (Guid id, ICrimeLocationService svc) =>
{
    var crimeLocation = await svc.GetCrimeLocationAsync(id);
    return crimeLocation is not null ? Results.Ok(crimeLocation) : Results.NotFound();
}).RequireAuthorization("Authenticated");

app.MapGet("/api/crime-locations/{id:guid}/footage", async (Guid id, ICrimeLocationService svc, double? radiusKm) =>
{
    var footage = await svc.GetFootageNearCrimeLocationAsync(id, radiusKm ?? 2.0);
    return Results.Ok(footage);
}).RequireAuthorization("ResponderAccess");

// === Surveillance Search ===

app.MapPost("/api/surveillance/search", async (SurveillanceSearchRequest request, IFootageService footageSvc) =>
{
    var footage = await footageSvc.FindFootageNearLocationAsync(
        request.Latitude, request.Longitude, request.RadiusKm,
        request.TimeWindowStart, request.TimeWindowEnd);

    var results = footage.Select(f => new SurveillanceSearchResult(f, [], 0)).ToList();
    return Results.Ok(results);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/surveillance/stats", async (
    IWatchRepository<CameraRegistration> cameras,
    IWatchRepository<FootageSubmission> footage,
    IWatchRepository<DetectionResult> detections,
    IWatchRepository<CrimeLocation> crimeLocations) =>
{
    var stats = new SurveillanceStats(
        TotalCameras: await cameras.Query().CountAsync(),
        VerifiedCameras: await cameras.Query().CountAsync(c => c.Status == CameraStatus.Verified || c.Status == CameraStatus.Active),
        TotalFootageSubmissions: await footage.Query().CountAsync(),
        AnalyzedFootage: await footage.Query().CountAsync(f => f.Status == FootageStatus.Analyzed),
        TotalDetections: await detections.Query().CountAsync(),
        ActiveCrimeLocations: await crimeLocations.Query().CountAsync(c => c.IsActive));

    return Results.Ok(stats);
}).RequireAuthorization("AdminOnly");

// === Object Tracking (multi-cloud ML backup) ===

app.MapPost("/api/object-tracking", async (ObjectTrackingRequest request, IObjectTrackingService svc) =>
{
    var result = await svc.TrackObjectsAsync(request);
    return result.Status == TrackingStatus.Failed && result.TrackingSessionId == Guid.Empty
        ? Results.NotFound(new { message = "Crime location not found" })
        : Results.Created($"/api/object-tracking/{result.TrackingSessionId}", result);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/object-tracking", async (
    IObjectTrackingService svc,
    Guid? crimeLocationId,
    int? page,
    int? pageSize) =>
{
    var result = await svc.ListTrackingSessionsAsync(crimeLocationId, page ?? 1, pageSize ?? 20);
    return Results.Ok(result);
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/object-tracking/{id:guid}", async (Guid id, IObjectTrackingService svc) =>
{
    var session = await svc.GetTrackingSessionAsync(id);
    return session is not null ? Results.Ok(session) : Results.NotFound();
}).RequireAuthorization("ResponderAccess");

app.MapGet("/api/object-tracking/{id:guid}/matches", async (Guid id, IObjectTrackingService svc) =>
{
    var matches = await svc.GetMatchesForSessionAsync(id);
    return Results.Ok(matches);
}).RequireAuthorization("ResponderAccess");

app.MapPost("/api/object-tracking/verify-alibi", async (AlibiVerificationRequest request, IObjectTrackingService svc) =>
{
    var result = await svc.VerifyAlibiAsync(request);
    return Results.Created($"/api/object-tracking/verify-alibi/{result.VerificationId}", result);
}).RequireAuthorization("ResponderAccess");

// SignalR hub endpoints (/hubs/cameras, /hubs/detections, /hubs/footagesubmissions)
app.MapWatchHubs();

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
