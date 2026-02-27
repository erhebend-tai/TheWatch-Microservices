using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates the Blazor Dashboard project: Radzen pages, Service Explorer, Vibe Voice, Hubs.
/// </summary>
public sealed class DashboardProjectGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public DashboardProjectGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
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
        var projectName = $"{service.PascalName}.Dashboard";
        _logger.LogDebug("  Generating {Project}...", projectName);

        // Project file
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            $"{projectName}.csproj",
            _engine.Render(Templates.ProjectFile, new { Service = service, Config = _config }),
            ct);

        // Program.cs
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Program.cs",
            _engine.Render(Templates.ProgramCs, new { Service = service }),
            ct);

        // _Imports.razor
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "_Imports.razor",
            _engine.Render(Templates.Imports, new { Service = service }),
            ct);

        // App.razor
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "App.razor",
            _engine.Render(Templates.AppRazor, new { Service = service }),
            ct);

        // Main layout
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Layout", "MainLayout.razor"),
            _engine.Render(Templates.MainLayout, new { Service = service }),
            ct);

        // NavMenu
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Layout", "NavMenu.razor"),
            _engine.Render(Templates.NavMenu, new { Service = service }),
            ct);

        // Home page
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Pages", "Home.razor"),
            _engine.Render(Templates.HomePage, new { Service = service }),
            ct);

        // Service Explorer
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Pages", "ServiceExplorer.razor"),
            _engine.Render(Templates.ServiceExplorer, new { Service = service }),
            ct);

        // Service Explorer code-behind
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Pages", "ServiceExplorer.razor.cs"),
            _engine.Render(Templates.ServiceExplorerCode, new { Service = service }),
            ct);

        // Service context
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Services", "ServiceContext.cs"),
            _engine.Render(Templates.ServiceContext, new { Service = service }),
            ct);

        // Vibe Voice components
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Components", "VoicePanel.razor"),
            _engine.Render(Templates.VoicePanel, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Components", "VoicePanel.razor.cs"),
            _engine.Render(Templates.VoicePanelCode, new { Service = service }),
            ct);

        // Voice services
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Services", "VoicePipelineService.cs"),
            _engine.Render(Templates.VoicePipelineService, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Services", "VoiceCommandService.cs"),
            _engine.Render(Templates.VoiceCommandService, new { Service = service }),
            ct);

        // SignalR hub
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Hubs", "DashboardHub.cs"),
            _engine.Render(Templates.DashboardHub, new { Service = service }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Hubs", "VoiceHub.cs"),
            _engine.Render(Templates.VoiceHub, new { Service = service }),
            ct);

        // wwwroot
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("wwwroot", "css", "app.css"),
            _engine.Render(Templates.AppCss, new { Service = service }),
            ct);
    }

    private static class Templates
    {
        public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{{ Config.TargetFramework }}</TargetFramework>
                <RootNamespace>{{ Service.PascalName }}.Dashboard</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Radzen.Blazor" Version="5.*" />
                <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="9.*" />
                <PackageReference Include="Serilog.AspNetCore" Version="8.*" />
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\{{ Service.PascalName }}.Application\{{ Service.PascalName }}.Application.csproj" />
                <ProjectReference Include="..\{{ Service.PascalName }}.Infrastructure\{{ Service.PascalName }}.Infrastructure.csproj" />
              </ItemGroup>
            </Project>
            """;

        public const string ProgramCs = """
            using {{ Service.PascalName }}.Dashboard.Hubs;
            using {{ Service.PascalName }}.Dashboard.Services;
            using Radzen;

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddRadzenComponents();
            builder.Services.AddSignalR();

            builder.Services.AddScoped<ServiceContext>();
            builder.Services.AddScoped<VoicePipelineService>();
            builder.Services.AddScoped<VoiceCommandService>();
            builder.Services.AddScoped<ExportService>();

            builder.Services.AddScoped<DialogService>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<TooltipService>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<{{ Service.PascalName }}.Dashboard.App>()
                .AddInteractiveServerRenderMode();

            app.MapHub<DashboardHub>("/hubs/dashboard");
            app.MapHub<VoiceHub>("/hubs/voice");

            app.Run();
            """;

        public const string Imports = """
            @using System.Net.Http
            @using Microsoft.AspNetCore.Components.Forms
            @using Microsoft.AspNetCore.Components.Routing
            @using Microsoft.AspNetCore.Components.Web
            @using Microsoft.JSInterop
            @using Radzen
            @using Radzen.Blazor
            @using {{ Service.PascalName }}.Dashboard
            @using {{ Service.PascalName }}.Dashboard.Components
            @using {{ Service.PascalName }}.Dashboard.Layout
            @using {{ Service.PascalName }}.Dashboard.Pages
            @using {{ Service.PascalName }}.Dashboard.Services
            """;

        public const string AppRazor = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <base href="/" />
                <link rel="stylesheet" href="_content/Radzen.Blazor/css/material-base.css" />
                <link rel="stylesheet" href="css/app.css" />
                <HeadOutlet @rendermode="InteractiveServer" />
            </head>
            <body>
                <Routes @rendermode="InteractiveServer" />
                <script src="_framework/blazor.web.js"></script>
                <script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
            </body>
            </html>
            """;

        public const string MainLayout = """
            @inherits LayoutComponentBase

            <RadzenLayout>
                <RadzenHeader>
                    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
                        <RadzenSidebarToggle Click="@(() => _sidebarExpanded = !_sidebarExpanded)" />
                        <RadzenLabel Text="{{ Service.Title }} Dashboard" Style="font-weight: bold; font-size: 1.2rem;" />
                    </RadzenStack>
                </RadzenHeader>
                <RadzenSidebar @bind-Expanded="@_sidebarExpanded">
                    <NavMenu />
                </RadzenSidebar>
                <RadzenBody>
                    <div class="rz-p-4">
                        @Body
                    </div>
                </RadzenBody>
                <RadzenFooter>
                    <RadzenText TextStyle="TextStyle.Caption" Text="@($"{{ Service.PascalName }} v{{ Service.Version }}")" />
                </RadzenFooter>
            </RadzenLayout>

            <RadzenDialog />
            <RadzenNotification />
            <RadzenTooltip />

            @code {
                private bool _sidebarExpanded = true;
            }
            """;

        public const string NavMenu = """
            <RadzenPanelMenu>
                <RadzenPanelMenuItem Text="Home" Icon="home" Path="" />
                <RadzenPanelMenuItem Text="Service Explorer" Icon="folder_open" Path="explorer" />
            {{~ for tag in Service.Tags ~}}
                <RadzenPanelMenuItem Text="{{ tag.Name }}" Icon="api" Path="{{ tag.Name | string.downcase }}" />
            {{~ end ~}}
                <RadzenPanelMenuItem Text="Health" Icon="monitor_heart" Path="health" />
                <RadzenPanelMenuItem Text="Jobs" Icon="schedule" Path="jobs" />
            </RadzenPanelMenu>
            """;

        public const string HomePage = """
            @page "/"

            <PageTitle>{{ Service.PascalName }} Dashboard</PageTitle>

            <RadzenText TextStyle="TextStyle.H3" TagName="TagName.H1">{{ Service.Title }}</RadzenText>
            <RadzenText TextStyle="TextStyle.Body1">{{ Service.Description }}</RadzenText>

            <RadzenRow Gap="1rem" class="rz-mt-4">
                <RadzenColumn Size="4">
                    <RadzenCard>
                        <RadzenText TextStyle="TextStyle.H6">Endpoints</RadzenText>
                        <RadzenText TextStyle="TextStyle.DisplayH3">{{ Service.Operations | array.size }}</RadzenText>
                    </RadzenCard>
                </RadzenColumn>
                <RadzenColumn Size="4">
                    <RadzenCard>
                        <RadzenText TextStyle="TextStyle.H6">Controllers</RadzenText>
                        <RadzenText TextStyle="TextStyle.DisplayH3">{{ Service.Tags | array.size }}</RadzenText>
                    </RadzenCard>
                </RadzenColumn>
                <RadzenColumn Size="4">
                    <RadzenCard>
                        <RadzenText TextStyle="TextStyle.H6">Schemas</RadzenText>
                        <RadzenText TextStyle="TextStyle.DisplayH3">{{ Service.Schemas | array.size }}</RadzenText>
                    </RadzenCard>
                </RadzenColumn>
            </RadzenRow>

            <VoicePanel />
            """;

        public const string ServiceExplorer = """
            @page "/explorer"

            <PageTitle>Service Explorer — {{ Service.PascalName }}</PageTitle>

            <RadzenText TextStyle="TextStyle.H4">Service Explorer</RadzenText>

            <RadzenSplitter Orientation="Orientation.Horizontal" style="height: calc(100vh - 200px);">
                <RadzenSplitterPane Size="30%" Min="200px">
                    <RadzenCard style="height: 100%; overflow-y: auto;">
                        <RadzenTree @bind-Value="@_selectedNode" Change="@OnNodeSelected"
                                    Style="width: 100%;">
                            <RadzenTreeItem Text="Controllers" Icon="api" Expanded="true">
                                @foreach (var controller in Controllers)
                                {
                                    <RadzenTreeItem Text="@controller.Name" Icon="class_"
                                                    Value="@controller">
                                        @foreach (var op in controller.Operations)
                                        {
                                            <RadzenTreeItem Text="@op.Name"
                                                            Icon="@GetMethodIcon(op.Method)"
                                                            Value="@op" />
                                        }
                                    </RadzenTreeItem>
                                }
                            </RadzenTreeItem>
                            <RadzenTreeItem Text="Entities" Icon="storage" Expanded="true">
                                @foreach (var entity in Entities)
                                {
                                    <RadzenTreeItem Text="@entity.Name" Icon="table_chart"
                                                    Value="@entity">
                                        @foreach (var prop in entity.Properties)
                                        {
                                            <RadzenTreeItem Text="@($"{prop.Name}: {prop.Type}")"
                                                            Icon="text_fields" />
                                        }
                                    </RadzenTreeItem>
                                }
                            </RadzenTreeItem>
                            <RadzenTreeItem Text="Jobs" Icon="schedule">
                                <RadzenTreeItem Text="HealthCheckJob" Icon="monitor_heart" />
                                <RadzenTreeItem Text="DataSyncJob" Icon="sync" />
                                <RadzenTreeItem Text="CleanupJob" Icon="delete_sweep" />
                            </RadzenTreeItem>
                        </RadzenTree>
                    </RadzenCard>
                </RadzenSplitterPane>
                <RadzenSplitterPane>
                    <RadzenCard style="height: 100%; overflow-y: auto;">
                        @if (_selectedDetail is not null)
                        {
                            <RadzenText TextStyle="TextStyle.H5">@_selectedDetail.Title</RadzenText>
                            <RadzenText TextStyle="TextStyle.Body2" class="rz-mt-2">@_selectedDetail.Description</RadzenText>

                            @if (_selectedDetail.Properties.Count > 0)
                            {
                                <RadzenDataGrid Data="@_selectedDetail.Properties" TItem="PropertyInfo"
                                                AllowSorting="true" class="rz-mt-4">
                                    <Columns>
                                        <RadzenDataGridColumn Property="Name" Title="Name" />
                                        <RadzenDataGridColumn Property="Type" Title="Type" />
                                        <RadzenDataGridColumn Property="Required" Title="Required" Width="80px" />
                                    </Columns>
                                </RadzenDataGrid>
                            }
                        }
                        else
                        {
                            <RadzenText TextStyle="TextStyle.Body1" class="rz-color-secondary">
                                Select a node in the tree to view details.
                            </RadzenText>
                        }
                    </RadzenCard>
                </RadzenSplitterPane>
            </RadzenSplitter>
            """;

        public const string ServiceExplorerCode = """
            using Microsoft.AspNetCore.Components;

            namespace {{ Service.PascalName }}.Dashboard.Pages;

            public partial class ServiceExplorer : ComponentBase
            {
                private object? _selectedNode;
                private DetailView? _selectedDetail;

                public record ControllerNode(string Name, List<OperationNode> Operations);
                public record OperationNode(string Name, string Method, string Path);
                public record EntityNode(string Name, List<PropertyInfo> Properties);
                public record PropertyInfo(string Name, string Type, bool Required);
                public record DetailView(string Title, string Description, List<PropertyInfo> Properties);

                public List<ControllerNode> Controllers { get; set; } = [];
                public List<EntityNode> Entities { get; set; } = [];

                protected override void OnInitialized()
                {
                    // Populated from ServiceContext at runtime
                }

                private void OnNodeSelected(TreeEventArgs args)
                {
                    _selectedDetail = args.Value switch
                    {
                        ControllerNode c => new DetailView(
                            $"{c.Name}Controller",
                            $"API controller with {c.Operations.Count} operations",
                            []),
                        OperationNode op => new DetailView(
                            $"{op.Method.ToUpper()} {op.Path}",
                            op.Name,
                            []),
                        EntityNode e => new DetailView(
                            e.Name,
                            $"Domain entity with {e.Properties.Count} properties",
                            e.Properties),
                        _ => null
                    };
                }

                private static string GetMethodIcon(string method) => method.ToUpperInvariant() switch
                {
                    "GET" => "download",
                    "POST" => "add_circle",
                    "PUT" => "edit",
                    "PATCH" => "build",
                    "DELETE" => "delete",
                    _ => "api"
                };
            }
            """;

        public const string ServiceContext = """
            namespace {{ Service.PascalName }}.Dashboard.Services;

            /// <summary>
            /// Holds runtime metadata about the current service for the dashboard.
            /// </summary>
            public class ServiceContext
            {
                public string ServiceName { get; set; } = "{{ Service.PascalName }}";
                public string ServiceVersion { get; set; } = "{{ Service.Version }}";
                public string Description { get; set; } = "{{ Service.Description }}";
                public List<string> Tags { get; set; } = [];
                public List<EndpointInfo> Endpoints { get; set; } = [];
                public List<EntityInfo> Entities { get; set; } = [];

                public record EndpointInfo(string Method, string Path, string OperationId, string Tag);
                public record EntityInfo(string Name, List<PropertyInfo> Properties);
                public record PropertyInfo(string Name, string Type, bool Required);
            }
            """;

        public const string VoicePanel = """
            <div class="voice-panel rz-mt-4">
                <RadzenCard>
                    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                        <RadzenButton Icon="@(_isListening ? "mic_off" : "mic")"
                                      ButtonStyle="@(_isListening ? ButtonStyle.Danger : ButtonStyle.Primary)"
                                      Click="@ToggleListening"
                                      Size="ButtonSize.Large" />
                        <RadzenTextBox @bind-Value="@_transcription" Placeholder="Speak or type a command..."
                                       Style="flex: 1;" ReadOnly="@_isListening" />
                        <RadzenButton Text="Execute" Icon="play_arrow" Click="@ExecuteCommand"
                                      Disabled="@string.IsNullOrWhiteSpace(_transcription)" />
                    </RadzenStack>

                    @if (!string.IsNullOrEmpty(_lastResponse))
                    {
                        <RadzenAlert AlertStyle="AlertStyle.Info" Shade="Shade.Lighter" class="rz-mt-2">
                            @_lastResponse
                        </RadzenAlert>
                    }
                </RadzenCard>
            </div>
            """;

        public const string VoicePanelCode = """
            using {{ Service.PascalName }}.Dashboard.Services;
            using Microsoft.AspNetCore.Components;
            using Microsoft.JSInterop;

            namespace {{ Service.PascalName }}.Dashboard.Components;

            public partial class VoicePanel : ComponentBase
            {
                [Inject] private VoicePipelineService VoicePipeline { get; set; } = null!;
                [Inject] private VoiceCommandService CommandService { get; set; } = null!;
                [Inject] private IJSRuntime JS { get; set; } = null!;

                private bool _isListening;
                private string _transcription = string.Empty;
                private string _lastResponse = string.Empty;

                private async Task ToggleListening()
                {
                    _isListening = !_isListening;

                    if (_isListening)
                    {
                        _transcription = string.Empty;
                        _lastResponse = string.Empty;
                        await VoicePipeline.StartListeningAsync();
                        VoicePipeline.OnTranscription += HandleTranscription;
                    }
                    else
                    {
                        VoicePipeline.OnTranscription -= HandleTranscription;
                        await VoicePipeline.StopListeningAsync();
                    }
                }

                private void HandleTranscription(string text)
                {
                    _transcription = text;
                    InvokeAsync(StateHasChanged);
                }

                private async Task ExecuteCommand()
                {
                    if (string.IsNullOrWhiteSpace(_transcription))
                        return;

                    _lastResponse = await CommandService.ExecuteAsync(_transcription);
                    StateHasChanged();
                }
            }
            """;

        public const string VoicePipelineService = """
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Dashboard.Services;

            /// <summary>
            /// Manages the voice input pipeline: microphone capture → transcription → command parsing.
            /// </summary>
            public class VoicePipelineService
            {
                private readonly ILogger<VoicePipelineService> _logger;
                private bool _isListening;

                public event Action<string>? OnTranscription;
                public event Action<string>? OnError;

                public VoicePipelineService(ILogger<VoicePipelineService> logger)
                {
                    _logger = logger;
                }

                public Task StartListeningAsync()
                {
                    _isListening = true;
                    _logger.LogInformation("Voice pipeline started for {{ Service.PascalName }}");
                    // TODO: Initialize browser Speech Recognition API via JS interop
                    return Task.CompletedTask;
                }

                public Task StopListeningAsync()
                {
                    _isListening = false;
                    _logger.LogInformation("Voice pipeline stopped for {{ Service.PascalName }}");
                    return Task.CompletedTask;
                }

                public void ProcessTranscription(string text)
                {
                    OnTranscription?.Invoke(text);
                }
            }
            """;

        public const string VoiceCommandService = """
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Dashboard.Services;

            /// <summary>
            /// Parses and executes voice commands against the service context.
            /// Connects to logging and REST pipelines.
            /// </summary>
            public class VoiceCommandService
            {
                private readonly ILogger<VoiceCommandService> _logger;
                private readonly ServiceContext _context;

                public VoiceCommandService(
                    ILogger<VoiceCommandService> logger,
                    ServiceContext context)
                {
                    _logger = logger;
                    _context = context;
                }

                public Task<string> ExecuteAsync(string command)
                {
                    _logger.LogInformation("Voice command received: {Command}", command);

                    var normalized = command.Trim().ToLowerInvariant();

                    // Built-in commands
                    var response = normalized switch
                    {
                        var c when c.StartsWith("list endpoints") =>
                            $"Found {_context.Endpoints.Count} endpoints in {_context.ServiceName}",
                        var c when c.StartsWith("list entities") =>
                            $"Found {_context.Entities.Count} entities in {_context.ServiceName}",
                        var c when c.StartsWith("status") or c.StartsWith("health") =>
                            $"{_context.ServiceName} v{_context.ServiceVersion} is running",
                        var c when c.StartsWith("help") =>
                            "Available commands: list endpoints, list entities, status, help",
                        _ => $"Unknown command: {command}. Say 'help' for available commands."
                    };

                    _logger.LogInformation("Voice command response: {Response}", response);
                    return Task.FromResult(response);
                }
            }
            """;

        public const string DashboardHub = """
            using Microsoft.AspNetCore.SignalR;

            namespace {{ Service.PascalName }}.Dashboard.Hubs;

            public class DashboardHub : Hub
            {
                private readonly ILogger<DashboardHub> _logger;

                public DashboardHub(ILogger<DashboardHub> logger) => _logger = logger;

                public override async Task OnConnectedAsync()
                {
                    _logger.LogInformation("Dashboard client connected: {ConnectionId}", Context.ConnectionId);
                    await base.OnConnectedAsync();
                }

                public override async Task OnDisconnectedAsync(Exception? exception)
                {
                    _logger.LogInformation("Dashboard client disconnected: {ConnectionId}", Context.ConnectionId);
                    await base.OnDisconnectedAsync(exception);
                }

                public async Task SendMetricUpdate(string metricName, double value)
                {
                    await Clients.All.SendAsync("MetricUpdated", metricName, value);
                }
            }
            """;

        public const string VoiceHub = """
            using Microsoft.AspNetCore.SignalR;

            namespace {{ Service.PascalName }}.Dashboard.Hubs;

            public class VoiceHub : Hub
            {
                private readonly ILogger<VoiceHub> _logger;

                public VoiceHub(ILogger<VoiceHub> logger) => _logger = logger;

                public async Task SendTranscription(string text)
                {
                    _logger.LogDebug("Voice transcription received: {Text}", text);
                    await Clients.Caller.SendAsync("TranscriptionReceived", text);
                }

                public async Task SendCommand(string command)
                {
                    _logger.LogInformation("Voice command via hub: {Command}", command);
                    await Clients.Caller.SendAsync("CommandResult", $"Executed: {command}");
                }
            }
            """;

        public const string AppCss = """
            /* {{ Service.PascalName }} Dashboard Styles */
            :root {
                --primary: #1e88e5;
                --surface: #ffffff;
                --background: #f5f5f5;
            }

            html, body {
                margin: 0;
                padding: 0;
                font-family: 'Roboto', sans-serif;
            }

            .voice-panel {
                position: sticky;
                bottom: 0;
                z-index: 100;
            }

            .rz-sidebar {
                background: var(--surface);
            }

            .rz-body {
                background: var(--background);
            }
            """;
    }
}
