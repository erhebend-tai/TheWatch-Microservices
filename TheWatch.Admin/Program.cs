using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using Serilog;
using TheWatch.Admin;
using TheWatch.Admin.Services;

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

// Radzen services
builder.Services.AddRadzenComponents();

// Auth — admin-only enforcement
builder.Services.AddScoped<AdminAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AdminAuthStateProvider>());
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

// Admin services
builder.Services.AddScoped<AdminApiClient>();

// Service discovery + HttpClient for microservice calls
builder.Services.AddServiceDiscovery();
builder.Services.AddHttpClient("microservices", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
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
app.UseAntiforgery();

app.MapRazorComponents<TheWatch.Admin.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
