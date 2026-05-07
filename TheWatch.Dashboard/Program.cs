using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using Serilog;
using TheWatch.Dashboard;
using TheWatch.Dashboard.Services;
using TheWatch.Shared.Security;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        // Item 303: 15-minute circuit disconnect timeout (NIST AC-12, STIG V-222578)
        options.DisconnectedCircuitMaxRetained = 100;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(15);
    });

// Item 302: Enforce HttpOnly, Secure, SameSite=Strict on all cookies (STIG V-222575/576)
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});

// Item 242: Hardened anti-forgery configuration — __Host- cookie prefix, Secure, SameSite=Strict
builder.Services.AddWatchAntiForgery();

// Radzen services
builder.Services.AddRadzenComponents();

// Auth
builder.Services.AddScoped<WatchAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<WatchAuthStateProvider>());
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

// Dashboard data services
builder.Services.AddSingleton<MappingDataService>();
builder.Services.AddSingleton<CatalogDataService>();
builder.Services.AddSingleton<MicroserviceClient>();
builder.Services.AddSingleton<DashboardSignalRService>();

// Service discovery + HttpClient for microservice calls
builder.Services.AddServiceDiscovery();
builder.Services.AddHttpClient("microservices", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddServiceDiscovery();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
// Item 302: Enforce cookie security policy (STIG V-222575/576)
app.UseCookiePolicy();
app.UseWatchSerilogRequestLogging();
// Item 242: Use hardened anti-forgery (replaces plain UseAntiforgery())
app.UseWatchAntiForgery();

app.MapRazorComponents<TheWatch.Dashboard.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
