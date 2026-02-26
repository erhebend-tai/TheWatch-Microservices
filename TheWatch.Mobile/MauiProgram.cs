using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Radzen;
using TheWatch.Mobile.Auth;
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
        builder.Services.AddSingleton<SpeechListenerService>();
        builder.Services.AddSingleton<PhraseService>();

        // Push notifications
        builder.Services.AddSingleton<WatchPushNotificationService>();

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
