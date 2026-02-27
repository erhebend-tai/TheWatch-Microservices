using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates a YARP (Yet Another Reverse Proxy) based .NET API Gateway project.
/// Includes route configuration, authentication, rate limiting, health aggregation,
/// Swagger aggregation, Dockerfile, and K8s deployment manifests.
/// One gateway per domain.
/// </summary>
public sealed class ApiGatewayGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public ApiGatewayGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
    {
        _engine = engine;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Generates the API Gateway for an entire domain.
    /// </summary>
    public async Task GenerateDomainGatewayAsync(
        DomainDescriptor domain,
        string domainRoot,
        FileEmitter emitter,
        CancellationToken ct)
    {
        var projectName = $"{domain.PascalName}Gateway";
        var gatewayRoot = Path.Combine(domainRoot, "gateway", "src", projectName);

        _logger.LogDebug("  Generating API Gateway for domain {Domain}...", domain.DomainName);

        // ─── Project file ───
        await emitter.EmitAsync(
            Path.Combine(gatewayRoot, $"{projectName}.csproj"),
            _engine.Render(Templates.ProjectFile, new { Domain = domain, Config = _config }),
            ct);

        // ─── Program.cs ───
        await emitter.EmitAsync(
            Path.Combine(gatewayRoot, "Program.cs"),
            _engine.Render(Templates.ProgramCs, new { Domain = domain, Config = _config }),
            ct);

        // ─── appsettings.json with YARP route config ───
        await emitter.EmitAsync(
            Path.Combine(gatewayRoot, "appsettings.json"),
            _engine.Render(Templates.AppSettings, new { Domain = domain, Config = _config }),
            ct);

        // ─── appsettings.Development.json ───
        await emitter.EmitAsync(
            Path.Combine(gatewayRoot, "appsettings.Development.json"),
            _engine.Render(Templates.AppSettingsDev, new { Domain = domain }),
            ct);

        // ─── Rate limiting middleware ───
        await emitter.EmitAsync(
            Path.Combine(gatewayRoot, "Middleware", "RateLimitingMiddleware.cs"),
            _engine.Render(Templates.RateLimitMiddleware, new { Domain = domain }),
            ct);

        // ─── Correlation ID middleware ───
        await emitter.EmitAsync(
            Path.Combine(gatewayRoot, "Middleware", "CorrelationIdMiddleware.cs"),
            _engine.Render(Templates.CorrelationMiddleware, new { Domain = domain }),
            ct);

        // ─── Request logging middleware ───
        await emitter.EmitAsync(
            Path.Combine(gatewayRoot, "Middleware", "RequestLoggingMiddleware.cs"),
            _engine.Render(Templates.RequestLoggingMiddleware, new { Domain = domain }),
            ct);

        // ─── Health aggregation endpoint ───
        await emitter.EmitAsync(
            Path.Combine(gatewayRoot, "Health", "AggregatedHealthCheck.cs"),
            _engine.Render(Templates.AggregatedHealthCheck, new { Domain = domain }),
            ct);

        // ─── Swagger aggregation ───
        await emitter.EmitAsync(
            Path.Combine(gatewayRoot, "Swagger", "SwaggerAggregator.cs"),
            _engine.Render(Templates.SwaggerAggregator, new { Domain = domain }),
            ct);

        // ─── Auth configuration ───
        await emitter.EmitAsync(
            Path.Combine(gatewayRoot, "Auth", "GatewayAuthConfiguration.cs"),
            _engine.Render(Templates.AuthConfig, new { Domain = domain, Config = _config }),
            ct);

        // ─── Dockerfile ───
        await emitter.EmitAsync(
            Path.Combine(domainRoot, "gateway", "Dockerfile"),
            _engine.Render(Templates.Dockerfile, new { Domain = domain, Config = _config }),
            ct);

        // ─── K8s deployment ───
        await emitter.EmitAsync(
            Path.Combine(domainRoot, "gateway", "k8s", "gateway-deployment.yaml"),
            _engine.Render(Templates.K8sDeployment, new { Domain = domain }),
            ct);

        await emitter.EmitAsync(
            Path.Combine(domainRoot, "gateway", "k8s", "gateway-service.yaml"),
            _engine.Render(Templates.K8sService, new { Domain = domain }),
            ct);

        await emitter.EmitAsync(
            Path.Combine(domainRoot, "gateway", "k8s", "gateway-hpa.yaml"),
            _engine.Render(Templates.K8sHPA, new { Domain = domain }),
            ct);

        _logger.LogDebug("    Generated API Gateway with {Count} service routes", domain.Services.Count);
    }

    private static class Templates
    {
        public const string ProjectFile = """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{{ Config.TargetFramework }}</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <RootNamespace>{{ Domain.PascalName }}Gateway</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Yarp.ReverseProxy" Version="2.*" />
                <PackageReference Include="Serilog.AspNetCore" Version="8.*" />
                <PackageReference Include="Asp.Versioning.Mvc" Version="8.*" />
                <PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="8.*" />
                <PackageReference Include="AspNetCore.HealthChecks.Uris" Version="8.*" />
                <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.*-*" />
                <PackageReference Include="Microsoft.AspNetCore.RateLimiting" Version="10.*-*" />
                <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
              </ItemGroup>
            </Project>
            """;

        public const string ProgramCs = """
            using {{ Domain.PascalName }}Gateway.Auth;
            using {{ Domain.PascalName }}Gateway.Health;
            using {{ Domain.PascalName }}Gateway.Middleware;
            using {{ Domain.PascalName }}Gateway.Swagger;
            using Microsoft.AspNetCore.RateLimiting;
            using Serilog;
            using System.Threading.RateLimiting;

            var builder = WebApplication.CreateBuilder(args);

            // ─── Serilog ───
            builder.Host.UseSerilog((context, services, config) => config
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("ServiceName", "{{ Domain.PascalName }}Gateway"));

            // ─── Authentication ───
            builder.Services.AddGatewayAuth(builder.Configuration);

            // ─── Rate Limiting ───
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddFixedWindowLimiter("fixed", opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 10;
                });

                options.AddSlidingWindowLimiter("sliding", opt =>
                {
                    opt.PermitLimit = 200;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.SegmentsPerWindow = 4;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 20;
                });

                options.AddTokenBucketLimiter("token", opt =>
                {
                    opt.TokenLimit = 100;
                    opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
                    opt.TokensPerPeriod = 20;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 10;
                });
            });

            // ─── YARP Reverse Proxy ───
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            // ─── Health Checks ───
            var healthBuilder = builder.Services.AddHealthChecks();
            {{~ for svc in Domain.Services ~}}
            healthBuilder.AddUrlGroup(
                new Uri("http://{{ svc.KebabName }}:8080/health/live"),
                name: "{{ svc.KebabName }}",
                tags: ["service", "{{ Domain.DomainName | string.downcase }}"]);
            {{~ end ~}}

            // ─── Swagger ───
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "{{ Domain.PascalName }} API Gateway",
                    Version = "v1",
                    Description = "Aggregated API gateway for {{ Domain.DomainName }} domain services"
                });
            });
            builder.Services.AddSingleton<SwaggerAggregator>();

            // ─── CORS ───
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("GatewayPolicy", policy =>
                {
                    policy.WithOrigins("https://localhost:3000", "https://*.thewatch.app")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetPreflightMaxAge(TimeSpan.FromHours(24));
                });
            });

            var app = builder.Build();

            // ─── Middleware Pipeline ───
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();

            app.UseSerilogRequestLogging();
            app.UseCors("GatewayPolicy");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            // ─── Swagger UI ───
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "{{ Domain.PascalName }} Gateway v1");
            {{~ for svc in Domain.Services ~}}
                    c.SwaggerEndpoint("/swagger/{{ svc.KebabName }}/swagger.json", "{{ svc.PascalName }}");
            {{~ end ~}}
                });
            }

            // ─── Health endpoints ───
            app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
            app.MapHealthChecks("/health/ready");
            app.MapGet("/health/services", AggregatedHealthCheck.CheckAllAsync);

            // ─── YARP ───
            app.MapReverseProxy();

            app.Run();
            """;

        public const string AppSettings = """
            {
              "Serilog": {
                "MinimumLevel": {
                  "Default": "Information",
                  "Override": {
                    "Microsoft": "Warning",
                    "Yarp": "Information"
                  }
                },
                "WriteTo": [{ "Name": "Console" }],
                "Enrich": ["FromLogContext", "WithMachineName"]
              },
              "AllowedHosts": "*",
              "Authentication": {
                "JwtBearer": {
                  "Authority": "https://auth.thewatch.app",
                  "Audience": "{{ Domain.DomainName | string.downcase }}-api"
                }
              },
              "ReverseProxy": {
                "Routes": {
            {{~ for svc in Domain.Services ~}}
                  "{{ svc.KebabName }}-route": {
                    "ClusterId": "{{ svc.KebabName }}-cluster",
                    "RateLimiterPolicy": "sliding",
                    "AuthorizationPolicy": "default",
                    "Match": {
                      "Path": "/api/{{ svc.KebabName }}/{**catch-all}"
                    },
                    "Transforms": [
                      { "PathRemovePrefix": "/api/{{ svc.KebabName }}" },
                      { "PathPrefix": "/api" },
                      { "RequestHeader": "X-Forwarded-Service", "Set": "{{ svc.KebabName }}" }
                    ]
                  },
                  "{{ svc.KebabName }}-health-route": {
                    "ClusterId": "{{ svc.KebabName }}-cluster",
                    "Match": {
                      "Path": "/services/{{ svc.KebabName }}/health/{**catch-all}"
                    },
                    "Transforms": [
                      { "PathRemovePrefix": "/services/{{ svc.KebabName }}" }
                    ]
                  },
                  "{{ svc.KebabName }}-swagger-route": {
                    "ClusterId": "{{ svc.KebabName }}-cluster",
                    "Match": {
                      "Path": "/swagger/{{ svc.KebabName }}/{**catch-all}"
                    },
                    "Transforms": [
                      { "PathRemovePrefix": "/swagger/{{ svc.KebabName }}" },
                      { "PathPrefix": "/swagger" }
                    ]
                  }{{ if !for.last }},{{ end }}
            {{~ end ~}}
                },
                "Clusters": {
            {{~ for svc in Domain.Services ~}}
                  "{{ svc.KebabName }}-cluster": {
                    "LoadBalancingPolicy": "RoundRobin",
                    "HealthCheck": {
                      "Active": {
                        "Enabled": true,
                        "Interval": "00:00:30",
                        "Timeout": "00:00:10",
                        "Policy": "ConsecutiveFailures",
                        "Path": "/health/live"
                      }
                    },
                    "Destinations": {
                      "{{ svc.KebabName }}-primary": {
                        "Address": "http://{{ svc.KebabName }}:8080"
                      }
                    }
                  }{{ if !for.last }},{{ end }}
            {{~ end ~}}
                }
              }
            }
            """;

        public const string AppSettingsDev = """
            {
              "Serilog": {
                "MinimumLevel": {
                  "Default": "Debug"
                }
              },
              "Authentication": {
                "JwtBearer": {
                  "Authority": "https://localhost:5001",
                  "RequireHttpsMetadata": false
                }
              }
            }
            """;

        public const string RateLimitMiddleware = """
            namespace {{ Domain.PascalName }}Gateway.Middleware;

            /// <summary>
            /// Custom rate limiting middleware that adds rate limit headers to responses.
            /// Works in conjunction with ASP.NET Core's built-in rate limiter.
            /// </summary>
            public class RateLimitingMiddleware
            {
                private readonly RequestDelegate _next;

                public RateLimitingMiddleware(RequestDelegate next) => _next = next;

                public async Task InvokeAsync(HttpContext context)
                {
                    await _next(context);

                    // Add rate limit headers if not already present
                    if (!context.Response.Headers.ContainsKey("X-RateLimit-Limit"))
                    {
                        context.Response.Headers["X-RateLimit-Limit"] = "200";
                    }
                }
            }
            """;

        public const string CorrelationMiddleware = """
            namespace {{ Domain.PascalName }}Gateway.Middleware;

            /// <summary>
            /// Ensures every request has a correlation ID for distributed tracing.
            /// </summary>
            public class CorrelationIdMiddleware
            {
                private const string Header = "X-Correlation-ID";
                private readonly RequestDelegate _next;

                public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

                public async Task InvokeAsync(HttpContext context)
                {
                    if (!context.Request.Headers.ContainsKey(Header))
                    {
                        context.Request.Headers[Header] = Guid.NewGuid().ToString();
                    }

                    var correlationId = context.Request.Headers[Header].ToString();
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers[Header] = correlationId;
                        return Task.CompletedTask;
                    });

                    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
                    {
                        await _next(context);
                    }
                }
            }
            """;

        public const string RequestLoggingMiddleware = """
            using System.Diagnostics;

            namespace {{ Domain.PascalName }}Gateway.Middleware;

            /// <summary>
            /// Logs incoming requests with timing, upstream service, and status code.
            /// </summary>
            public class RequestLoggingMiddleware
            {
                private readonly RequestDelegate _next;
                private readonly ILogger<RequestLoggingMiddleware> _logger;

                public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
                {
                    _next = next;
                    _logger = logger;
                }

                public async Task InvokeAsync(HttpContext context)
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        await _next(context);
                        sw.Stop();
                        _logger.LogInformation(
                            "Gateway {Method} {Path} → {StatusCode} in {Elapsed}ms",
                            context.Request.Method, context.Request.Path,
                            context.Response.StatusCode, sw.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        _logger.LogError(ex,
                            "Gateway {Method} {Path} FAILED after {Elapsed}ms",
                            context.Request.Method, context.Request.Path, sw.ElapsedMilliseconds);
                        throw;
                    }
                }
            }
            """;

        public const string AggregatedHealthCheck = """
            using Microsoft.Extensions.Diagnostics.HealthChecks;

            namespace {{ Domain.PascalName }}Gateway.Health;

            /// <summary>
            /// Aggregates health status from all downstream services in the {{ Domain.DomainName }} domain.
            /// </summary>
            public static class AggregatedHealthCheck
            {
                public static async Task<IResult> CheckAllAsync(
                    HealthCheckService healthCheckService)
                {
                    var report = await healthCheckService.CheckHealthAsync();

                    var result = new
                    {
                        Status = report.Status.ToString(),
                        Duration = report.TotalDuration.TotalMilliseconds,
                        Services = report.Entries.Select(e => new
                        {
                            Name = e.Key,
                            Status = e.Value.Status.ToString(),
                            Duration = e.Value.Duration.TotalMilliseconds,
                            Description = e.Value.Description,
                            Tags = e.Value.Tags
                        })
                    };

                    return report.Status == HealthStatus.Healthy
                        ? Results.Ok(result)
                        : Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            }
            """;

        public const string SwaggerAggregator = """
            namespace {{ Domain.PascalName }}Gateway.Swagger;

            /// <summary>
            /// Aggregates Swagger documents from all downstream services.
            /// Used to provide a unified API documentation experience.
            /// </summary>
            public class SwaggerAggregator
            {
                private readonly IHttpClientFactory _httpClientFactory;
                private readonly ILogger<SwaggerAggregator> _logger;

                private static readonly Dictionary<string, string> ServiceEndpoints = new()
                {
            {{~ for svc in Domain.Services ~}}
                    ["{{ svc.KebabName }}"] = "http://{{ svc.KebabName }}:8080/swagger/v1/swagger.json",
            {{~ end ~}}
                };

                public SwaggerAggregator(IHttpClientFactory httpClientFactory, ILogger<SwaggerAggregator> logger)
                {
                    _httpClientFactory = httpClientFactory;
                    _logger = logger;
                }

                public async Task<Dictionary<string, object?>> GetAllSchemasAsync(CancellationToken ct = default)
                {
                    var results = new Dictionary<string, object?>();
                    var client = _httpClientFactory.CreateClient();

                    foreach (var (name, url) in ServiceEndpoints)
                    {
                        try
                        {
                            var response = await client.GetAsync(url, ct);
                            if (response.IsSuccessStatusCode)
                            {
                                var json = await response.Content.ReadAsStringAsync(ct);
                                results[name] = System.Text.Json.JsonSerializer.Deserialize<object>(json);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to fetch Swagger doc from {Service}", name);
                            results[name] = null;
                        }
                    }

                    return results;
                }
            }
            """;

        public const string AuthConfig = """
            using Microsoft.AspNetCore.Authentication.JwtBearer;

            namespace {{ Domain.PascalName }}Gateway.Auth;

            /// <summary>
            /// Configures JWT Bearer authentication for the API Gateway.
            /// </summary>
            public static class GatewayAuthConfiguration
            {
                public static IServiceCollection AddGatewayAuth(
                    this IServiceCollection services,
                    IConfiguration configuration)
                {
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            var authSection = configuration.GetSection("Authentication:JwtBearer");
                            options.Authority = authSection["Authority"];
                            options.Audience = authSection["Audience"];
                            options.RequireHttpsMetadata =
                                bool.TryParse(authSection["RequireHttpsMetadata"], out var val) && val;

                            options.TokenValidationParameters = new()
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                ClockSkew = TimeSpan.FromMinutes(1)
                            };
                        });

                    services.AddAuthorizationBuilder()
                        .AddPolicy("default", policy => policy.RequireAuthenticatedUser())
                        .AddPolicy("admin", policy => policy.RequireRole("admin", "superadmin"))
                        .AddPolicy("service", policy => policy.RequireClaim("scope", "service-to-service"));

                    return services;
                }
            }
            """;

        public const string Dockerfile = """
            # {{ Domain.PascalName }} API Gateway — Dockerfile
            # Auto-generated by MicroGen

            FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
            WORKDIR /src

            COPY src/{{ Domain.PascalName }}Gateway/*.csproj ./{{ Domain.PascalName }}Gateway/
            RUN dotnet restore {{ Domain.PascalName }}Gateway/{{ Domain.PascalName }}Gateway.csproj

            COPY src/{{ Domain.PascalName }}Gateway/ ./{{ Domain.PascalName }}Gateway/
            RUN dotnet publish {{ Domain.PascalName }}Gateway/{{ Domain.PascalName }}Gateway.csproj \
                -c Release -o /app/publish --no-restore

            FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
            WORKDIR /app

            RUN adduser --disabled-password --gecos "" appuser
            USER appuser

            COPY --from=build /app/publish .

            ENV ASPNETCORE_URLS=http://+:8080
            EXPOSE 8080

            HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
                CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1

            ENTRYPOINT ["dotnet", "{{ Domain.PascalName }}Gateway.dll"]
            """;

        public const string K8sDeployment = """
            # {{ Domain.PascalName }} API Gateway — Kubernetes Deployment
            # Auto-generated by MicroGen
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: {{ Domain.DomainName | string.downcase }}-gateway
              namespace: {{ Domain.DomainName | string.downcase }}
              labels:
                app.kubernetes.io/name: {{ Domain.DomainName | string.downcase }}-gateway
                app.kubernetes.io/component: api-gateway
            spec:
              replicas: 2
              selector:
                matchLabels:
                  app: {{ Domain.DomainName | string.downcase }}-gateway
              template:
                metadata:
                  labels:
                    app: {{ Domain.DomainName | string.downcase }}-gateway
                spec:
                  containers:
                    - name: gateway
                      image: {{ Domain.DomainName | string.downcase }}-gateway:latest
                      ports:
                        - containerPort: 8080
                          protocol: TCP
                      env:
                        - name: ASPNETCORE_ENVIRONMENT
                          value: "Production"
                        - name: ASPNETCORE_URLS
                          value: "http://+:8080"
                      resources:
                        requests:
                          cpu: "200m"
                          memory: "256Mi"
                        limits:
                          cpu: "1000m"
                          memory: "512Mi"
                      livenessProbe:
                        httpGet:
                          path: /health/live
                          port: 8080
                        initialDelaySeconds: 10
                        periodSeconds: 30
                        timeoutSeconds: 5
                        failureThreshold: 3
                      readinessProbe:
                        httpGet:
                          path: /health/ready
                          port: 8080
                        initialDelaySeconds: 5
                        periodSeconds: 10
                        timeoutSeconds: 5
                        failureThreshold: 3
                      startupProbe:
                        httpGet:
                          path: /health/live
                          port: 8080
                        initialDelaySeconds: 5
                        periodSeconds: 5
                        failureThreshold: 10
            """;

        public const string K8sService = """
            # {{ Domain.PascalName }} API Gateway — Kubernetes Service
            # Auto-generated by MicroGen
            apiVersion: v1
            kind: Service
            metadata:
              name: {{ Domain.DomainName | string.downcase }}-gateway
              namespace: {{ Domain.DomainName | string.downcase }}
              labels:
                app.kubernetes.io/name: {{ Domain.DomainName | string.downcase }}-gateway
                app.kubernetes.io/component: api-gateway
            spec:
              type: LoadBalancer
              selector:
                app: {{ Domain.DomainName | string.downcase }}-gateway
              ports:
                - name: http
                  port: 80
                  targetPort: 8080
                  protocol: TCP
                - name: https
                  port: 443
                  targetPort: 8080
                  protocol: TCP
            """;

        public const string K8sHPA = """
            # {{ Domain.PascalName }} API Gateway — Horizontal Pod Autoscaler
            # Auto-generated by MicroGen
            apiVersion: autoscaling/v2
            kind: HorizontalPodAutoscaler
            metadata:
              name: {{ Domain.DomainName | string.downcase }}-gateway-hpa
              namespace: {{ Domain.DomainName | string.downcase }}
            spec:
              scaleTargetRef:
                apiVersion: apps/v1
                kind: Deployment
                name: {{ Domain.DomainName | string.downcase }}-gateway
              minReplicas: 2
              maxReplicas: 10
              metrics:
                - type: Resource
                  resource:
                    name: cpu
                    target:
                      type: Utilization
                      averageUtilization: 70
                - type: Resource
                  resource:
                    name: memory
                    target:
                      type: Utilization
                      averageUtilization: 80
            """;
    }
}
