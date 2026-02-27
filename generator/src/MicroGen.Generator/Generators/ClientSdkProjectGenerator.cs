using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates a typed HTTP client SDK project (*.Client) for consuming the service API.
/// Produces strongly-typed methods per operation, DI registration, and Polly retry policies.
/// </summary>
public sealed class ClientSdkProjectGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public ClientSdkProjectGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
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
        var projectName = $"{service.PascalName}.Client";
        _logger.LogDebug("  Generating {Project}...", projectName);

        // ─── Project file ───
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            $"{projectName}.csproj",
            _engine.Render(Templates.ProjectFile, new { Service = service, Config = _config }),
            ct);

        // ─── DI Extension ───
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "DependencyInjection.cs",
            _engine.Render(Templates.DependencyInjection, new { Service = service, Config = _config }),
            ct);

        // ─── Configuration Options ───
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            $"{service.PascalName}ClientOptions.cs",
            _engine.Render(Templates.ClientOptions, new { Service = service }),
            ct);

        // ─── Base client ───
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            $"Base{service.PascalName}Client.cs",
            _engine.Render(Templates.BaseClient, new { Service = service }),
            ct);

        // ─── Per-tag typed client ───
        foreach (var tag in service.Tags)
        {
            var tagName = tag.Name ?? "Default";
            var ops = service.Operations
                .Where(o => o.Tag != null && o.Tag.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (ops.Count == 0) continue;

            var queryOps = ops.Where(o => o.IsQuery).ToList();
            var commandOps = ops.Where(o => o.IsCommand).ToList();

            // Interface
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Clients", $"I{tag.PascalName}Client.cs"),
                _engine.Render(Templates.ClientInterface, new
                {
                    Service = service,
                    Tag = tag,
                    Operations = ops,
                    QueryOps = queryOps,
                    CommandOps = commandOps
                }),
                ct);

            // Implementation
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Clients", $"{tag.PascalName}Client.cs"),
                _engine.Render(Templates.ClientImplementation, new
                {
                    Service = service,
                    Tag = tag,
                    Operations = ops,
                    QueryOps = queryOps,
                    CommandOps = commandOps
                }),
                ct);
        }

        // ─── Shared models (re-export Application DTOs as Client models) ───
        foreach (var schema in service.Schemas.Where(s => !s.IsEntity || s.Properties.Count > 0))
        {
            await emitter.EmitToProjectAsync(serviceRoot, projectName,
                Path.Combine("Models", $"{schema.PascalName}Model.cs"),
                _engine.Render(Templates.ClientModel, new { Service = service, Schema = schema }),
                ct);
        }

        // ─── Resilience / Polly policies ───
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Resilience/HttpResiliencePolicies.cs",
            _engine.Render(Templates.ResiliencePolicies, new { Service = service }),
            ct);

        // ─── Delegating handler for auth ───
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            "Handlers/AuthenticationDelegatingHandler.cs",
            _engine.Render(Templates.AuthHandler, new { Service = service }),
            ct);

        _logger.LogDebug("    Generated Client SDK: {Count} tag clients, {Count2} models",
            service.Tags.Count, service.Schemas.Count);
    }

    private static class Templates
    {
        public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{{ Config.TargetFramework }}</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                <RootNamespace>{{ Service.PascalName }}.Client</RootNamespace>
                <Description>Typed HTTP client SDK for the {{ Service.Title }} API.</Description>
                <PackageId>{{ Service.PascalName }}.Client</PackageId>
                <Version>{{ Service.Version }}</Version>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Microsoft.Extensions.Http" Version="9.*" />
                <PackageReference Include="Microsoft.Extensions.Http.Polly" Version="9.*" />
                <PackageReference Include="Microsoft.Extensions.Options" Version="9.*" />
                <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.*" />
                <PackageReference Include="System.Text.Json" Version="9.*" />
                <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.*" />
            {{~ if Config.Features.AspireAppHost ~}}
                <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="9.*" />
            {{~ end ~}}
              </ItemGroup>
            </Project>
            """;

        public const string DependencyInjection = """
            using {{ Service.PascalName }}.Client.Clients;
            using {{ Service.PascalName }}.Client.Handlers;
            using {{ Service.PascalName }}.Client.Resilience;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Options;

            namespace {{ Service.PascalName }}.Client;

            /// <summary>
            /// Registers {{ Service.PascalName }} typed HTTP clients with DI.
            /// </summary>
            public static class DependencyInjection
            {
                /// <summary>
                /// Adds {{ Service.PascalName }} API clients with Polly resilience and optional auth handler.
                /// </summary>
                public static IServiceCollection Add{{ Service.PascalName }}Client(
                    this IServiceCollection services,
                    Action<{{ Service.PascalName }}ClientOptions> configureOptions)
                {
                    services.Configure(configureOptions);
                    services.AddTransient<AuthenticationDelegatingHandler>();

            {{~ for tag in Service.Tags ~}}
                    services.AddHttpClient<I{{ tag.PascalName }}Client, {{ tag.PascalName }}Client>((sp, client) =>
                    {
                        var options = sp.GetRequiredService<IOptions<{{ Service.PascalName }}ClientOptions>>().Value;
                        client.BaseAddress = new Uri(options.BaseUrl);
                        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                        client.DefaultRequestHeaders.Add("Accept", "application/json");
                    })
                    .AddHttpMessageHandler<AuthenticationDelegatingHandler>()
            {{~ if Config.Features.AspireAppHost ~}}
                    .AddServiceDiscovery()
            {{~ end ~}}
                    .AddStandardResilienceHandler();
            {{~ end ~}}

                    return services;
                }
            }
            """;

        public const string ClientOptions = """
            namespace {{ Service.PascalName }}.Client;

            /// <summary>
            /// Configuration options for the {{ Service.PascalName }} API client SDK.
            /// </summary>
            public sealed class {{ Service.PascalName }}ClientOptions
            {
                /// <summary>Base URL of the API (e.g. "https://api.example.com/api/v1").</summary>
                public string BaseUrl { get; set; } = "https://localhost:5001";

                /// <summary>Request timeout in seconds.</summary>
                public int TimeoutSeconds { get; set; } = 30;

                /// <summary>API key for authentication (if using API key auth).</summary>
                public string? ApiKey { get; set; }

                /// <summary>Bearer token for JWT authentication.</summary>
                public string? BearerToken { get; set; }

                /// <summary>Custom headers to include on every request.</summary>
                public Dictionary<string, string> DefaultHeaders { get; set; } = [];
            }
            """;

        public const string BaseClient = """
            using System.Net.Http.Json;
            using System.Text.Json;

            namespace {{ Service.PascalName }}.Client;

            /// <summary>
            /// Base class for typed HTTP clients with shared serialization and error handling.
            /// </summary>
            public abstract class Base{{ Service.PascalName }}Client
            {
                protected readonly HttpClient Http;

                protected static readonly JsonSerializerOptions JsonOptions = new()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = false
                };

                protected Base{{ Service.PascalName }}Client(HttpClient http)
                {
                    Http = http;
                }

                protected async Task<T?> GetAsync<T>(string path, CancellationToken ct = default)
                {
                    var response = await Http.GetAsync(path, ct);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
                }

                protected async Task<TResponse?> PostAsync<TRequest, TResponse>(
                    string path, TRequest body, CancellationToken ct = default)
                {
                    var response = await Http.PostAsJsonAsync(path, body, JsonOptions, ct);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
                }

                protected async Task<TResponse?> PutAsync<TRequest, TResponse>(
                    string path, TRequest body, CancellationToken ct = default)
                {
                    var response = await Http.PutAsJsonAsync(path, body, JsonOptions, ct);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
                }

                protected async Task<TResponse?> PatchAsync<TRequest, TResponse>(
                    string path, TRequest body, CancellationToken ct = default)
                {
                    var content = JsonContent.Create(body, options: JsonOptions);
                    var response = await Http.PatchAsync(path, content, ct);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
                }

                protected async Task DeleteAsync(string path, CancellationToken ct = default)
                {
                    var response = await Http.DeleteAsync(path, ct);
                    response.EnsureSuccessStatusCode();
                }

                protected async Task<HttpResponseMessage> SendRawAsync(
                    HttpMethod method, string path, HttpContent? content = null, CancellationToken ct = default)
                {
                    var request = new HttpRequestMessage(method, path) { Content = content };
                    return await Http.SendAsync(request, ct);
                }
            }
            """;

        public const string ClientInterface = """
            using {{ Service.PascalName }}.Client.Models;

            namespace {{ Service.PascalName }}.Client.Clients;

            /// <summary>
            /// Typed HTTP client interface for {{ Tag.PascalName }} operations.
            /// {{ Tag.Description }}
            /// </summary>
            public interface I{{ Tag.PascalName }}Client
            {
            {{~ for op in QueryOps ~}}
                /// <summary>{{ op.Summary }}</summary>
                Task<{{ op.PascalOperationId }}ResponseModel?> {{ op.PascalOperationId }}Async(
            {{~ for p in op.PathParameters ~}}
                    {{ p.CSharpType }} {{ p.CamelName }},
            {{~ end ~}}
            {{~ for p in op.QueryParameters ~}}
                    {{ p.CSharpType }}{{ if !p.Required }}?{{ end }} {{ p.CamelName }}{{ if !p.Required }} = default{{ end }},
            {{~ end ~}}
                    CancellationToken cancellationToken = default);
            {{~ end ~}}
            {{~ for op in CommandOps ~}}
                /// <summary>{{ op.Summary }}</summary>
                Task<{{ op.PascalOperationId }}ResponseModel?> {{ op.PascalOperationId }}Async(
            {{~ for p in op.PathParameters ~}}
                    {{ p.CSharpType }} {{ p.CamelName }},
            {{~ end ~}}
            {{~ if op.HasRequestBody ~}}
                    {{ op.PascalOperationId }}RequestModel request,
            {{~ end ~}}
                    CancellationToken cancellationToken = default);
            {{~ end ~}}
            }
            """;

        public const string ClientImplementation = """
            using {{ Service.PascalName }}.Client.Models;

            namespace {{ Service.PascalName }}.Client.Clients;

            /// <summary>
            /// Typed HTTP client for {{ Tag.PascalName }} operations.
            /// </summary>
            public sealed class {{ Tag.PascalName }}Client : Base{{ Service.PascalName }}Client, I{{ Tag.PascalName }}Client
            {
                public {{ Tag.PascalName }}Client(HttpClient http) : base(http) { }

            {{~ for op in QueryOps ~}}
                /// <inheritdoc />
                public async Task<{{ op.PascalOperationId }}ResponseModel?> {{ op.PascalOperationId }}Async(
            {{~ for p in op.PathParameters ~}}
                    {{ p.CSharpType }} {{ p.CamelName }},
            {{~ end ~}}
            {{~ for p in op.QueryParameters ~}}
                    {{ p.CSharpType }}{{ if !p.Required }}?{{ end }} {{ p.CamelName }}{{ if !p.Required }} = default{{ end }},
            {{~ end ~}}
                    CancellationToken cancellationToken = default)
                {
                    var path = $"{{ op.Path }}";
            {{~ for p in op.PathParameters ~}}
                    path = path.Replace("{" + "{{ p.Name }}" + "}", {{ p.CamelName }}.ToString());
            {{~ end ~}}
                    return await GetAsync<{{ op.PascalOperationId }}ResponseModel>(path, cancellationToken);
                }

            {{~ end ~}}
            {{~ for op in CommandOps ~}}
                /// <inheritdoc />
                public async Task<{{ op.PascalOperationId }}ResponseModel?> {{ op.PascalOperationId }}Async(
            {{~ for p in op.PathParameters ~}}
                    {{ p.CSharpType }} {{ p.CamelName }},
            {{~ end ~}}
            {{~ if op.HasRequestBody ~}}
                    {{ op.PascalOperationId }}RequestModel request,
            {{~ end ~}}
                    CancellationToken cancellationToken = default)
                {
                    var path = $"{{ op.Path }}";
            {{~ for p in op.PathParameters ~}}
                    path = path.Replace("{" + "{{ p.Name }}" + "}", {{ p.CamelName }}.ToString());
            {{~ end ~}}
            {{~ if op.HttpMethod == "POST" ~}}
                    return await PostAsync<{{ if op.HasRequestBody }}{{ op.PascalOperationId }}RequestModel{{ else }}object{{ end }}, {{ op.PascalOperationId }}ResponseModel>(
                        path, {{ if op.HasRequestBody }}request{{ else }}new { }{{ end }}, cancellationToken);
            {{~ else if op.HttpMethod == "PUT" ~}}
                    return await PutAsync<{{ if op.HasRequestBody }}{{ op.PascalOperationId }}RequestModel{{ else }}object{{ end }}, {{ op.PascalOperationId }}ResponseModel>(
                        path, {{ if op.HasRequestBody }}request{{ else }}new { }{{ end }}, cancellationToken);
            {{~ else if op.HttpMethod == "PATCH" ~}}
                    return await PatchAsync<{{ if op.HasRequestBody }}{{ op.PascalOperationId }}RequestModel{{ else }}object{{ end }}, {{ op.PascalOperationId }}ResponseModel>(
                        path, {{ if op.HasRequestBody }}request{{ else }}new { }{{ end }}, cancellationToken);
            {{~ else if op.HttpMethod == "DELETE" ~}}
                    await DeleteAsync(path, cancellationToken);
                    return new {{ op.PascalOperationId }}ResponseModel();
            {{~ end ~}}
                }

            {{~ end ~}}
            }
            """;

        public const string ClientModel = """
            namespace {{ Service.PascalName }}.Client.Models;

            /// <summary>
            /// Client model for {{ Schema.PascalName }}.
            /// {{ Schema.Description }}
            /// </summary>
            public sealed record {{ Schema.PascalName }}Model
            {
            {{~ for prop in Schema.Properties ~}}
                /// <summary>{{ prop.Description }}</summary>
                public {{ prop.CSharpType }}{{ if !prop.Required }}?{{ end }} {{ prop.PascalName }} { get; init; }
            {{~ end ~}}
            }
            """;

        public const string ResiliencePolicies = """
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Http.Resilience;

            namespace {{ Service.PascalName }}.Client.Resilience;

            /// <summary>
            /// Configures HTTP resilience policies (retry, circuit breaker, timeout)
            /// for {{ Service.PascalName }} API clients.
            /// </summary>
            public static class HttpResiliencePolicies
            {
                /// <summary>
                /// Adds standard resilience with customized retry and circuit breaker settings.
                /// </summary>
                public static IHttpClientBuilder AddCustomResilience(this IHttpClientBuilder builder)
                {
                    builder.AddStandardResilienceHandler(options =>
                    {
                        options.Retry.MaxRetryAttempts = 3;
                        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                        options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;

                        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
                        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                        options.CircuitBreaker.MinimumThroughput = 10;
                        options.CircuitBreaker.FailureRatio = 0.5;

                        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                    });

                    return builder;
                }
            }
            """;

        public const string AuthHandler = """
            using Microsoft.Extensions.Options;

            namespace {{ Service.PascalName }}.Client.Handlers;

            /// <summary>
            /// Adds authentication headers to outgoing HTTP requests.
            /// Supports API key and Bearer token authentication.
            /// </summary>
            public class AuthenticationDelegatingHandler : DelegatingHandler
            {
                private readonly {{ Service.PascalName }}ClientOptions _options;

                public AuthenticationDelegatingHandler(IOptions<{{ Service.PascalName }}ClientOptions> options)
                {
                    _options = options.Value;
                }

                protected override async Task<HttpResponseMessage> SendAsync(
                    HttpRequestMessage request,
                    CancellationToken cancellationToken)
                {
                    // Bearer token
                    if (!string.IsNullOrEmpty(_options.BearerToken))
                    {
                        request.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.BearerToken);
                    }

                    // API key
                    if (!string.IsNullOrEmpty(_options.ApiKey))
                    {
                        request.Headers.Add("X-API-Key", _options.ApiKey);
                    }

                    // Custom headers
                    foreach (var (key, value) in _options.DefaultHeaders)
                    {
                        if (!request.Headers.Contains(key))
                            request.Headers.Add(key, value);
                    }

                    return await base.SendAsync(request, cancellationToken);
                }
            }
            """;
    }
}
