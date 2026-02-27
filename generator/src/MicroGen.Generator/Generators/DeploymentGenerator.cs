using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates deployment artifacts: Dockerfile, docker-compose, Kubernetes manifests, Helm charts, GitHub Actions.
/// </summary>
public sealed class DeploymentGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public DeploymentGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
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
        _logger.LogDebug("  Generating deployment artifacts for {Service}...", service.PascalName);

        // Dockerfile
        await emitter.EmitAsync(
            Path.Combine(serviceRoot, "Dockerfile"),
            _engine.Render(Templates.Dockerfile, new { Service = service, Config = _config }),
            ct);

        // .dockerignore
        await emitter.EmitAsync(
            Path.Combine(serviceRoot, ".dockerignore"),
            _engine.Render(Templates.DockerIgnore, new { Service = service }),
            ct);

        // Kubernetes manifests
        var k8sDir = Path.Combine(serviceRoot, "deploy", "k8s");

        await emitter.EmitAsync(
            Path.Combine(k8sDir, "namespace.yaml"),
            _engine.Render(Templates.K8sNamespace, new { Service = service }),
            ct);

        await emitter.EmitAsync(
            Path.Combine(k8sDir, "deployment.yaml"),
            _engine.Render(Templates.K8sDeployment, new { Service = service, Config = _config }),
            ct);

        await emitter.EmitAsync(
            Path.Combine(k8sDir, "service.yaml"),
            _engine.Render(Templates.K8sService, new { Service = service }),
            ct);

        await emitter.EmitAsync(
            Path.Combine(k8sDir, "configmap.yaml"),
            _engine.Render(Templates.K8sConfigMap, new { Service = service }),
            ct);

        await emitter.EmitAsync(
            Path.Combine(k8sDir, "hpa.yaml"),
            _engine.Render(Templates.K8sHpa, new { Service = service }),
            ct);

        // Helm chart
        var helmDir = Path.Combine(serviceRoot, "deploy", "helm", service.KebabName);

        await emitter.EmitAsync(
            Path.Combine(helmDir, "Chart.yaml"),
            _engine.Render(Templates.HelmChart, new { Service = service }),
            ct);

        await emitter.EmitAsync(
            Path.Combine(helmDir, "values.yaml"),
            _engine.Render(Templates.HelmValues, new { Service = service, Config = _config }),
            ct);

        await emitter.EmitAsync(
            Path.Combine(helmDir, "templates", "deployment.yaml"),
            _engine.Render(Templates.HelmDeploymentTemplate, new { Service = service }),
            ct);

        await emitter.EmitAsync(
            Path.Combine(helmDir, "templates", "service.yaml"),
            _engine.Render(Templates.HelmServiceTemplate, new { Service = service }),
            ct);

        // --- GitHub Actions workflows ---
        if (_config.Features.GitHubActions)
        {
            var ghDir = Path.Combine(serviceRoot, ".github", "workflows");

            // CI build workflow
            await emitter.EmitAsync(
                Path.Combine(ghDir, "ci.yaml"),
                _engine.Render(CITemplates.GitHubActionsCi, new { Service = service, Config = _config }),
                ct);

            // CodeQL security scanning
            if (_config.Features.CodeQL)
            {
                await emitter.EmitAsync(
                    Path.Combine(ghDir, "codeql.yaml"),
                    _engine.Render(CITemplates.GitHubActionsCodeQL, new { Service = service, Config = _config }),
                    ct);
            }

            // Docker build & push workflow
            await emitter.EmitAsync(
                Path.Combine(ghDir, "docker-publish.yaml"),
                _engine.Render(CITemplates.GitHubActionsDockerPublish, new { Service = service, Config = _config }),
                ct);

            // Release / deploy workflow
            await emitter.EmitAsync(
                Path.Combine(ghDir, "release.yaml"),
                _engine.Render(CITemplates.GitHubActionsRelease, new { Service = service, Config = _config }),
                ct);

            // Dependency review (PR gate)
            await emitter.EmitAsync(
                Path.Combine(ghDir, "dependency-review.yaml"),
                _engine.Render(CITemplates.GitHubActionsDependencyReview, new { Service = service, Config = _config }),
                ct);

            // Dependabot config
            if (_config.CICD.EnableDependabot)
            {
                await emitter.EmitAsync(
                    Path.Combine(serviceRoot, ".github", "dependabot.yml"),
                    _engine.Render(CITemplates.Dependabot, new { Service = service, Config = _config }),
                    ct);
            }
        }

        // --- TeamCity workflow ---
        if (_config.Features.TeamCity)
        {
            var tcDir = Path.Combine(serviceRoot, ".teamcity");

            // TeamCity Kotlin DSL settings
            await emitter.EmitAsync(
                Path.Combine(tcDir, "settings.kts"),
                _engine.Render(TeamCityTemplates.SettingsKts, new { Service = service, Config = _config }),
                ct);

            // TeamCity pom.xml for Kotlin DSL
            await emitter.EmitAsync(
                Path.Combine(tcDir, "pom.xml"),
                _engine.Render(TeamCityTemplates.PomXml, new { Service = service }),
                ct);
        }
    }

    /// <summary>
    /// Generates docker-compose.yaml for all services at the root level.
    /// </summary>
    public async Task GenerateDockerComposeAsync(
        IReadOnlyList<ServiceDescriptor> services,
        string outputRoot,
        FileEmitter emitter,
        CancellationToken ct)
    {
        _logger.LogDebug("  Generating docker-compose.yaml...");

        await emitter.EmitAsync(
            Path.Combine(outputRoot, "docker-compose.yaml"),
            _engine.Render(Templates.DockerCompose, new { Services = services, Config = _config }),
            ct);

        await emitter.EmitAsync(
            Path.Combine(outputRoot, "docker-compose.override.yaml"),
            _engine.Render(Templates.DockerComposeOverride, new { Services = services }),
            ct);
    }

    private static class Templates
    {
        public const string Dockerfile = """
            # Build stage
            FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
            WORKDIR /src

            COPY ["{{ Service.PascalName }}.Api/{{ Service.PascalName }}.Api.csproj", "{{ Service.PascalName }}.Api/"]
            COPY ["{{ Service.PascalName }}.Application/{{ Service.PascalName }}.Application.csproj", "{{ Service.PascalName }}.Application/"]
            COPY ["{{ Service.PascalName }}.Domain/{{ Service.PascalName }}.Domain.csproj", "{{ Service.PascalName }}.Domain/"]
            COPY ["{{ Service.PascalName }}.Infrastructure/{{ Service.PascalName }}.Infrastructure.csproj", "{{ Service.PascalName }}.Infrastructure/"]
            COPY ["{{ Service.PascalName }}.Jobs/{{ Service.PascalName }}.Jobs.csproj", "{{ Service.PascalName }}.Jobs/"]
            RUN dotnet restore "{{ Service.PascalName }}.Api/{{ Service.PascalName }}.Api.csproj"

            COPY . .
            RUN dotnet build "{{ Service.PascalName }}.Api/{{ Service.PascalName }}.Api.csproj" -c Release -o /app/build

            # Publish stage
            FROM build AS publish
            RUN dotnet publish "{{ Service.PascalName }}.Api/{{ Service.PascalName }}.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

            # Runtime stage
            FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
            WORKDIR /app

            RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
            USER appuser

            COPY --from=publish /app/publish .

            ENV ASPNETCORE_URLS=http://+:8080
            EXPOSE 8080

            HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
                CMD curl -f http://localhost:8080/health/live || exit 1

            ENTRYPOINT ["dotnet", "{{ Service.PascalName }}.Api.dll"]
            """;

        public const string DockerIgnore = """
            **/bin/
            **/obj/
            **/.vs/
            **/.vscode/
            **/node_modules/
            **/*.user
            **/*.suo
            .git/
            .gitignore
            README.md
            docker-compose*.yaml
            deploy/
            """;

        public const string K8sNamespace = """
            apiVersion: v1
            kind: Namespace
            metadata:
              name: {{ Service.KebabName }}
              labels:
                app.kubernetes.io/name: {{ Service.KebabName }}
                app.kubernetes.io/part-of: thewatch
            """;

        public const string K8sDeployment = """
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: {{ Service.KebabName }}
              namespace: {{ Service.KebabName }}
              labels:
                app: {{ Service.KebabName }}
            spec:
              replicas: 2
              selector:
                matchLabels:
                  app: {{ Service.KebabName }}
              template:
                metadata:
                  labels:
                    app: {{ Service.KebabName }}
                spec:
                  containers:
                    - name: {{ Service.KebabName }}
                      image: thewatch/{{ Service.KebabName }}:latest
                      ports:
                        - containerPort: 8080
                          protocol: TCP
                      env:
                        - name: ASPNETCORE_ENVIRONMENT
                          value: "Production"
                        - name: ConnectionStrings__DefaultConnection
                          valueFrom:
                            secretKeyRef:
                              name: {{ Service.KebabName }}-secrets
                              key: db-connection-string
                        - name: ConnectionStrings__Redis
                          valueFrom:
                            secretKeyRef:
                              name: {{ Service.KebabName }}-secrets
                              key: redis-connection-string
            {{~ if Config.Features.Kafka ~}}
                        - name: Kafka__BootstrapServers
                          value: "{{ Service.DomainName | string.downcase }}-kafka-bootstrap:9092"
                        - name: Kafka__SchemaRegistryUrl
                          value: "http://{{ Service.DomainName | string.downcase }}-schema-registry:8081"
                        - name: Kafka__ConsumerGroup
                          value: "{{ Service.KebabName }}-consumer-group"
            {{~ end ~}}
            {{~ if Config.Features.Dubbo ~}}
                        - name: Dubbo__RegistryAddress
                          value: "nacos://{{ Service.DomainName | string.downcase }}-nacos:8848"
                        - name: Dubbo__Protocol
                          value: "tri"
                        - name: Dubbo__Port
                          value: "50051"
            {{~ end ~}}
                      resources:
                        requests:
                          cpu: "100m"
                          memory: "256Mi"
                        limits:
                          cpu: "500m"
                          memory: "512Mi"
                      livenessProbe:
                        httpGet:
                          path: /health/live
                          port: 8080
                        initialDelaySeconds: 15
                        periodSeconds: 30
                      readinessProbe:
                        httpGet:
                          path: /health/ready
                          port: 8080
                        initialDelaySeconds: 5
                        periodSeconds: 10
            """;

        public const string K8sService = """
            apiVersion: v1
            kind: Service
            metadata:
              name: {{ Service.KebabName }}
              namespace: {{ Service.KebabName }}
            spec:
              type: ClusterIP
              selector:
                app: {{ Service.KebabName }}
              ports:
                - port: 80
                  targetPort: 8080
                  protocol: TCP
            """;

        public const string K8sConfigMap = """
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: {{ Service.KebabName }}-config
              namespace: {{ Service.KebabName }}
            data:
              ServiceName: "{{ Service.PascalName }}"
              ServiceVersion: "{{ Service.Version }}"
              ASPNETCORE_ENVIRONMENT: "Production"
            """;

        public const string K8sHpa = """
            apiVersion: autoscaling/v2
            kind: HorizontalPodAutoscaler
            metadata:
              name: {{ Service.KebabName }}
              namespace: {{ Service.KebabName }}
            spec:
              scaleTargetRef:
                apiVersion: apps/v1
                kind: Deployment
                name: {{ Service.KebabName }}
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

        public const string HelmChart = """
            apiVersion: v2
            name: {{ Service.KebabName }}
            description: Helm chart for {{ Service.Title }}
            type: application
            version: 0.1.0
            appVersion: "{{ Service.Version }}"
            """;

        public const string HelmValues = """
            replicaCount: 2

            image:
              repository: thewatch/{{ Service.KebabName }}
              pullPolicy: IfNotPresent
              tag: "latest"

            service:
              type: ClusterIP
              port: 80

            resources:
              requests:
                cpu: 100m
                memory: 256Mi
              limits:
                cpu: 500m
                memory: 512Mi

            autoscaling:
              enabled: true
              minReplicas: 2
              maxReplicas: 10
              targetCPUUtilizationPercentage: 70

            env:
              ASPNETCORE_ENVIRONMENT: Production
            {{~ if Config.Features.Kafka ~}}

            kafka:
              bootstrapServers: "{{ Service.DomainName | string.downcase }}-kafka-bootstrap:9092"
              schemaRegistryUrl: "http://{{ Service.DomainName | string.downcase }}-schema-registry:8081"
              consumerGroup: "{{ Service.KebabName }}-consumer-group"
            {{~ end ~}}
            {{~ if Config.Features.Dubbo ~}}

            dubbo:
              registryAddress: "nacos://{{ Service.DomainName | string.downcase }}-nacos:8848"
              protocol: tri
              port: 50051
            {{~ end ~}}
            """;

        public const string HelmDeploymentTemplate = """
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: {{ "{{" }} include "{{ Service.KebabName }}.fullname" . {{ "}}" }}
              labels:
                {{ "{{" }}- include "{{ Service.KebabName }}.labels" . | nindent 4 {{ "}}" }}
            spec:
              replicas: {{ "{{" }} .Values.replicaCount {{ "}}" }}
              selector:
                matchLabels:
                  {{ "{{" }}- include "{{ Service.KebabName }}.selectorLabels" . | nindent 6 {{ "}}" }}
              template:
                metadata:
                  labels:
                    {{ "{{" }}- include "{{ Service.KebabName }}.selectorLabels" . | nindent 8 {{ "}}" }}
                spec:
                  containers:
                    - name: {{ "{{" }} .Chart.Name {{ "}}" }}
                      image: "{{ "{{" }} .Values.image.repository {{ "}}" }}:{{ "{{" }} .Values.image.tag {{ "}}" }}"
                      imagePullPolicy: {{ "{{" }} .Values.image.pullPolicy {{ "}}" }}
                      ports:
                        - containerPort: 8080
                      resources:
                        {{ "{{" }}- toYaml .Values.resources | nindent 12 {{ "}}" }}
            """;

        public const string HelmServiceTemplate = """
            apiVersion: v1
            kind: Service
            metadata:
              name: {{ "{{" }} include "{{ Service.KebabName }}.fullname" . {{ "}}" }}
              labels:
                {{ "{{" }}- include "{{ Service.KebabName }}.labels" . | nindent 4 {{ "}}" }}
            spec:
              type: {{ "{{" }} .Values.service.type {{ "}}" }}
              ports:
                - port: {{ "{{" }} .Values.service.port {{ "}}" }}
                  targetPort: 8080
                  protocol: TCP
              selector:
                {{ "{{" }}- include "{{ Service.KebabName }}.selectorLabels" . | nindent 4 {{ "}}" }}
            """;

        public const string DockerCompose = """
            version: "3.9"

            services:
            {{~ for svc in Services ~}}
              {{ svc.KebabName }}:
                build:
                  context: ./{{ svc.KebabName }}
                  dockerfile: Dockerfile
                ports:
                  - "${{'{{'}}} {{ svc.PascalName | string.upcase }}_PORT:-{{ 5000 + for.index }}{{'}}'}}:8080"
                environment:
                  - ASPNETCORE_ENVIRONMENT=Development
                  - ConnectionStrings__DefaultConnection=Host=postgres;Database={{ svc.KebabName }};Username=app;Password=secret
                  - ConnectionStrings__Redis=redis:6379
                depends_on:
                  - postgres
                  - redis
                restart: unless-stopped

            {{~ end ~}}
              postgres:
                image: postgres:18-alpine
                environment:
                  POSTGRES_USER: app
                  POSTGRES_PASSWORD: secret
                ports:
                  - "5432:5432"
                volumes:
                  - pgdata:/var/lib/postgresql/data

              redis:
                image: redis:7-alpine
                ports:
                  - "6379:6379"
                volumes:
                  - redisdata:/data
            {{~ if Config.Features.Kafka ~}}

              kafka:
                image: bitnami/kafka:{{ Config.Apache.KafkaVersion }}
                ports:
                  - "9092:9092"
                  - "9094:9094"
                environment:
                  KAFKA_CFG_NODE_ID: "1"
                  KAFKA_CFG_PROCESS_ROLES: "broker,controller"
                  KAFKA_CFG_CONTROLLER_QUORUM_VOTERS: "1@kafka:9093"
                  KAFKA_CFG_LISTENERS: "PLAINTEXT://:9092,CONTROLLER://:9093,EXTERNAL://:9094"
                  KAFKA_CFG_ADVERTISED_LISTENERS: "PLAINTEXT://kafka:9092,EXTERNAL://localhost:9094"
                  KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP: "CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT,EXTERNAL:PLAINTEXT"
                  KAFKA_CFG_CONTROLLER_LISTENER_NAMES: "CONTROLLER"
                  KAFKA_KRAFT_CLUSTER_ID: "local-dev-kafka"
                volumes:
                  - kafkadata:/bitnami/kafka
                healthcheck:
                  test: ["CMD-SHELL", "kafka-broker-api-versions.sh --bootstrap-server localhost:9092"]
                  interval: 10s
                  timeout: 10s
                  retries: 5

              schema-registry:
                image: confluentinc/cp-schema-registry:{{ Config.Apache.SchemaRegistryVersion }}
                ports:
                  - "8081:8081"
                environment:
                  SCHEMA_REGISTRY_HOST_NAME: "schema-registry"
                  SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS: "kafka:9092"
                depends_on:
                  kafka:
                    condition: service_healthy
            {{~ end ~}}
            {{~ if Config.Features.Dubbo ~}}

              nacos:
                image: nacos/nacos-server:v{{ Config.Apache.NacosVersion }}
                ports:
                  - "8848:8848"
                  - "9848:9848"
                environment:
                  MODE: standalone
                  NACOS_AUTH_ENABLE: "false"
                  JVM_XMS: "256m"
                  JVM_XMX: "512m"
                volumes:
                  - nacosdata:/home/nacos/data
                healthcheck:
                  test: ["CMD", "curl", "-sf", "http://localhost:8848/nacos/v1/console/health/liveness"]
                  interval: 10s
                  timeout: 5s
                  retries: 5
            {{~ end ~}}

            volumes:
              pgdata:
              redisdata:
            {{~ if Config.Features.Kafka ~}}
              kafkadata:
            {{~ end ~}}
            {{~ if Config.Features.Dubbo ~}}
              nacosdata:
            {{~ end ~}}
            """;

        public const string DockerComposeOverride = """
            version: "3.9"

            # Development overrides
            services:
            {{~ for svc in Services ~}}
              {{ svc.KebabName }}:
                environment:
                  - ASPNETCORE_ENVIRONMENT=Development
                  - Serilog__MinimumLevel__Default=Debug
            {{~ end ~}}
            """;
    }

    /// <summary>
    /// GitHub Actions CI/CD workflow templates.
    /// </summary>
    internal static class CITemplates
    {
        public const string GitHubActionsCi = """
            name: CI — {{ Service.PascalName }}

            on:
              push:
                branches: [{{ Config.CICD.DefaultBranch }}]
                paths:
                  - '{{ Service.KebabName }}/**'
                  - '.github/workflows/ci.yaml'
              pull_request:
                branches: [{{ Config.CICD.DefaultBranch }}]
                paths:
                  - '{{ Service.KebabName }}/**'
                  - '.github/workflows/ci.yaml'

            permissions:
              contents: read
              checks: write
              pull-requests: write

            env:
              DOTNET_VERSION: '10.0.x'
              DOTNET_NOLOGO: true
              DOTNET_CLI_TELEMETRY_OPTOUT: true
              NUGET_PACKAGES: ${{ "{{" }} github.workspace {{ "}}" }}/.nuget/packages

            jobs:
              build:
                name: Build & Test
                runs-on: ubuntu-latest
                strategy:
                  matrix:
                    os: [ubuntu-latest, windows-latest]
                steps:
                  - name: Checkout
                    uses: actions/checkout@v4
                    with:
                      fetch-depth: 0

                  - name: Setup .NET
                    uses: actions/setup-dotnet@v4
                    with:
                      dotnet-version: ${{ "{{" }} env.DOTNET_VERSION {{ "}}" }}

                  - name: Cache NuGet packages
                    uses: actions/cache@v4
                    with:
                      path: ${{ "{{" }} env.NUGET_PACKAGES {{ "}}" }}
                      key: ${{ "{{" }} runner.os {{ "}}" }}-nuget-${{ "{{" }} hashFiles('**/packages.lock.json', '**/*.csproj') {{ "}}" }}
                      restore-keys: ${{ "{{" }} runner.os {{ "}}" }}-nuget-

                  - name: Restore
                    run: dotnet restore {{ Service.KebabName }}/

                  - name: Build
                    run: dotnet build {{ Service.KebabName }}/ --no-restore -c Release -warnaserror

                  - name: Test
                    run: >
                      dotnet test {{ Service.KebabName }}/
                      --no-build -c Release
                      --logger "trx;LogFileName=test-results.trx"
                      --collect:"XPlat Code Coverage"
                      --results-directory ./test-results

                  - name: Upload Test Results
                    if: always()
                    uses: actions/upload-artifact@v4
                    with:
                      name: test-results-${{ "{{" }} matrix.os {{ "}}" }}
                      path: ./test-results/

                  - name: Publish Test Results
                    if: always() && matrix.os == 'ubuntu-latest'
                    uses: dorny/test-reporter@v1
                    with:
                      name: .NET Tests — {{ Service.PascalName }}
                      path: ./test-results/**/*.trx
                      reporter: dotnet-trx

                  - name: Upload Code Coverage
                    if: matrix.os == 'ubuntu-latest'
                    uses: codecov/codecov-action@v4
                    with:
                      directory: ./test-results
                      fail_ci_if_error: false
                      token: ${{ "{{" }} secrets.CODECOV_TOKEN {{ "}}" }}

              format-check:
                name: Format & Lint
                runs-on: ubuntu-latest
                steps:
                  - uses: actions/checkout@v4

                  - name: Setup .NET
                    uses: actions/setup-dotnet@v4
                    with:
                      dotnet-version: ${{ "{{" }} env.DOTNET_VERSION {{ "}}" }}

                  - name: Restore
                    run: dotnet restore {{ Service.KebabName }}/

                  - name: Check Formatting
                    run: dotnet format {{ Service.KebabName }}/ --verify-no-changes --verbosity diagnostic

              security-scan:
                name: Security Scan
                runs-on: ubuntu-latest
                steps:
                  - uses: actions/checkout@v4

                  - name: Setup .NET
                    uses: actions/setup-dotnet@v4
                    with:
                      dotnet-version: ${{ "{{" }} env.DOTNET_VERSION {{ "}}" }}

                  - name: Restore
                    run: dotnet restore {{ Service.KebabName }}/

                  - name: Run Security Audit
                    run: dotnet list {{ Service.KebabName }}/ package --vulnerable --include-transitive 2>&1 | tee audit.log

                  - name: Check for Vulnerabilities
                    run: |
                      if grep -qi "has the following vulnerable packages" audit.log; then
                        echo "::error::Vulnerable packages detected!"
                        exit 1
                      fi
            """;

        public const string GitHubActionsCodeQL = """
            name: CodeQL — {{ Service.PascalName }}

            on:
              push:
                branches: [{{ Config.CICD.DefaultBranch }}]
                paths:
                  - '{{ Service.KebabName }}/**'
              pull_request:
                branches: [{{ Config.CICD.DefaultBranch }}]
                paths:
                  - '{{ Service.KebabName }}/**'
              schedule:
                - cron: '{{ Config.CICD.CodeQLSchedule }}'

            permissions:
              actions: read
              contents: read
              security-events: write

            jobs:
              analyze:
                name: Analyze
                runs-on: ubuntu-latest
                timeout-minutes: 360
                strategy:
                  fail-fast: false
                  matrix:
                    language:
            {{~ for lang in Config.CICD.CodeQLLanguages ~}}
                      - '{{ lang }}'
            {{~ end ~}}

                steps:
                  - name: Checkout
                    uses: actions/checkout@v4

                  - name: Setup .NET
                    uses: actions/setup-dotnet@v4
                    with:
                      dotnet-version: '10.0.x'

                  - name: Initialize CodeQL
                    uses: github/codeql-action/init@v3
                    with:
                      languages: ${{ "{{" }} matrix.language {{ "}}" }}
                      queries: security-extended,security-and-quality
                      config: |
                        paths:
                          - '{{ Service.KebabName }}'
                        paths-ignore:
                          - '{{ Service.KebabName }}/**/obj'
                          - '{{ Service.KebabName }}/**/bin'
                          - '{{ Service.KebabName }}/**/*.Tests'

                  - name: Build for Analysis
                    run: |
                      dotnet restore {{ Service.KebabName }}/
                      dotnet build {{ Service.KebabName }}/ --no-restore -c Release

                  - name: Perform CodeQL Analysis
                    uses: github/codeql-action/analyze@v3
                    with:
                      category: "/language:${{ "{{" }} matrix.language {{ "}}" }}/service:{{ Service.KebabName }}"
                      upload: true

                  - name: Upload SARIF
                    if: always()
                    uses: github/codeql-action/upload-sarif@v3
                    with:
                      sarif_file: /home/runner/work/_temp/codeql_databases
                      category: "{{ Service.PascalName }}-codeql"
            """;

        public const string GitHubActionsDockerPublish = """
            name: Docker Publish — {{ Service.PascalName }}

            on:
              push:
                branches: [{{ Config.CICD.DefaultBranch }}]
                tags: ['v*.*.*']
                paths:
                  - '{{ Service.KebabName }}/**'
                  - '.github/workflows/docker-publish.yaml'
              workflow_dispatch:
                inputs:
                  tag:
                    description: 'Image tag override'
                    required: false
                    default: ''

            permissions:
              contents: read
              packages: write
              id-token: write
              attestations: write

            env:
              REGISTRY: {{ Config.CICD.ContainerRegistry }}
              IMAGE_NAME: {{ Config.CICD.RegistryOwner }}/{{ Service.KebabName }}

            jobs:
              build-and-push:
                name: Build & Push
                runs-on: ubuntu-latest
                outputs:
                  image-digest: ${{ "{{" }} steps.build.outputs.digest {{ "}}" }}
                  image-tag: ${{ "{{" }} steps.meta.outputs.version {{ "}}" }}
                steps:
                  - name: Checkout
                    uses: actions/checkout@v4

                  - name: Setup Docker Buildx
                    uses: docker/setup-buildx-action@v3

                  - name: Log in to Container Registry
                    uses: docker/login-action@v3
                    with:
                      registry: ${{ "{{" }} env.REGISTRY {{ "}}" }}
                      username: ${{ "{{" }} github.actor {{ "}}" }}
                      password: ${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}

                  - name: Extract Metadata
                    id: meta
                    uses: docker/metadata-action@v5
                    with:
                      images: ${{ "{{" }} env.REGISTRY {{ "}}" }}/${{ "{{" }} env.IMAGE_NAME {{ "}}" }}
                      tags: |
                        type=ref,event=branch
                        type=ref,event=pr
                        type=semver,pattern={{ "{{" }}version{{ "}}" }}
                        type=semver,pattern={{ "{{" }}major{{ "}}" }}.{{ "{{" }}minor{{ "}}" }}
                        type=sha,prefix=
                        type=raw,value=latest,enable={{ "{{" }}is_default_branch{{ "}}" }}

                  - name: Build and Push
                    id: build
                    uses: docker/build-push-action@v6
                    with:
                      context: ./{{ Service.KebabName }}
                      push: ${{ "{{" }} github.event_name != 'pull_request' {{ "}}" }}
                      tags: ${{ "{{" }} steps.meta.outputs.tags {{ "}}" }}
                      labels: ${{ "{{" }} steps.meta.outputs.labels {{ "}}" }}
                      cache-from: type=gha
                      cache-to: type=gha,mode=max
                      platforms: linux/amd64,linux/arm64
                      build-args: |
                        BUILD_VERSION=${{ "{{" }} steps.meta.outputs.version {{ "}}" }}
                        BUILD_SHA=${{ "{{" }} github.sha {{ "}}" }}

                  - name: Generate Artifact Attestation
                    if: github.event_name != 'pull_request'
                    uses: actions/attest-build-provenance@v2
                    with:
                      subject-name: ${{ "{{" }} env.REGISTRY {{ "}}" }}/${{ "{{" }} env.IMAGE_NAME {{ "}}" }}
                      subject-digest: ${{ "{{" }} steps.build.outputs.digest {{ "}}" }}
                      push-to-registry: true

              scan-image:
                name: Scan Image
                needs: build-and-push
                runs-on: ubuntu-latest
                if: github.event_name != 'pull_request'
                steps:
                  - name: Run Trivy Vulnerability Scanner
                    uses: aquasecurity/trivy-action@master
                    with:
                      image-ref: ${{ "{{" }} env.REGISTRY {{ "}}" }}/${{ "{{" }} env.IMAGE_NAME {{ "}}" }}:${{ "{{" }} needs.build-and-push.outputs.image-tag {{ "}}" }}
                      format: 'sarif'
                      output: 'trivy-results.sarif'
                      severity: 'CRITICAL,HIGH'

                  - name: Upload Trivy SARIF
                    uses: github/codeql-action/upload-sarif@v3
                    with:
                      sarif_file: 'trivy-results.sarif'
                      category: '{{ Service.PascalName }}-container-scan'
            """;

        public const string GitHubActionsRelease = """
            name: Release — {{ Service.PascalName }}

            on:
              push:
                tags: ['v*.*.*']
              workflow_dispatch:
                inputs:
                  version:
                    description: 'Release version (e.g., 1.0.0)'
                    required: true
                  prerelease:
                    description: 'Is pre-release?'
                    type: boolean
                    default: false

            permissions:
              contents: write
              packages: write
              id-token: write

            env:
              DOTNET_VERSION: '10.0.x'

            jobs:
              release:
                name: Create Release
                runs-on: ubuntu-latest
                steps:
                  - name: Checkout
                    uses: actions/checkout@v4
                    with:
                      fetch-depth: 0

                  - name: Setup .NET
                    uses: actions/setup-dotnet@v4
                    with:
                      dotnet-version: ${{ "{{" }} env.DOTNET_VERSION {{ "}}" }}

                  - name: Determine Version
                    id: version
                    run: |
                      if [ "${{ "{{" }} github.event_name {{ "}}" }}" = "workflow_dispatch" ]; then
                        echo "version=${{ "{{" }} github.event.inputs.version {{ "}}" }}" >> $GITHUB_OUTPUT
                      else
                        echo "version=${GITHUB_REF_NAME#v}" >> $GITHUB_OUTPUT
                      fi

                  - name: Build Release
                    run: >
                      dotnet publish {{ Service.KebabName }}/src/{{ Service.PascalName }}.Api/
                      -c Release
                      -o ./publish
                      /p:Version=${{ "{{" }} steps.version.outputs.version {{ "}}" }}
                      /p:UseAppHost=false

                  - name: Run Tests
                    run: >
                      dotnet test {{ Service.KebabName }}/
                      -c Release
                      --logger "trx"
                      --collect:"XPlat Code Coverage"

                  - name: Package Release
                    run: |
                      cd publish
                      tar -czf ../{{ Service.KebabName }}-${{ "{{" }} steps.version.outputs.version {{ "}}" }}.tar.gz .

                  - name: Generate Changelog
                    id: changelog
                    uses: mikepenz/release-changelog-builder-action@v4
                    with:
                      configurationJson: |
                        {
                          "categories": [
                            {"title": "Features", "labels": ["feature", "enhancement"]},
                            {"title": "Bug Fixes", "labels": ["bug", "fix"]},
                            {"title": "Security", "labels": ["security"]},
                            {"title": "Documentation", "labels": ["documentation"]}
                          ]
                        }
                    env:
                      GITHUB_TOKEN: ${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}

                  - name: Create GitHub Release
                    uses: softprops/action-gh-release@v2
                    with:
                      tag_name: v${{ "{{" }} steps.version.outputs.version {{ "}}" }}
                      name: "{{ Service.PascalName }} v${{ "{{" }} steps.version.outputs.version {{ "}}" }}"
                      body: ${{ "{{" }} steps.changelog.outputs.changelog {{ "}}" }}
                      prerelease: ${{ "{{" }} github.event.inputs.prerelease || false {{ "}}" }}
                      files: |
                        {{ Service.KebabName }}-${{ "{{" }} steps.version.outputs.version {{ "}}" }}.tar.gz
                    env:
                      GITHUB_TOKEN: ${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}

              deploy-staging:
                name: Deploy to Staging
                needs: release
                runs-on: ubuntu-latest
                environment: staging
                steps:
                  - name: Checkout
                    uses: actions/checkout@v4

                  - name: Deploy to K8s Staging
                    run: |
                      echo "Deploying {{ Service.PascalName }} v${{ "{{" }} needs.release.outputs.version {{ "}}" }} to staging"
                      # kubectl set image deployment/{{ Service.KebabName }} \
                      #   {{ Service.KebabName }}={{ Config.CICD.ContainerRegistry }}/{{ Config.CICD.RegistryOwner }}/{{ Service.KebabName }}:${{ "{{" }} github.sha {{ "}}" }} \
                      #   -n {{ Service.KebabName }}-staging
            """;

        public const string GitHubActionsDependencyReview = """
            name: Dependency Review — {{ Service.PascalName }}

            on:
              pull_request:
                branches: [{{ Config.CICD.DefaultBranch }}]
                paths:
                  - '{{ Service.KebabName }}/**/*.csproj'
                  - '{{ Service.KebabName }}/**/packages.lock.json'
                  - '{{ Service.KebabName }}/**/Directory.Packages.props'

            permissions:
              contents: read
              pull-requests: write

            jobs:
              dependency-review:
                name: Review Dependencies
                runs-on: ubuntu-latest
                steps:
                  - name: Checkout
                    uses: actions/checkout@v4

                  - name: Dependency Review
                    uses: actions/dependency-review-action@v4
                    with:
                      fail-on-severity: high
                      deny-licenses: GPL-3.0, AGPL-3.0
                      comment-summary-in-pr: always
                      warn-only: false

              license-check:
                name: License Compliance
                runs-on: ubuntu-latest
                steps:
                  - uses: actions/checkout@v4

                  - name: Setup .NET
                    uses: actions/setup-dotnet@v4
                    with:
                      dotnet-version: '10.0.x'

                  - name: Install License Tool
                    run: dotnet tool install --global dotnet-project-licenses

                  - name: Check Licenses
                    run: |
                      dotnet restore {{ Service.KebabName }}/
                      dotnet-project-licenses -i {{ Service.KebabName }}/ --output-format json > licenses.json
                      echo "License report generated"

                  - name: Upload License Report
                    uses: actions/upload-artifact@v4
                    with:
                      name: license-report
                      path: licenses.json
            """;

        public const string Dependabot = """
            version: 2
            updates:
              - package-ecosystem: "nuget"
                directory: "/{{ Service.KebabName }}"
                schedule:
                  interval: "weekly"
                  day: "monday"
                  time: "06:00"
                  timezone: "America/New_York"
                open-pull-requests-limit: 10
                reviewers:
                  - "{{ Config.CICD.RegistryOwner }}"
                labels:
                  - "dependencies"
                  - "nuget"
                ignore:
                  - dependency-name: "Microsoft.*"
                    update-types: ["version-update:semver-patch"]
                groups:
                  microsoft:
                    patterns:
                      - "Microsoft.*"
                      - "System.*"
                  testing:
                    patterns:
                      - "xunit*"
                      - "Moq"
                      - "FluentAssertions"
                      - "Bogus"

              - package-ecosystem: "docker"
                directory: "/{{ Service.KebabName }}"
                schedule:
                  interval: "weekly"
                labels:
                  - "dependencies"
                  - "docker"

              - package-ecosystem: "github-actions"
                directory: "/"
                schedule:
                  interval: "weekly"
                labels:
                  - "dependencies"
                  - "github-actions"
            """;
    }

    /// <summary>
    /// TeamCity Kotlin DSL and configuration templates.
    /// </summary>
    internal static class TeamCityTemplates
    {
      public const string SettingsKts = """"
            import jetbrains.buildServer.configs.kotlin.*
            import jetbrains.buildServer.configs.kotlin.buildFeatures.*
            import jetbrains.buildServer.configs.kotlin.buildSteps.*
            import jetbrains.buildServer.configs.kotlin.triggers.*
            import jetbrains.buildServer.configs.kotlin.vcs.*

            /*
             * TeamCity Kotlin DSL configuration for {{ Service.PascalName }}
             * Generated by MicroGen
             */

            version = "2024.12"

            project {
                description = "{{ Service.Title }} — CI/CD Pipeline"

                vcsRoot(GitVcs)

                buildType(Build)
                buildType(CodeQLScan)
                buildType(DockerPublish)
                buildType(Release)

                params {
                    param("env.DOTNET_VERSION", "10.0")
                    param("env.DOTNET_NOLOGO", "true")
                    param("env.DOTNET_CLI_TELEMETRY_OPTOUT", "true")
                    param("service.name", "{{ Service.KebabName }}")
                    param("service.pascal", "{{ Service.PascalName }}")
                    param("registry", "{{ Config.CICD.ContainerRegistry }}")
                    param("registry.owner", "{{ Config.CICD.RegistryOwner }}")
                }
            }

            object GitVcs : GitVcsRoot({
                name = "{{ Service.PascalName }} VCS Root"
                url = "https://github.com/{{ Config.CICD.RegistryOwner }}/{{ Service.KebabName }}.git"
                branch = "refs/heads/{{ Config.CICD.DefaultBranch }}"
                branchSpec = "+:refs/heads/*"
                authMethod = token {
                    userName = "oauth2"
                    tokenId = "tc_token_id:CID_github_token"
                }
            })

            // ─── Build & Test ──────────────────────────────────────────────

            object Build : BuildType({
                name = "Build & Test"
                description = "Restore, build, test, and publish {{ Service.PascalName }}"

                vcs {
                    root(GitVcs)
                    cleanCheckout = true
                }

                triggers {
                    vcs {
                        branchFilter = "+:*"
                        triggerRules = """
                            +:{{ Service.KebabName }}/**
                            +:.teamcity/**
                        """.trimIndent()
                    }
                }

                steps {
                    dotnetRestore {
                        name = "Restore NuGet Packages"
                        projects = "%service.name%/"
                    }

                    dotnetBuild {
                        name = "Build (Release)"
                        projects = "%service.name%/"
                        configuration = "Release"
                        args = "--no-restore -warnaserror"
                    }

                    dotnetTest {
                        name = "Run Tests"
                        projects = "%service.name%/"
                        configuration = "Release"
                        args = """
                            --no-build
                            --logger "trx;LogFileName=test-results.trx"
                            --collect:"XPlat Code Coverage"
                            --results-directory ./test-results
                        """.trimIndent()
                    }

                    dotnetPublish {
                        name = "Publish Application"
                        projects = "%service.name%/src/%service.pascal%.Api/"
                        configuration = "Release"
                        outputDir = "./publish"
                        args = "/p:UseAppHost=false"
                    }

                    script {
                        name = "Check Formatting"
                        scriptContent = "dotnet format %service.name%/ --verify-no-changes --verbosity diagnostic"
                    }

                    script {
                        name = "Security Audit"
                        scriptContent = """
                            dotnet list %service.name%/ package --vulnerable --include-transitive 2>&1 | tee audit.log
                            if grep -qi "has the following vulnerable packages" audit.log; then
                                echo "##teamcity[buildProblem description='Vulnerable packages detected!']"
                            fi
                        """.trimIndent()
                    }
                }

                features {
                    xmlReport {
                        reportType = XmlReportPlugin.XmlReportType.TRX
                        rules = "./test-results/**/*.trx"
                    }

                    commitStatusPublisher {
                        publisher = github {
                            authType = personalToken {
                                token = "credentialsJSON:github-token"
                            }
                        }
                    }

                    perfmon { }
                }

                artifactRules = """
                    ./publish/** => %service.name%-%build.number%.zip
                    ./test-results/** => test-results-%build.number%.zip
                """.trimIndent()
            })

            // ─── CodeQL / Security Scanning ────────────────────────────────

            object CodeQLScan : BuildType({
                name = "CodeQL Security Scan"
                description = "Run CodeQL analysis on {{ Service.PascalName }} for vulnerability detection"

                vcs {
                    root(GitVcs)
                    cleanCheckout = true
                }

                triggers {
                    vcs {
                        branchFilter = "+:refs/heads/{{ Config.CICD.DefaultBranch }}"
                        triggerRules = "+:{{ Service.KebabName }}/**"
                    }
                    schedule {
                        schedulingPolicy = weekly {
                            dayOfWeek = ScheduleTrigger.DAY.Monday
                            hour = 6
                        }
                        branchFilter = "+:refs/heads/{{ Config.CICD.DefaultBranch }}"
                        triggerBuild = always()
                    }
                }

                steps {
                    script {
                        name = "Initialize CodeQL"
                        scriptContent = """
                            # Download and initialize CodeQL CLI
                            CODEQL_VERSION="2.19.3"
                            wget -q "https://github.com/github/codeql-action/releases/download/codeql-bundle-v${'$'}CODEQL_VERSION/codeql-bundle-linux64.tar.gz"
                            tar -xzf codeql-bundle-linux64.tar.gz
                            export PATH="${'$'}PWD/codeql:${'$'}PATH"

                            # Create CodeQL database from .NET build
                            codeql database create codeql-db \
                                --language=csharp \
                                --source-root=%service.name% \
                                --command="dotnet build %service.name%/ -c Release"

                            echo "CodeQL database created successfully"
                        """.trimIndent()
                    }

                    script {
                        name = "Run CodeQL Analysis"
                        scriptContent = """
                            export PATH="${'$'}PWD/codeql:${'$'}PATH"

                            # Run security-extended and quality queries
                            codeql database analyze codeql-db \
                                --format=sarif-latest \
                                --output=codeql-results.sarif \
                                csharp-security-extended csharp-security-and-quality

                            echo "CodeQL analysis complete — SARIF report generated"
                        """.trimIndent()
                    }

                    script {
                        name = "Parse SARIF Results"
                        scriptContent = """
                            # Parse the SARIF file for critical findings
                            CRITICAL=${'$'}(python3 -c "
                            import json, sys
                            with open('codeql-results.sarif') as f:
                                sarif = json.load(f)
                            results = sarif.get('runs', [{}])[0].get('results', [])
                            critical = [r for r in results if r.get('level') in ('error', 'warning')]
                            print(len(critical))
                            for r in critical[:10]:
                                loc = r.get('locations', [{}])[0].get('physicalLocation', {})
                                uri = loc.get('artifactLocation', {}).get('uri', '?')
                                line = loc.get('region', {}).get('startLine', '?')
                                print(f'  {r[\"level\"].upper()}: {r[\"message\"][\"text\"][:100]} @ {uri}:{line}')
                            ")

                            echo "Found ${'$'}CRITICAL CodeQL findings"
                            if [ "${'$'}CRITICAL" -gt "0" ]; then
                                echo "##teamcity[buildProblem description='CodeQL found ${'$'}CRITICAL security findings']"
                            fi
                        """.trimIndent()
                    }
                }

                artifactRules = """
                    codeql-results.sarif => codeql-reports/
                """.trimIndent()

                features {
                    commitStatusPublisher {
                        publisher = github {
                            authType = personalToken {
                                token = "credentialsJSON:github-token"
                            }
                        }
                    }
                }
            })

            // ─── Docker Build & Publish ────────────────────────────────────

            object DockerPublish : BuildType({
                name = "Docker Build & Publish"
                description = "Build and push {{ Service.PascalName }} container image"

                vcs {
                    root(GitVcs)
                    cleanCheckout = true
                }

                triggers {
                    finishBuildTrigger {
                        buildType = "${Build.id}"
                        successfulOnly = true
                        branchFilter = "+:refs/heads/{{ Config.CICD.DefaultBranch }}"
                    }
                }

                steps {
                    dockerCommand {
                        name = "Build Docker Image"
                        commandType = build {
                            source = file {
                                path = "%service.name%/Dockerfile"
                            }
                            contextDir = "%service.name%"
                            namesAndTags = "%registry%/%registry.owner%/%service.name%:%build.number%"
                            commandArgs = """
                                --build-arg BUILD_VERSION=%build.number%
                                --build-arg BUILD_SHA=%build.vcs.number%
                                --platform linux/amd64
                            """.trimIndent()
                        }
                    }

                    dockerCommand {
                        name = "Tag Latest"
                        commandType = other {
                            subCommand = "tag"
                            commandArgs = "%registry%/%registry.owner%/%service.name%:%build.number% %registry%/%registry.owner%/%service.name%:latest"
                        }
                    }

                    dockerCommand {
                        name = "Push Image"
                        commandType = push {
                            namesAndTags = """
                                %registry%/%registry.owner%/%service.name%:%build.number%
                                %registry%/%registry.owner%/%service.name%:latest
                            """.trimIndent()
                        }
                    }

                    script {
                        name = "Scan Image with Trivy"
                        scriptContent = """
                            docker run --rm \
                                -v /var/run/docker.sock:/var/run/docker.sock \
                                aquasec/trivy image \
                                --severity HIGH,CRITICAL \
                                --format table \
                                %registry%/%registry.owner%/%service.name%:%build.number%
                        """.trimIndent()
                    }
                }

                features {
                    dockerSupport {
                        loginToRegistry = on {
                            dockerRegistryId = "PROJECT_EXT_DockerRegistry"
                        }
                    }
                }
            })

            // ─── Release ───────────────────────────────────────────────────

            object Release : BuildType({
                name = "Release"
                description = "Create a versioned release of {{ Service.PascalName }}"

                vcs {
                    root(GitVcs)
                    cleanCheckout = true
                }

                params {
                    text("release.version", "", display = ParameterDisplay.PROMPT,
                        label = "Release Version",
                        description = "Semantic version (e.g., 1.0.0)")
                    checkbox("release.prerelease", "false",
                        label = "Pre-release",
                        checked = "true", unchecked = "false")
                }

                steps {
                    dotnetRestore {
                        name = "Restore"
                        projects = "%service.name%/"
                    }

                    dotnetBuild {
                        name = "Build Release"
                        projects = "%service.name%/"
                        configuration = "Release"
                        args = "/p:Version=%release.version% --no-restore"
                    }

                    dotnetTest {
                        name = "Run Tests"
                        projects = "%service.name%/"
                        configuration = "Release"
                        args = "--no-build"
                    }

                    dotnetPublish {
                        name = "Publish"
                        projects = "%service.name%/src/%service.pascal%.Api/"
                        configuration = "Release"
                        outputDir = "./release"
                        args = "/p:Version=%release.version% /p:UseAppHost=false"
                    }

                    script {
                        name = "Package Release"
                        scriptContent = """
                            cd release
                            tar -czf ../%service.name%-%release.version%.tar.gz .
                            echo "Release package created: %service.name%-%release.version%.tar.gz"
                        """.trimIndent()
                    }

                    script {
                        name = "Tag Release"
                        scriptContent = """
                            git tag -a "v%release.version%" -m "Release %service.pascal% v%release.version%"
                            git push origin "v%release.version%"
                        """.trimIndent()
                    }
                }

                artifactRules = """
                    %service.name%-%release.version%.tar.gz => releases/
                    release/** => %service.name%-%release.version%/
                """.trimIndent()
            })
            """";

        public const string PomXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <project xmlns="http://maven.apache.org/POM/4.0.0"
                     xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                     xsi:schemaLocation="http://maven.apache.org/POM/4.0.0
                     http://maven.apache.org/xsd/maven-4.0.0.xsd">
                <modelVersion>4.0.0</modelVersion>

                <groupId>app.thewatch</groupId>
                <artifactId>{{ Service.KebabName }}-teamcity</artifactId>
                <version>1.0-SNAPSHOT</version>
                <packaging>pom</packaging>

                <repositories>
                    <repository>
                        <id>jetbrains-all</id>
                        <url>https://download.jetbrains.com/teamcity-repository</url>
                    </repository>
                </repositories>

                <pluginRepositories>
                    <pluginRepository>
                        <id>JetBrains</id>
                        <url>https://download.jetbrains.com/teamcity-repository</url>
                    </pluginRepository>
                </pluginRepositories>

                <build>
                    <sourceDirectory>${project.basedir}</sourceDirectory>
                    <plugins>
                        <plugin>
                            <groupId>org.jetbrains.teamcity</groupId>
                            <artifactId>teamcity-configs-maven-plugin</artifactId>
                            <version>2024.12</version>
                            <configuration>
                                <format>kotlin</format>
                            </configuration>
                        </plugin>
                        <plugin>
                            <groupId>org.jetbrains.kotlin</groupId>
                            <artifactId>kotlin-maven-plugin</artifactId>
                            <version>1.9.25</version>
                            <executions>
                                <execution>
                                    <id>compile</id>
                                    <phase>process-sources</phase>
                                    <goals><goal>compile</goal></goals>
                                </execution>
                            </executions>
                        </plugin>
                    </plugins>
                </build>

                <dependencies>
                    <dependency>
                        <groupId>org.jetbrains.teamcity</groupId>
                        <artifactId>configs-dsl-kotlin-latest</artifactId>
                        <version>1.0-SNAPSHOT</version>
                        <scope>compile</scope>
                    </dependency>
                </dependencies>
            </project>
            """;
    }
}
