using Hangfire;
using Hangfire.Batches;
using Hangfire.InMemory;
using Serilog;
using TheWatch.P9.DoctorServices;
using TheWatch.P9.DoctorServices.Doctors;
using TheWatch.P9.DoctorServices.Services;
using TheWatch.Shared.Contracts;
using TheWatch.P9.DoctorServices.Data.Seeders;
using TheWatch.Shared.Gcp;
using TheWatch.Shared.Cloudflare;
using TheWatch.Shared.Security;
using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.FamilyHealth;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();
builder.ConfigureWatchOpenApi();
builder.AddWatchPersistenceAspire();
builder.ConfigureWatchNotifications();
builder.Services.AddGcpServicesIfConfigured(builder.Configuration);
builder.Services.AddCloudflareServicesIfConfigured(builder.Configuration);

builder.Services.AddWatchCors(builder.Configuration);

// Hangfire with InMemory storage + Pro batches
builder.Services.AddHangfire(config =>
    config
        .UseInMemoryStorage()
        .UseBatches());
builder.Services.AddHangfireServer();

// ── Inter-service typed HTTP clients (Item 213) ──
// Shared delegating handlers for correlation ID + API key auth (Items 218, 219)
builder.Services.AddWatchClientHandlers();

// Item 213: IFamilyHealthClient — patient family health data during appointments/telehealth
builder.Services.AddFamilyHealthClient()
    .AddWatchClientDefaults(builder.Configuration["ServiceUrls:FamilyHealth"] ?? "https+http://p7-familyhealth");

// Services
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.AddWatchSecurity();
builder.Services.AddScoped<IWatchDataSeeder, DoctorServicesSeeder>();
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
    Authorization = [new TheWatch.Shared.Security.HangfireDashboardAuthFilter()],
    IsReadOnlyFunc = _ => true
});
app.MapWatchControllers();

// Recurring Hangfire jobs
RecurringJob.AddOrUpdate<IAppointmentService>(
    "appointment-cleanup",
    svc => svc.CleanupPastAppointmentsAsync(TimeSpan.FromDays(90)),
    "0 5 * * 0"); // Weekly on Sunday at 5 AM

// Health endpoint
app.MapGet("/health", () => new HealthResponse(
    "TheWatch.P9.DoctorServices",
    "P9",
    "Healthy",
    DateTime.UtcNow));

// Service info
app.MapGet("/info", () => new
{
    Service = "TheWatch.P9.DoctorServices",
    Program = "P9",
    Name = "DoctorServices",
    Description = "Marketplace, appointments, telehealth",
    Icon = "local_hospital",
    Version = "0.2.0"
});

// === Doctor Endpoints ===

app.MapPost("/api/doctors", async (CreateDoctorProfileRequest request, IDoctorService svc) =>
{
    var doctor = await svc.CreateProfileAsync(request);
    return Results.Created($"/api/doctors/{doctor.Id}", doctor);
}).RequireAuthorization("DoctorAccess");

app.MapGet("/api/doctors", async (IDoctorService svc, int? page, int? pageSize) =>
{
    var result = await svc.ListAsync(page ?? 1, pageSize ?? 20);
    return Results.Ok(result);
}).RequireAuthorization("Authenticated");

app.MapGet("/api/doctors/{id:guid}", async (Guid id, IDoctorService svc) =>
{
    var doctor = await svc.GetByIdAsync(id);
    return doctor is not null ? Results.Ok(doctor) : Results.NotFound();
}).RequireAuthorization("Authenticated");

app.MapGet("/api/doctors/search", async (
    IDoctorService svc,
    string? specialization,
    double? lat,
    double? lon,
    double? radiusKm,
    bool? acceptingOnly) =>
{
    var query = new DoctorSearchQuery(specialization, lat, lon, radiusKm, acceptingOnly);
    var results = await svc.SearchAsync(query);
    return Results.Ok(results);
}).RequireAuthorization("Authenticated");

// === Appointment Endpoints ===

app.MapPost("/api/appointments", async (BookAppointmentRequest request, IAppointmentService svc) =>
{
    var appt = await svc.BookAsync(request);
    return Results.Created($"/api/appointments/{appt.Id}", appt);
}).RequireAuthorization("Authenticated");

app.MapGet("/api/appointments/{id:guid}", async (Guid id, IAppointmentService svc) =>
{
    var appt = await svc.GetByIdAsync(id);
    return appt is not null ? Results.Ok(appt) : Results.NotFound();
}).RequireAuthorization("Authenticated");

app.MapGet("/api/appointments", async (IAppointmentService svc, Guid? doctorId, Guid? patientId) =>
{
    var result = await svc.ListUpcomingAsync(doctorId, patientId);
    return Results.Ok(result);
}).RequireAuthorization("Authenticated");

app.MapPut("/api/appointments/{id:guid}/status", async (Guid id, UpdateAppointmentStatusRequest request, IAppointmentService svc) =>
{
    var appt = await svc.UpdateStatusAsync(id, request);
    return appt is not null ? Results.Ok(appt) : Results.NotFound();
}).RequireAuthorization("DoctorAccess");

app.MapPut("/api/appointments/{id:guid}/reschedule", async (Guid id, RescheduleRequest request, IAppointmentService svc) =>
{
    var appt = await svc.RescheduleAsync(id, request);
    return appt is not null ? Results.Ok(appt) : Results.NotFound();
}).RequireAuthorization("DoctorAccess");

app.MapPost("/api/appointments/{id:guid}/telehealth", async (Guid id, IAppointmentService svc) =>
{
    var session = await svc.CreateSessionAsync(id);
    return Results.Created($"/api/telehealth/{session.Id}", session);
}).RequireAuthorization("DoctorAccess");

app.Run();

// Needed for WebApplicationFactory in tests
public partial class Program { }
