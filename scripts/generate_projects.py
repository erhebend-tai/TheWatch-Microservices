#!/usr/bin/env python3
"""
generate_projects.py — Generate all microservice projects, test projects,
shared project, Aspire host, and solution file.
"""

import os
import json
from pathlib import Path
from textwrap import dedent

OUT = Path("E:/json_output/Microservices")

PROGRAMS = [
    ("P1", "CoreGateway",       "API Gateway, user profiles, platform config",
     "gateway", "bi-house-door"),
    ("P2", "VoiceEmergency",    "Emergency reporting, dispatch, voice SOS",
     "phone-vibrate", "bi-exclamation-triangle"),
    ("P3", "MeshNetwork",       "Messaging, notifications, mesh relay",
     "broadcast-pin", "bi-broadcast"),
    ("P4", "Wearable",          "Device sync, heartbeat, offline queue",
     "smartwatch", "bi-smartwatch"),
    ("P5", "AuthSecurity",      "Authentication, MFA, JWT, security",
     "shield-lock", "bi-shield-lock"),
    ("P6", "FirstResponder",    "Responder registration, dispatch, check-in",
     "person-badge", "bi-person-badge"),
    ("P7", "FamilyHealth",      "Child check-in, vitals, medical alerts",
     "heart-pulse", "bi-heart-pulse"),
    ("P8", "DisasterRelief",    "Evacuation, resource matching, shelters",
     "cloud-lightning", "bi-cloud-lightning"),
    ("P9", "DoctorServices",    "Marketplace, appointments, telehealth",
     "hospital", "bi-hospital"),
    ("P10", "Gamification",     "Rewards, challenges, leaderboards",
     "trophy", "bi-trophy"),
]

def write(path, content):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def gen_shared():
    """TheWatch.Shared — shared models, contracts, Hangfire config."""
    d = OUT / "TheWatch.Shared"
    write(d / "TheWatch.Shared.csproj", dedent("""\
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Hangfire.Core" Version="1.8.*" />
          </ItemGroup>
        </Project>
    """))

    write(d / "Contracts" / "IWatchService.cs", dedent("""\
        namespace TheWatch.Shared.Contracts;

        public interface IWatchService
        {
            string ServiceName { get; }
            string Program { get; }
        }
    """))

    write(d / "Contracts" / "HealthResponse.cs", dedent("""\
        namespace TheWatch.Shared.Contracts;

        public record HealthResponse(
            string Service,
            string Program,
            string Status,
            DateTime Timestamp,
            Dictionary<string, object>? Details = null);
    """))

    write(d / "Hangfire" / "HangfireDefaults.cs", dedent("""\
        using Hangfire;

        namespace TheWatch.Shared.Hangfire;

        public static class HangfireDefaults
        {
            public static void ConfigureFilters(IGlobalConfiguration config)
            {
                config
                    .UseFilter(new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = new[] { 10, 60, 300 } });
            }
        }
    """))


def gen_service_project(prog_id, name, description, icon, bi_icon):
    """Generate one microservice project."""
    proj_name = f"TheWatch.{prog_id}.{name}"
    d = OUT / proj_name

    write(d / f"{proj_name}.csproj", dedent(f"""\
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <RootNamespace>{proj_name}</RootNamespace>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Aspire.Hosting.AppHost" Version="9.2.0" Condition="false" />
            <PackageReference Include="Hangfire.AspNetCore" Version="1.8.*" />
            <PackageReference Include="Hangfire.MemoryStorage" Version="1.8.*" />
            <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.*-*" />
            <PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.*" />
          </ItemGroup>

          <ItemGroup>
            <ProjectReference Include="..\\TheWatch.Shared\\TheWatch.Shared.csproj" />
            <ProjectReference Include="..\\TheWatch.Generators\\TheWatch.Generators.csproj"
                              OutputItemType="Analyzer"
                              ReferenceOutputAssembly="false" />
          </ItemGroup>

          <ItemGroup>
            <AdditionalFiles Include="..\\Models.json" Link="Data\\Models.json" />
            <AdditionalFiles Include="..\\Interfaces.json" Link="Data\\Interfaces.json" />
            <AdditionalFiles Include="..\\_mapping.json" Link="Data\\_mapping.json" />
          </ItemGroup>
        </Project>
    """))

    write(d / "Program.cs", dedent(f"""\
        using Hangfire;
        using Hangfire.MemoryStorage;
        using TheWatch.Shared.Contracts;

        var builder = WebApplication.CreateBuilder(args);

        // OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Hangfire
        builder.Services.AddHangfire(config =>
            config.UseMemoryStorage());
        builder.Services.AddHangfireServer();

        // Service registrations (generated)
        // builder.Services.RegisterAllJobs();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHangfireDashboard("/hangfire");

        // Health endpoint
        app.MapGet("/health", () => new HealthResponse(
            "{proj_name}",
            "{prog_id}",
            "Healthy",
            DateTime.UtcNow));

        // Service info
        app.MapGet("/info", () => new
        {{
            Service = "{proj_name}",
            Program = "{prog_id}",
            Name = "{name}",
            Description = "{description}",
            Icon = "{bi_icon}",
            Version = "0.1.0"
        }});

        // Generated endpoints
        // app.MapAllEndpoints();

        app.Run();

        // Needed for WebApplicationFactory in tests
        public partial class Program {{ }}
    """))

    write(d / "appsettings.json", json.dumps({
        "Logging": {
            "LogLevel": {"Default": "Information", "Microsoft.AspNetCore": "Warning"}
        },
        "AllowedHosts": "*",
        "ConnectionStrings": {
            "SqlServer": f"Server=localhost;Database=Watch{name}DB;Trusted_Connection=true;TrustServerCertificate=true",
            "MongoDB": "mongodb+srv://cluster.mongodb.net/TheWatch",
            "Redis": "localhost:6379"
        },
        "Hangfire": {
            "DashboardPath": "/hangfire",
            "WorkerCount": 2
        }
    }, indent=2))

    write(d / "Properties" / "launchSettings.json", json.dumps({
        "profiles": {
            proj_name: {
                "commandName": "Project",
                "dotnetRunMessages": True,
                "launchBrowser": True,
                "launchUrl": "swagger",
                "applicationUrl": f"https://localhost:{5100 + int(prog_id[1:])};http://localhost:{5200 + int(prog_id[1:])}",
                "environmentVariables": {
                    "ASPNETCORE_ENVIRONMENT": "Development"
                }
            }
        }
    }, indent=2))


def gen_test_project(prog_id, name):
    """Generate test project for one microservice."""
    svc_name = f"TheWatch.{prog_id}.{name}"
    test_name = f"{svc_name}.Tests"
    d = OUT / test_name

    write(d / f"{test_name}.csproj", dedent(f"""\
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <IsPackable>false</IsPackable>
            <RootNamespace>{test_name}</RootNamespace>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
            <PackageReference Include="xunit" Version="2.9.*" />
            <PackageReference Include="xunit.runner.visualstudio" Version="2.8.*" />
            <PackageReference Include="FluentAssertions" Version="8.*" />
            <PackageReference Include="NSubstitute" Version="5.*" />
            <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.*-*" />
          </ItemGroup>

          <ItemGroup>
            <ProjectReference Include="..\\{svc_name}\\{svc_name}.csproj" />
            <ProjectReference Include="..\\TheWatch.Generators\\TheWatch.Generators.csproj"
                              OutputItemType="Analyzer"
                              ReferenceOutputAssembly="false" />
          </ItemGroup>

          <ItemGroup>
            <AdditionalFiles Include="..\\_mapping.json" Link="Data\\_mapping.json" />
          </ItemGroup>
        </Project>
    """))

    write(d / "HealthEndpointTests.cs", dedent(f"""\
        using System.Net;
        using System.Net.Http.Json;
        using FluentAssertions;
        using Microsoft.AspNetCore.Mvc.Testing;
        using TheWatch.Shared.Contracts;

        namespace {test_name};

        public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
        {{
            private readonly HttpClient _client;

            public HealthEndpointTests(WebApplicationFactory<Program> factory)
            {{
                _client = factory.CreateClient();
            }}

            [Fact]
            public async Task Health_ReturnsHealthy()
            {{
                // Act
                var response = await _client.GetAsync("/health");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
                health.Should().NotBeNull();
                health!.Status.Should().Be("Healthy");
                health.Program.Should().Be("{prog_id}");
            }}

            [Fact]
            public async Task Info_ReturnsServiceMetadata()
            {{
                var response = await _client.GetAsync("/info");
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }}
        }}
    """))


def gen_aspire_dashboard():
    """Generate Aspire AppHost with file-explorer-style dashboard."""
    d = OUT / "TheWatch.Aspire.AppHost"

    write(d / "TheWatch.Aspire.AppHost.csproj", dedent("""\
        <Project Sdk="Microsoft.NET.Sdk">
          <Sdk Name="Aspire.AppHost.Sdk" Version="9.2.0" />

          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
            <UserSecretsId>thewatch-aspire-apphost</UserSecretsId>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Aspire.Hosting.AppHost" Version="9.2.0" />
          </ItemGroup>

    """))

    # Add project references for all programs
    refs = ""
    for prog_id, name, *_ in PROGRAMS:
        proj = f"TheWatch.{prog_id}.{name}"
        refs += f'    <ProjectReference Include="..\\{proj}\\{proj}.csproj" />\n'

    csproj = (d / "TheWatch.Aspire.AppHost.csproj").read_text()
    csproj += f"  <ItemGroup>\n{refs}  </ItemGroup>\n\n</Project>\n"
    write(d / "TheWatch.Aspire.AppHost.csproj", csproj)

    # Program.cs with all services
    lines = [
        'var builder = DistributedApplication.CreateBuilder(args);',
        '',
    ]

    for prog_id, name, desc, icon, bi_icon in PROGRAMS:
        proj = f"TheWatch.{prog_id}.{name}"
        var_name = f"p{prog_id[1:]}_{name.lower()}"
        lines.append(f'var {var_name} = builder.AddProject<Projects.{proj.replace(".", "_")}>("{prog_id.lower()}-{name.lower()}")')
        lines.append(f'    .WithExternalHttpEndpoints();')
        lines.append('')

    lines.append('builder.Build().Run();')
    write(d / "Program.cs", '\n'.join(lines))

    write(d / "Properties" / "launchSettings.json", json.dumps({
        "profiles": {
            "TheWatch.Aspire.AppHost": {
                "commandName": "Project",
                "dotnetRunMessages": True,
                "launchBrowser": True,
                "launchUrl": "",
                "environmentVariables": {
                    "ASPNETCORE_ENVIRONMENT": "Development",
                    "DOTNET_ENVIRONMENT": "Development",
                    "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:21287",
                    "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:22057"
                }
            }
        }
    }, indent=2))

    write(d / "appsettings.json", json.dumps({
        "Logging": {
            "LogLevel": {"Default": "Information", "Microsoft.AspNetCore": "Warning"}
        }
    }, indent=2))


def gen_aspire_service_defaults():
    """Generate Aspire ServiceDefaults project."""
    d = OUT / "TheWatch.Aspire.ServiceDefaults"

    write(d / "TheWatch.Aspire.ServiceDefaults.csproj", dedent("""\
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.*" />
            <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="9.*" />
            <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.*" />
            <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
            <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
            <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
            <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.*" />
          </ItemGroup>
        </Project>
    """))

    write(d / "Extensions.cs", dedent("""\
        using Microsoft.AspNetCore.Builder;
        using Microsoft.AspNetCore.Diagnostics.HealthChecks;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Diagnostics.HealthChecks;
        using Microsoft.Extensions.Logging;
        using OpenTelemetry;
        using OpenTelemetry.Metrics;
        using OpenTelemetry.Trace;

        namespace TheWatch.Aspire.ServiceDefaults;

        public static class Extensions
        {
            public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
            {
                builder.ConfigureOpenTelemetry();
                builder.AddDefaultHealthChecks();
                builder.Services.AddServiceDiscovery();
                builder.Services.ConfigureHttpClientDefaults(http =>
                {
                    http.AddStandardResilienceHandler();
                    http.AddServiceDiscovery();
                });
                return builder;
            }

            public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
            {
                builder.Logging.AddOpenTelemetry(logging =>
                {
                    logging.IncludeFormattedMessage = true;
                    logging.IncludeScopes = true;
                });

                builder.Services.AddOpenTelemetry()
                    .WithMetrics(metrics =>
                    {
                        metrics.AddAspNetCoreInstrumentation()
                               .AddHttpClientInstrumentation()
                               .AddRuntimeInstrumentation();
                    })
                    .WithTracing(tracing =>
                    {
                        tracing.AddSource(builder.Environment.ApplicationName)
                               .AddAspNetCoreInstrumentation()
                               .AddHttpClientInstrumentation();
                    });

                builder.AddOpenTelemetryExporters();
                return builder;
            }

            private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
            {
                var useOtlp = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
                if (useOtlp)
                {
                    builder.Services.AddOpenTelemetry().UseOtlpExporter();
                }
                return builder;
            }

            public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
            {
                builder.Services.AddHealthChecks()
                    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
                return builder;
            }

            public static WebApplication MapDefaultEndpoints(this WebApplication app)
            {
                app.MapHealthChecks("/health");
                app.MapHealthChecks("/alive", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("live")
                });
                return app;
            }
        }
    """))


def gen_dashboard_page():
    """Generate a Blazor-style file explorer dashboard page for the Aspire host."""
    d = OUT / "TheWatch.Aspire.AppHost"

    # Since Aspire uses its own dashboard, we create a static HTML page
    # that links to all services — served from wwwroot
    html_parts = []
    html_parts.append("""<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>TheWatch - Service Explorer</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css">
    <style>
        :root { --bg: #0d1117; --card-bg: #161b22; --border: #30363d; --text: #e6edf3;
                --text-dim: #8b949e; --accent: #58a6ff; --success: #3fb950; --warn: #d29922; }
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { background: var(--bg); color: var(--text); font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif; }
        .header { background: var(--card-bg); border-bottom: 1px solid var(--border); padding: 16px 24px; display: flex; align-items: center; gap: 12px; }
        .header h1 { font-size: 20px; font-weight: 600; }
        .header .badge { background: var(--accent); color: var(--bg); padding: 2px 8px; border-radius: 12px; font-size: 12px; font-weight: 600; }
        .breadcrumb { padding: 12px 24px; font-size: 14px; color: var(--text-dim); border-bottom: 1px solid var(--border); }
        .breadcrumb a { color: var(--accent); text-decoration: none; }
        .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 16px; padding: 24px; }
        .card { background: var(--card-bg); border: 1px solid var(--border); border-radius: 8px; padding: 20px; cursor: pointer; transition: all 0.15s; display: flex; gap: 16px; align-items: flex-start; }
        .card:hover { border-color: var(--accent); transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,0.3); }
        .card .icon { font-size: 32px; color: var(--accent); flex-shrink: 0; width: 48px; height: 48px; display: flex; align-items: center; justify-content: center; background: rgba(88,166,255,0.1); border-radius: 8px; }
        .card .info { flex: 1; min-width: 0; }
        .card .info h3 { font-size: 15px; font-weight: 600; margin-bottom: 4px; }
        .card .info .program { font-size: 12px; color: var(--accent); font-weight: 600; margin-bottom: 4px; }
        .card .info p { font-size: 13px; color: var(--text-dim); line-height: 1.4; }
        .card .info .links { margin-top: 8px; display: flex; gap: 8px; flex-wrap: wrap; }
        .card .info .links a { font-size: 12px; color: var(--accent); text-decoration: none; padding: 2px 8px; border: 1px solid var(--border); border-radius: 4px; }
        .card .info .links a:hover { background: rgba(88,166,255,0.1); }
        .status { display: inline-block; width: 8px; height: 8px; border-radius: 50%; background: var(--success); margin-right: 4px; }
        .footer { padding: 24px; text-align: center; color: var(--text-dim); font-size: 13px; border-top: 1px solid var(--border); }
        .search { padding: 16px 24px; }
        .search input { width: 100%; padding: 8px 12px; background: var(--card-bg); border: 1px solid var(--border); border-radius: 6px; color: var(--text); font-size: 14px; }
        .search input:focus { outline: none; border-color: var(--accent); }
    </style>
</head>
<body>
    <div class="header">
        <i class="bi bi-shield-check" style="font-size:24px;color:var(--accent)"></i>
        <h1>TheWatch Service Explorer</h1>
        <span class="badge">10 Services</span>
    </div>
    <div class="breadcrumb">
        <a href="#">TheWatch</a> / <a href="#">Microservices</a> / All Programs
    </div>
    <div class="search">
        <input type="text" id="search" placeholder="Search services... (Ctrl+K)" oninput="filterCards(this.value)">
    </div>
    <div class="grid" id="grid">
""")

    for prog_id, name, desc, icon, bi_icon in PROGRAMS:
        port_https = 5100 + int(prog_id[1:])
        port_http = 5200 + int(prog_id[1:])
        proj = f"TheWatch.{prog_id}.{name}"
        html_parts.append(f"""
        <div class="card" data-name="{name.lower()} {prog_id.lower()} {desc.lower()}" onclick="window.open('https://localhost:{port_https}/swagger','_blank')">
            <div class="icon"><i class="bi {bi_icon}"></i></div>
            <div class="info">
                <span class="program">{prog_id}</span>
                <h3><span class="status"></span>{name}</h3>
                <p>{desc}</p>
                <div class="links">
                    <a href="https://localhost:{port_https}/swagger" target="_blank">Swagger</a>
                    <a href="https://localhost:{port_https}/health" target="_blank">Health</a>
                    <a href="https://localhost:{port_https}/hangfire" target="_blank">Hangfire</a>
                    <a href="https://localhost:{port_https}/info" target="_blank">Info</a>
                </div>
            </div>
        </div>
""")

    html_parts.append("""
    </div>
    <div class="footer">
        TheWatch Platform &mdash; 10 Programs, 2,902 API Operations &mdash; .NET 10 + Hangfire Pro + Aspire
    </div>
    <script>
        function filterCards(q) {
            q = q.toLowerCase();
            document.querySelectorAll('.card').forEach(c => {
                c.style.display = c.dataset.name.includes(q) ? '' : 'none';
            });
        }
        document.addEventListener('keydown', e => {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                document.getElementById('search').focus();
            }
        });
        // Auto-check health on load
        document.querySelectorAll('.card').forEach(card => {
            const healthLink = card.querySelector('a[href*="health"]');
            if (healthLink) {
                fetch(healthLink.href, {mode:'no-cors'}).catch(() => {
                    const dot = card.querySelector('.status');
                    if (dot) { dot.style.background = '#d29922'; }
                });
            }
        });
    </script>
</body>
</html>""")

    write(d / "wwwroot" / "index.html", "".join(html_parts))


def gen_solution():
    """Generate TheWatch.sln."""
    lines = [
        "",
        "Microsoft Visual Studio Solution File, Format Version 12.00",
        "# Visual Studio Version 17",
        "VisualStudioVersion = 17.0.31903.59",
        "MinimumVisualStudioVersion = 10.0.40219.1",
    ]

    import uuid

    # Folders
    sln_folder_id = str(uuid.uuid4()).upper()
    test_folder_id = str(uuid.uuid4()).upper()

    projects = []
    test_projects = []

    # Shared
    pid = str(uuid.uuid4()).upper()
    projects.append(("TheWatch.Shared", f"TheWatch.Shared\\TheWatch.Shared.csproj", pid))

    # Generators
    pid = str(uuid.uuid4()).upper()
    projects.append(("TheWatch.Generators", f"TheWatch.Generators\\TheWatch.Generators.csproj", pid))

    # Service projects
    for prog_id, name, *_ in PROGRAMS:
        proj = f"TheWatch.{prog_id}.{name}"
        pid = str(uuid.uuid4()).upper()
        projects.append((proj, f"{proj}\\{proj}.csproj", pid))

        # Test project
        tproj = f"{proj}.Tests"
        tpid = str(uuid.uuid4()).upper()
        test_projects.append((tproj, f"{tproj}\\{tproj}.csproj", tpid))

    # Aspire
    pid = str(uuid.uuid4()).upper()
    projects.append(("TheWatch.Aspire.AppHost", f"TheWatch.Aspire.AppHost\\TheWatch.Aspire.AppHost.csproj", pid))
    pid = str(uuid.uuid4()).upper()
    projects.append(("TheWatch.Aspire.ServiceDefaults", f"TheWatch.Aspire.ServiceDefaults\\TheWatch.Aspire.ServiceDefaults.csproj", pid))

    faq = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"
    folder_type = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}"

    # Solution folders
    lines.append(f'Project("{folder_type}") = "tests", "tests", "{{{test_folder_id}}}"')
    lines.append("EndProject")

    for name, path, pid in projects + test_projects:
        lines.append(f'Project("{faq}") = "{name}", "{path}", "{{{pid}}}"')
        lines.append("EndProject")

    lines.append("Global")
    lines.append("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution")
    lines.append("\t\tDebug|Any CPU = Debug|Any CPU")
    lines.append("\t\tRelease|Any CPU = Release|Any CPU")
    lines.append("\tEndGlobalSection")
    lines.append("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution")
    for _, _, pid in projects + test_projects:
        lines.append(f"\t\t{{{pid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU")
        lines.append(f"\t\t{{{pid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU")
        lines.append(f"\t\t{{{pid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU")
        lines.append(f"\t\t{{{pid}}}.Release|Any CPU.Build.0 = Release|Any CPU")
    lines.append("\tEndGlobalSection")

    # Nest test projects under test folder
    lines.append("\tGlobalSection(NestedProjects) = preSolution")
    for _, _, tpid in test_projects:
        lines.append(f"\t\t{{{tpid}}} = {{{test_folder_id}}}")
    lines.append("\tEndGlobalSection")

    lines.append("EndGlobal")
    lines.append("")

    write(OUT / "TheWatch.sln", "\n".join(lines))


def copy_data_files():
    """Copy/link consolidated JSON files into the Microservices folder."""
    src = Path("E:/json_output")
    for f in ["Models.json", "Interfaces.json", "Services.json", "Controllers.json"]:
        source = src / f
        dest = OUT / f
        if source.exists() and not dest.exists():
            import shutil
            shutil.copy2(source, dest)
            print(f"  Copied {f}")


def main():
    print("Generating TheWatch Microservices solution...")

    print("\n1. Copying data files...")
    copy_data_files()

    print("2. Generating TheWatch.Shared...")
    gen_shared()

    print("3. Generating microservice projects...")
    for prog_id, name, desc, icon, bi_icon in PROGRAMS:
        proj = f"TheWatch.{prog_id}.{name}"
        print(f"   {proj}")
        gen_service_project(prog_id, name, desc, icon, bi_icon)

    print("4. Generating test projects...")
    for prog_id, name, *_ in PROGRAMS:
        test = f"TheWatch.{prog_id}.{name}.Tests"
        print(f"   {test}")
        gen_test_project(prog_id, name)

    print("5. Generating Aspire AppHost...")
    gen_aspire_dashboard()
    gen_dashboard_page()

    print("6. Generating Aspire ServiceDefaults...")
    gen_aspire_service_defaults()

    print("7. Generating solution file...")
    gen_solution()

    print("\nDone! Solution at: E:\\json_output\\Microservices\\TheWatch.sln")

    # Summary
    total_projects = 2 + len(PROGRAMS) * 2 + 2  # shared + generators + services + tests + aspire x2
    print(f"\nTotal projects: {total_projects}")
    print(f"  Shared:       1")
    print(f"  Generators:   1")
    print(f"  Services:     {len(PROGRAMS)}")
    print(f"  Tests:        {len(PROGRAMS)}")
    print(f"  Aspire:       2")


if __name__ == "__main__":
    main()
