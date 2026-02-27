using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using Serilog;
using TheWatch.Dashboard;
using TheWatch.Dashboard.Services;

SerilogSetup.BootstrapSerilog();

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureWatchSerilog();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
app.UseWatchSerilogRequestLogging();
app.UseAntiforgery();

app.MapRazorComponents<TheWatch.Dashboard.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
