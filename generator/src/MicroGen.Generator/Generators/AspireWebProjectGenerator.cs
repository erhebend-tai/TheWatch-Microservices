using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates a unified Radzen Blazor Server web app for a domain.
/// Uses Client SDKs from all services in the domain with Aspire service discovery.
/// One per domain.
/// </summary>
public sealed class AspireWebProjectGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public AspireWebProjectGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
    {
        _engine = engine;
        _config = config;
        _logger = logger;
    }

    public async Task GenerateDomainWebAsync(
        DomainDescriptor domain,
        string domainRoot,
        FileEmitter emitter,
        CancellationToken ct)
    {
        var projectName = $"{domain.PascalName}.Web";
        var projectRoot = Path.Combine(domainRoot, projectName);
        var model = new { Domain = domain, Config = _config };

        _logger.LogDebug("  Generating Aspire Web frontend for domain {Domain}...", domain.DomainName);

        // Project file
        await emitter.EmitAsync(
            Path.Combine(projectRoot, $"{projectName}.csproj"),
            _engine.Render(Templates.ProjectFile, model), ct);

        // Program.cs
        await emitter.EmitAsync(
            Path.Combine(projectRoot, "Program.cs"),
            _engine.Render(Templates.ProgramCs, model), ct);

        // Blazor shell
        await emitter.EmitAsync(
            Path.Combine(projectRoot, "_Imports.razor"),
            _engine.Render(Templates.Imports, model), ct);

        await emitter.EmitAsync(
            Path.Combine(projectRoot, "App.razor"),
            _engine.Render(Templates.AppRazor, model), ct);

        await emitter.EmitAsync(
            Path.Combine(projectRoot, "Routes.razor"),
            _engine.Render(Templates.RoutesRazor, model), ct);

        // Layout
        await emitter.EmitAsync(
            Path.Combine(projectRoot, "Layout", "MainLayout.razor"),
            _engine.Render(Templates.MainLayout, model), ct);

        await emitter.EmitAsync(
            Path.Combine(projectRoot, "Layout", "NavMenu.razor"),
            _engine.Render(Templates.NavMenu, model), ct);

        // Home page
        await emitter.EmitAsync(
            Path.Combine(projectRoot, "Pages", "Home.razor"),
            _engine.Render(Templates.HomePage, model), ct);

        // Per-service overview pages
        foreach (var svc in domain.Services)
        {
            await emitter.EmitAsync(
                Path.Combine(projectRoot, "Pages", $"{svc.PascalName}Overview.razor"),
                _engine.Render(Templates.ServiceOverviewPage, new { Domain = domain, Service = svc, Config = _config }),
                ct);
        }

        // Static assets
        await emitter.EmitAsync(
            Path.Combine(projectRoot, "wwwroot", "css", "app.css"),
            _engine.Render(Templates.AppCss, model), ct);
    }

    private static class Templates
    {
        public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{{ Config.TargetFramework }}</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <RootNamespace>{{ Domain.PascalName }}.Web</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Radzen.Blazor" Version="5.*" />
              </ItemGroup>

              <ItemGroup>
                <ProjectReference Include="..\{{ Domain.PascalName }}.Aspire.ServiceDefaults\{{ Domain.PascalName }}.Aspire.ServiceDefaults.csproj" />
            {{~ for svc in Domain.Services ~}}
                <ProjectReference Include="..\{{ svc.PascalName }}\src\{{ svc.PascalName }}.Client\{{ svc.PascalName }}.Client.csproj" />
            {{~ end ~}}
              </ItemGroup>
            </Project>
            """;

        public const string ProgramCs = """
            using {{ Domain.PascalName }}.Aspire.ServiceDefaults;
            {{~ for svc in Domain.Services ~}}
            using {{ svc.PascalName }}.Client;
            {{~ end ~}}
            using Radzen;

            var builder = WebApplication.CreateBuilder(args);

            builder.AddServiceDefaults();

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddRadzenComponents();
            builder.Services.AddScoped<DialogService>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<TooltipService>();

            // Register Client SDKs with Aspire service discovery
            {{~ for svc in Domain.Services ~}}
            builder.Services.Add{{ svc.PascalName }}Client(options =>
            {
                options.BaseUrl = "http://{{ svc.KebabName }}-api";
            });
            {{~ end ~}}

            var app = builder.Build();

            app.MapDefaultEndpoints();

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<{{ Domain.PascalName }}.Web.App>()
                .AddInteractiveServerRenderMode();

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
            @using {{ Domain.PascalName }}.Web
            @using {{ Domain.PascalName }}.Web.Layout
            @using {{ Domain.PascalName }}.Web.Pages
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

        public const string RoutesRazor = """
            <Router AppAssembly="typeof(Program).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
                    <FocusOnNavigate RouteData="routeData" Selector="h1" />
                </Found>
            </Router>
            """;

        public const string MainLayout = """
            @inherits LayoutComponentBase

            <RadzenLayout>
                <RadzenHeader>
                    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
                        <RadzenSidebarToggle Click="@(() => _sidebarExpanded = !_sidebarExpanded)" />
                        <RadzenLabel Text="{{ Domain.PascalName }} Platform" Style="font-weight: bold; font-size: 1.2rem;" />
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
                    <RadzenText TextStyle="TextStyle.Caption" Text="{{ Domain.PascalName }} — Powered by Aspire + Radzen" />
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
            {{~ for svc in Domain.Services ~}}
                <RadzenPanelMenuItem Text="{{ svc.PascalName }}" Icon="api" Path="{{ svc.KebabName }}">
                {{~ for tag in svc.Tags ~}}
                    <RadzenPanelMenuItem Text="{{ tag.Name }}" Icon="folder_open"
                                         Path="{{ svc.KebabName }}/{{ tag.Name | string.downcase }}" />
                {{~ end ~}}
                </RadzenPanelMenuItem>
            {{~ end ~}}
                <RadzenPanelMenuItem Text="Health" Icon="monitor_heart" Path="health" />
            </RadzenPanelMenu>
            """;

        public const string HomePage = """
            @page "/"

            <PageTitle>{{ Domain.PascalName }} Platform</PageTitle>

            <RadzenText TextStyle="TextStyle.H3" TagName="TagName.H1">{{ Domain.PascalName }} Platform</RadzenText>
            <RadzenText TextStyle="TextStyle.Body1" class="rz-color-secondary rz-mb-4">
                Unified dashboard for the {{ Domain.PascalName }} domain — {{ Domain.Services | array.size }} services,
                {{ Domain.TotalOperations }} endpoints, {{ Domain.TotalSchemas }} schemas.
            </RadzenText>

            <RadzenRow Gap="1rem" class="rz-mb-4">
                <RadzenColumn Size="3">
                    <RadzenCard>
                        <RadzenStack AlignItems="AlignItems.Center">
                            <RadzenIcon Icon="dns" Style="font-size: 2rem; color: var(--rz-primary);" />
                            <RadzenText TextStyle="TextStyle.DisplayH4">{{ Domain.Services | array.size }}</RadzenText>
                            <RadzenText TextStyle="TextStyle.Caption">Services</RadzenText>
                        </RadzenStack>
                    </RadzenCard>
                </RadzenColumn>
                <RadzenColumn Size="3">
                    <RadzenCard>
                        <RadzenStack AlignItems="AlignItems.Center">
                            <RadzenIcon Icon="api" Style="font-size: 2rem; color: var(--rz-success);" />
                            <RadzenText TextStyle="TextStyle.DisplayH4">{{ Domain.TotalOperations }}</RadzenText>
                            <RadzenText TextStyle="TextStyle.Caption">Endpoints</RadzenText>
                        </RadzenStack>
                    </RadzenCard>
                </RadzenColumn>
                <RadzenColumn Size="3">
                    <RadzenCard>
                        <RadzenStack AlignItems="AlignItems.Center">
                            <RadzenIcon Icon="storage" Style="font-size: 2rem; color: var(--rz-warning);" />
                            <RadzenText TextStyle="TextStyle.DisplayH4">{{ Domain.TotalSchemas }}</RadzenText>
                            <RadzenText TextStyle="TextStyle.Caption">Schemas</RadzenText>
                        </RadzenStack>
                    </RadzenCard>
                </RadzenColumn>
                <RadzenColumn Size="3">
                    <RadzenCard>
                        <RadzenStack AlignItems="AlignItems.Center">
                            <RadzenIcon Icon="check_circle" Style="font-size: 2rem; color: var(--rz-info);" />
                            <RadzenText TextStyle="TextStyle.DisplayH4">Healthy</RadzenText>
                            <RadzenText TextStyle="TextStyle.Caption">Status</RadzenText>
                        </RadzenStack>
                    </RadzenCard>
                </RadzenColumn>
            </RadzenRow>

            <RadzenText TextStyle="TextStyle.H5" class="rz-mb-2">Services</RadzenText>
            <RadzenDataGrid Data="@_services" TItem="ServiceInfo" AllowSorting="true" Density="Density.Compact">
                <Columns>
                    <RadzenDataGridColumn TItem="ServiceInfo" Property="Name" Title="Service" />
                    <RadzenDataGridColumn TItem="ServiceInfo" Property="Endpoints" Title="Endpoints" Width="120px"
                                          TextAlign="TextAlign.Right" />
                    <RadzenDataGridColumn TItem="ServiceInfo" Property="Schemas" Title="Schemas" Width="120px"
                                          TextAlign="TextAlign.Right" />
                    <RadzenDataGridColumn TItem="ServiceInfo" Property="Version" Title="Version" Width="120px" />
                    <RadzenDataGridColumn TItem="ServiceInfo" Title="Actions" Width="100px" Sortable="false">
                        <Template Context="svc">
                            <RadzenButton Text="View" Icon="open_in_new" ButtonStyle="ButtonStyle.Light"
                                          Size="ButtonSize.Small" Click="@(() => Nav.NavigateTo(svc.Path))" />
                        </Template>
                    </RadzenDataGridColumn>
                </Columns>
            </RadzenDataGrid>

            @code {
                [Inject] private NavigationManager Nav { get; set; } = null!;

                private record ServiceInfo(string Name, int Endpoints, int Schemas, string Version, string Path);

                private List<ServiceInfo> _services =
                [
            {{~ for svc in Domain.Services ~}}
                    new("{{ svc.PascalName }}", {{ svc.TotalOperations }}, {{ svc.Schemas | array.size }}, "{{ svc.Version }}", "{{ svc.KebabName }}"),
            {{~ end ~}}
                ];
            }
            """;

        public const string ServiceOverviewPage = """
            @page "/{{ Service.KebabName }}"

            <PageTitle>{{ Service.PascalName }} — {{ Domain.PascalName }}</PageTitle>

            <RadzenBreadCrumb class="rz-mb-4">
                <RadzenBreadCrumbItem Text="Home" Path="" />
                <RadzenBreadCrumbItem Text="{{ Service.PascalName }}" />
            </RadzenBreadCrumb>

            <RadzenText TextStyle="TextStyle.H4">{{ Service.Title }}</RadzenText>
            <RadzenText TextStyle="TextStyle.Body1" class="rz-color-secondary rz-mb-4">
                {{ Service.Description }}
            </RadzenText>

            <RadzenRow Gap="1rem" class="rz-mb-4">
                <RadzenColumn Size="4">
                    <RadzenCard>
                        <RadzenStack AlignItems="AlignItems.Center">
                            <RadzenIcon Icon="api" Style="font-size: 2rem; color: var(--rz-primary);" />
                            <RadzenText TextStyle="TextStyle.DisplayH4">{{ Service.TotalOperations }}</RadzenText>
                            <RadzenText TextStyle="TextStyle.Caption">Endpoints</RadzenText>
                        </RadzenStack>
                    </RadzenCard>
                </RadzenColumn>
                <RadzenColumn Size="4">
                    <RadzenCard>
                        <RadzenStack AlignItems="AlignItems.Center">
                            <RadzenIcon Icon="class_" Style="font-size: 2rem; color: var(--rz-success);" />
                            <RadzenText TextStyle="TextStyle.DisplayH4">{{ Service.Tags | array.size }}</RadzenText>
                            <RadzenText TextStyle="TextStyle.Caption">Controllers</RadzenText>
                        </RadzenStack>
                    </RadzenCard>
                </RadzenColumn>
                <RadzenColumn Size="4">
                    <RadzenCard>
                        <RadzenStack AlignItems="AlignItems.Center">
                            <RadzenIcon Icon="storage" Style="font-size: 2rem; color: var(--rz-warning);" />
                            <RadzenText TextStyle="TextStyle.DisplayH4">{{ Service.Schemas | array.size }}</RadzenText>
                            <RadzenText TextStyle="TextStyle.Caption">Schemas</RadzenText>
                        </RadzenStack>
                    </RadzenCard>
                </RadzenColumn>
            </RadzenRow>

            <RadzenText TextStyle="TextStyle.H5" class="rz-mb-2">Endpoints</RadzenText>
            <RadzenDataGrid Data="@_operations" TItem="OperationInfo" AllowSorting="true"
                            AllowFiltering="true" FilterMode="FilterMode.Simple"
                            Density="Density.Compact" PageSize="20" AllowPaging="true">
                <Columns>
                    <RadzenDataGridColumn TItem="OperationInfo" Property="Method" Title="Method" Width="100px">
                        <Template Context="op">
                            <RadzenBadge Text="@op.Method" BadgeStyle="@GetBadgeStyle(op.Method)" IsPill="true" />
                        </Template>
                    </RadzenDataGridColumn>
                    <RadzenDataGridColumn TItem="OperationInfo" Property="Path" Title="Path" />
                    <RadzenDataGridColumn TItem="OperationInfo" Property="OperationId" Title="Operation" />
                    <RadzenDataGridColumn TItem="OperationInfo" Property="Tag" Title="Tag" Width="140px" />
                </Columns>
            </RadzenDataGrid>

            @code {
                private record OperationInfo(string Method, string Path, string OperationId, string Tag);

                private List<OperationInfo> _operations =
                [
            {{~ for op in Service.Operations ~}}
                    new("{{ op.HttpMethod }}", "{{ op.Path }}", "{{ op.OperationId }}", "{{ op.Tag }}"),
            {{~ end ~}}
                ];

                private static BadgeStyle GetBadgeStyle(string method) => method.ToUpperInvariant() switch
                {
                    "GET" => BadgeStyle.Success,
                    "POST" => BadgeStyle.Primary,
                    "PUT" => BadgeStyle.Warning,
                    "PATCH" => BadgeStyle.Info,
                    "DELETE" => BadgeStyle.Danger,
                    _ => BadgeStyle.Light
                };
            }
            """;

        public const string AppCss = """
            /* {{ Domain.PascalName }} Web — Aspire + Radzen Blazor */
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

            .rz-sidebar {
                background: var(--surface);
            }

            .rz-body {
                background: var(--background);
            }
            """;
    }
}
