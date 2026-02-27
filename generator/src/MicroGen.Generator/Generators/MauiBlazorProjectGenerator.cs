using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates a MAUI Blazor hybrid application (Windows-focused template) for the service.
/// </summary>
public sealed class MauiBlazorProjectGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public MauiBlazorProjectGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
    {
        _engine = engine;
        _config = config;
        _logger = logger;
    }

    public async Task GenerateAsync(
        ServiceDescriptor service,
        string serviceRoot,
        FileEmitter emitter,
        CancellationToken ct)
    {
        var projectName = $"{service.PascalName}.Maui";
        _logger.LogInformation("  Generating MAUI Hybrid Project: {Project}", projectName);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Project file
        _logger.LogDebug("    Emitting .csproj...");
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            $"{projectName}.csproj",
            _engine.Render(Templates.ProjectFile, new { Service = service }),
            ct);

        // Shared app shell
        _logger.LogDebug("    Emitting App shell (App.xaml)...");
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "App.xaml",
            _engine.Render(Templates.AppXaml, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "App.xaml.cs",
            _engine.Render(Templates.AppXamlCs, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "MauiProgram.cs",
            _engine.Render(Templates.MauiProgram, new { Service = service }),
            ct);

        // Blazor content
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "wwwroot/index.html",
            _engine.Render(Templates.IndexHtml, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "wwwroot/css/app.css",
            Templates.AppCss,
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "_Imports.razor",
            Templates.Imports,
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Main.razor",
            _engine.Render(Templates.MainRazor, new { Service = service }),
            ct);

        // Pages
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Pages/Index.razor",
            _engine.Render(Templates.IndexRazor, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Pages/Status.razor",
            _engine.Render(Templates.StatusRazor, new { Service = service }),
            ct);

        // XAML host page
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "MainPage.xaml",
            _engine.Render(Templates.MainPageXaml, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "MainPage.xaml.cs",
            _engine.Render(Templates.MainPageCode, new { Service = service }),
            ct);

        // Styles
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Resources/Styles/Styles.xaml",
            Templates.Styles,
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Resources/Styles/Colors.xaml",
            Templates.Colors,
            ct);

        // Platform (Windows-only focus)
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Platforms/Windows/App.xaml",
            _engine.Render(Templates.PlatformAppXaml, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Platforms/Windows/App.xaml.cs",
            _engine.Render(Templates.PlatformAppXamlCs, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Platforms/Windows/Package.appxmanifest",
            _engine.Render(Templates.PackageManifest, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Platforms/Windows/app.manifest",
            Templates.AppManifest,
            ct);

        sw.Stop();
        _logger.LogInformation("  MAUI Hybrid Project generated in {Elapsed}ms", sw.ElapsedMilliseconds);
    }

    private static class Templates
    {
                public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <UseMaui>true</UseMaui>
                <SingleProject>true</SingleProject>
                <SupportedOSPlatformVersion>10.0.19041.0</SupportedOSPlatformVersion>
                <TargetPlatformMinVersion>10.0.19041.0</TargetPlatformMinVersion>
                <ApplicationTitle>{{ Service.PascalName }} Maui</ApplicationTitle>
                <ApplicationId>app.thewatch.{{ Service.KebabName }}.maui</ApplicationId>
                                <ApplicationIdGuid>00000000-0000-0000-0000-000000000000</ApplicationIdGuid>
                <EnablePreviewMsixTooling>true</EnablePreviewMsixTooling>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Microsoft.Maui.Controls" Version="8.0.60" />
                <PackageReference Include="Microsoft.Maui.Controls.Compatibility" Version="8.0.60" />
                <PackageReference Include="CommunityToolkit.Maui" Version="8.0.0" />
              </ItemGroup>

              <ItemGroup>
                <MauiAsset Include="Resources\\Raw\\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />
              </ItemGroup>
            </Project>
            """;

        public const string MauiProgram = """
            using Microsoft.Extensions.Logging;
            using Microsoft.Maui;
            using Microsoft.Maui.Controls.Hosting;
            using Microsoft.Maui.Hosting;

            namespace {{ Service.PascalName }}.Maui;

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

                    return builder.Build();
                }
            }
            """;

        public const string AppXaml = """
            <?xml version="1.0" encoding="UTF-8" ?>
            <Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                         x:Class="{{ Service.PascalName }}.Maui.App">
                <Application.Resources>
                    <ResourceDictionary>
                        <Color x:Key="PrimaryColor">#512BD4</Color>
                    </ResourceDictionary>
                </Application.Resources>
            </Application>
            """;

        public const string AppXamlCs = """
            using Microsoft.Maui.Controls;

            namespace {{ Service.PascalName }}.Maui;

            public partial class App : Application
            {
                public App()
                {
                    InitializeComponent();
                    MainPage = new MainPage();
                }
            }
            """;

        public const string MainPageXaml = """
            <?xml version="1.0" encoding="utf-8" ?>
            <ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                         xmlns:blazor="clr-namespace:Microsoft.AspNetCore.Components.WebView.Maui;assembly=Microsoft.AspNetCore.Components.WebView.Maui"
                         x:Class="{{ Service.PascalName }}.Maui.MainPage">
                <Grid>
                    <blazor:BlazorWebView HostPage="wwwroot/index.html">
                        <blazor:BlazorWebView.RootComponents>
                            <blazor:RootComponent Selector="#app" ComponentType="typeof(Main)" />
                        </blazor:BlazorWebView.RootComponents>
                    </blazor:BlazorWebView>
                </Grid>
            </ContentPage>
            """;

        public const string MainPageCode = """
            using Microsoft.Maui.Controls;

            namespace {{ Service.PascalName }}.Maui;

            public partial class MainPage : ContentPage
            {
                public MainPage()
                {
                    InitializeComponent();
                }
            }
            """;

        public const string MainRazor = """
            @inherits ComponentBase
            @page "/"

            <div class="page">
                <h1>{{ Service.Title }}</h1>
                <p>Welcome to the MAUI Blazor shell for <strong>{{ Service.PascalName }}</strong>.</p>
                <Status />
            </div>
            """;

        public const string IndexRazor = """
            @page "/home"
            <h3>{{ Service.Title }} Home</h3>
            <p>This is a MAUI Blazor hybrid container for the {{ Service.PascalName }} microservice.</p>
            """;

        public const string StatusRazor = """
            <div class="status-card">
                <h4>Status</h4>
                <ul>
                    <li>Service: {{ Service.PascalName }}</li>
                    <li>Domain: {{ Service.DomainName }}</li>
                    <li>Version: {{ Service.Version }}</li>
                </ul>
            </div>
            """;

        public const string Imports = """
            @using System.Net.Http
            @using Microsoft.AspNetCore.Components.Forms
            @using Microsoft.AspNetCore.Components.Routing
            @using Microsoft.AspNetCore.Components.Web
            @using Microsoft.JSInterop
            @using Microsoft.Extensions.Logging
            @using {{ Service.PascalName }}.Maui
            """;

        public const string IndexHtml = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>{{ Service.PascalName }} Maui</title>
                <base href="/" />
                <link rel="stylesheet" href="css/app.css" />
            </head>
            <body>
                <div id="app">Loading...</div>
                <script src="_framework/blazor.webview.js"></script>
            </body>
            </html>
            """;

        public const string AppCss = """
            body { font-family: 'Segoe UI', 'Helvetica Neue', sans-serif; margin: 0; padding: 16px; }
            .page { max-width: 960px; margin: 0 auto; }
            .status-card { border: 1px solid #ddd; padding: 12px; border-radius: 8px; background: #fafafa; }
            """;

        public const string Styles = """
            <?xml version="1.0" encoding="UTF-8" ?>
            <ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <Style TargetType="Label">
                    <Setter Property="TextColor" Value="{AppThemeBinding Light=Black, Dark=White}" />
                </Style>
            </ResourceDictionary>
            """;

        public const string Colors = """
            <?xml version="1.0" encoding="UTF-8" ?>
            <ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
                <Color x:Key="Primary">#512BD4</Color>
                <Color x:Key="Secondary">#6B7280</Color>
                <Color x:Key="Tertiary">#2563EB</Color>
            </ResourceDictionary>
            """;

        public const string PlatformAppXaml = """
            <maui:MauiWinUIApplication
                x:Class="{{ Service.PascalName }}.Maui.WinUI.App"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                xmlns:maui="using:Microsoft.Maui"
                xmlns:local="using:{{ Service.PascalName }}.Maui.WinUI">
                <Application.Resources>
                </Application.Resources>
            </maui:MauiWinUIApplication>
            """;

        public const string PlatformAppXamlCs = """
            using Microsoft.Maui;
            using Microsoft.UI.Xaml;

            namespace {{ Service.PascalName }}.Maui.WinUI;

            public partial class App : MauiWinUIApplication
            {
                public App()
                {
                    this.InitializeComponent();
                }

                protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
            }
            """;

        public const string PackageManifest = """
            <?xml version="1.0" encoding="utf-8"?>
            <Package
              xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
              xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
              xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
              IgnorableNamespaces="uap rescap">
              <Identity Name="app.thewatch.{{ Service.KebabName }}.maui"
                        Publisher="CN=TheWatch"
                        Version="1.0.0.0" />
              <Properties>
                <DisplayName>{{ Service.PascalName }} Maui</DisplayName>
                <PublisherDisplayName>The Watch</PublisherDisplayName>
                <Logo>Assets\\Square150x150Logo.png</Logo>
              </Properties>
              <Dependencies>
                <TargetDeviceFamily Name="Windows.Universal" MinVersion="10.0.19041.0" MaxVersionTested="10.0.22631.0" />
              </Dependencies>
              <Applications>
                <Application Id="App"
                             Executable="$targetnametoken$.exe"
                             EntryPoint="{{ Service.PascalName }}.Maui.WinUI.App">
                  <uap:VisualElements DisplayName="{{ Service.PascalName }} Maui"
                                      Description="MAUI Blazor shell for {{ Service.PascalName }}"
                                      Square150x150Logo="Assets\\Square150x150Logo.png"
                                      Square44x44Logo="Assets\\Square44x44Logo.png"
                                      BackgroundColor="transparent">
                  </uap:VisualElements>
                </Application>
              </Applications>
            </Package>
            """;

        public const string AppManifest = """
            <?xml version="1.0" encoding="utf-8"?>
            <assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
              <assemblyIdentity version="1.0.0.0" name="{{ Service.PascalName }}.Maui" />
              <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
                <application>
                  <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
                </application>
              </compatibility>
            </assembly>
            """;
    }
}
