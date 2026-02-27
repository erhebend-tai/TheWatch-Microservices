using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using Serilog;
using TheWatch.Admin;
using TheWatch.Admin.Services;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
app.UseWatchSerilogRequestLogging();
app.UseAntiforgery();

app.MapRazorComponents<TheWatch.Admin.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
