using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Radzen;
using TheWatch.Mobile.Auth;
using TheWatch.Mobile.Data;
using TheWatch.Mobile.Services;

namespace TheWatch.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Load appsettings.json from embedded resources
        using var stream = typeof(MauiProgram).Assembly
            .GetManifestResourceStream("TheWatch.Mobile.appsettings.json");
        if (stream is not null)
            builder.Configuration.AddJsonStream(stream);

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Radzen UI components
        builder.Services.AddRadzenComponents();

        // Authentication
        builder.Services.AddSingleton<WatchAuthService>();
        builder.Services.AddSingleton<MobileAuthStateProvider>();
        builder.Services.AddSingleton<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<MobileAuthStateProvider>());
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();

        // HTTP client with auth header injection
        builder.Services.AddSingleton<AuthDelegatingHandler>();
        builder.Services.AddHttpClient("watch-api", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        }).AddHttpMessageHandler<AuthDelegatingHandler>();
        builder.Services.AddSingleton(sp =>
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("watch-api"));

        // App services
        builder.Services.AddSingleton<WatchApiClient>();
        builder.Services.AddSingleton<PhraseService>();
        builder.Services.AddSingleton<SpeechListenerService>();

        // Platform-specific speech recognition (items 81-83)
#if ANDROID
        builder.Services.AddSingleton<ISpeechRecognitionEngine, Platforms.Android.Services.AndroidSpeechRecognizer>();
#elif IOS
        builder.Services.AddSingleton<ISpeechRecognitionEngine, Platforms.iOS.Services.IosSpeechRecognizer>();
#elif WINDOWS
        builder.Services.AddSingleton<ISpeechRecognitionEngine, Platforms.Windows.Services.WindowsSpeechRecognizer>();
#endif

        // Battery monitoring + connectivity (items 85, 87-92)
        builder.Services.AddSingleton<BatteryMonitorService>();
        builder.Services.AddSingleton<WatchLocalDbContext>();
        builder.Services.AddSingleton<IConnectivityMonitorService, ConnectivityMonitorService>();
        builder.Services.AddSingleton<ConnectivityMonitorService>();
        builder.Services.AddSingleton<IOfflineQueueService, OfflineQueueService>();
        builder.Services.AddSingleton<OfflineQueueService>();
        builder.Services.AddSingleton<CacheService>();
        builder.Services.AddSingleton<SyncEngine>();
        builder.Services.AddSingleton<MeshFallbackService>();

        // Native features (items 93-98)
        builder.Services.AddSingleton<CameraService>();
        builder.Services.AddSingleton<EmergencyLocationService>();
        builder.Services.AddSingleton<HapticService>();
        builder.Services.AddSingleton<BiometricGateService>();
        builder.Services.AddSingleton<LocationTrackingService>();

        // Evidence collection (items 99-105)
        builder.Services.AddSingleton<ChainOfCustodyService>();
        builder.Services.AddSingleton<EvidenceMetadataService>();
        builder.Services.AddSingleton<EvidenceUploadService>();
        builder.Services.AddSingleton<SitrepService>();
        builder.Services.AddSingleton<ContentModerationService>();

        // Ambulance pre-arrival triage (text/STT symptom capture + on-device medical reference)
        builder.Services.AddSingleton<IAmbulanceTriageService, AmbulanceTriageService>();

        // Push notifications
        builder.Services.AddSingleton<WatchPushNotificationService>();

        // SignalR real-time connections
        builder.Services.AddSingleton<WatchSignalRService>();

        return builder.Build();
    }
}

public class AuthDelegatingHandler : DelegatingHandler
{
    private readonly WatchAuthService _auth;

    public AuthDelegatingHandler(WatchAuthService auth) => _auth = auth;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _auth.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
