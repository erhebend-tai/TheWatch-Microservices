# Stream 4: DOCKER-K8S — Stage 10 (TODO Items 106-120)

You are working in a git worktree of TheWatch microservices solution. Your task is to create all Docker, Kubernetes/Helm, and CI/CD infrastructure.

## YOUR ASSIGNED TODO ITEMS

### 10A. Docker (106-110)
106. Create multi-stage Dockerfile for each of the 10 microservices
107. Create Dockerfile for Dashboard (Blazor Server)
108. Create docker-compose.yml for full local development stack (services + SQL + Redis + Kafka)
109. Create docker-compose.override.yml for development-specific config (ports, volumes)
110. Add .dockerignore files to prevent including bin/obj/node_modules

### 10B. Kubernetes / Helm (111-115)
111. Create Helm chart template with deployment, service, ingress per microservice
112. Configure HPA for P2 VoiceEmergency and P6 FirstResponder (emergency surge scaling)
113. Create ConfigMaps for service configuration (feature flags, endpoints)
114. Create Kubernetes Secrets for database credentials and JWT signing keys
115. Configure Ingress Controller with TLS termination and path-based routing

### 10C. CI/CD (116-120)
116. Create GitHub Actions workflow: build + test on every PR
117. Create GitHub Actions workflow: build Docker images and push to Azure Container Registry
118. Create GitHub Actions workflow: deploy to staging on merge to develop
119. Create GitHub Actions workflow: deploy to production on merge to main (manual approval gate)
120. Add CodeQL security scanning and dependency review to PR workflow

## FILES YOU MAY CREATE/MODIFY

### Dockerfiles (NEW files in each project root):
- `TheWatch.P1.CoreGateway/Dockerfile`
- `TheWatch.P2.VoiceEmergency/Dockerfile`
- `TheWatch.P3.MeshNetwork/Dockerfile`
- `TheWatch.P4.Wearable/Dockerfile`
- `TheWatch.P5.AuthSecurity/Dockerfile`
- `TheWatch.P6.FirstResponder/Dockerfile`
- `TheWatch.P7.FamilyHealth/Dockerfile`
- `TheWatch.P8.DisasterRelief/Dockerfile`
- `TheWatch.P9.DoctorServices/Dockerfile`
- `TheWatch.P10.Gamification/Dockerfile`
- `TheWatch.Geospatial/Dockerfile`
- `TheWatch.Dashboard/Dockerfile`

### Docker ignore (NEW files):
- `.dockerignore` at repo root (shared)
- Each project can use the root .dockerignore

### Compose files (NEW at repo root):
- `docker-compose.yml`
- `docker-compose.override.yml`

### Helm charts (NEW directory):
- `helm/thewatch/Chart.yaml`
- `helm/thewatch/values.yaml`
- `helm/thewatch/templates/` (deployment, service, ingress, hpa, configmap, secret per service)

### GitHub Actions (NEW directory):
- `.github/workflows/ci.yml` (build + test on PR)
- `.github/workflows/docker-publish.yml` (build + push images)
- `.github/workflows/deploy-staging.yml` (deploy on develop merge)
- `.github/workflows/deploy-production.yml` (deploy on main merge, manual gate)
- `.github/workflows/security.yml` (CodeQL + dependency review)

## FILES YOU MUST NOT TOUCH

- Any `.cs` file (do NOT modify code)
- Any `.csproj` file
- `TheWatch.sln`
- `TheWatch.Shared/`
- `TheWatch.Mobile/`
- `infra/` directory

## DOCKERFILE PATTERN (multi-stage)

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-level files
COPY nuget.config .
COPY TheWatch.sln .

# Copy project files for restore
COPY TheWatch.Shared/TheWatch.Shared.csproj TheWatch.Shared/
COPY TheWatch.Generators/TheWatch.Generators.csproj TheWatch.Generators/
COPY TheWatch.Aspire.ServiceDefaults/TheWatch.Aspire.ServiceDefaults.csproj TheWatch.Aspire.ServiceDefaults/
COPY TheWatch.P1.CoreGateway/TheWatch.P1.CoreGateway.csproj TheWatch.P1.CoreGateway/

# Restore
RUN dotnet restore TheWatch.P1.CoreGateway/TheWatch.P1.CoreGateway.csproj

# Copy everything and build
COPY . .
RUN dotnet publish TheWatch.P1.CoreGateway/TheWatch.P1.CoreGateway.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TheWatch.P1.CoreGateway.dll"]
```

Note: Each service depends on TheWatch.Shared, TheWatch.Generators, and TheWatch.Aspire.ServiceDefaults. The Dockerfile must COPY these project files for restore. The Generators project is netstandard2.0 (source generator), Shared and ServiceDefaults are class libraries.

Also note: The local NuGet source in nuget.config references `E:\json_output\Nugets\Hangfire` — for Docker builds, either:
1. Copy the Hangfire .nupkg files into a build-stage directory, or
2. Add a Docker-specific nuget.config that skips the local source (if packages are cached in restore)

Best approach: Create a `docker/nuget/` directory with the Hangfire Pro .nupkg files and reference it in a Docker-specific nuget.config.

## DOCKER-COMPOSE PATTERN

```yaml
services:
  sql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "YourStr0ngP@ssw0rd"
    ports:
      - "1433:1433"

  kafka:
    image: confluentinc/cp-kafka:latest
    # ...

  postgis:
    image: postgis/postgis:16-3.4
    # ...

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  p1-coregateway:
    build:
      context: .
      dockerfile: TheWatch.P1.CoreGateway/Dockerfile
    ports:
      - "5101:8080"
    depends_on:
      - sql
    environment:
      ConnectionStrings__WatchCoreGatewayDB: "Server=sql;Database=WatchCoreGatewayDB;User=sa;Password=YourStr0ngP@ssw0rd;TrustServerCertificate=true"
  # ... repeat for P2-P10, Geospatial, Dashboard
```

## HELM CHART PATTERN

```yaml
# helm/thewatch/templates/deployment.yaml
{{- range .Values.services }}
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ .name }}
spec:
  replicas: {{ .replicas | default 1 }}
  selector:
    matchLabels:
      app: {{ .name }}
  template:
    metadata:
      labels:
        app: {{ .name }}
    spec:
      containers:
        - name: {{ .name }}
          image: "{{ $.Values.image.registry }}/{{ .image }}:{{ $.Values.image.tag }}"
          ports:
            - containerPort: 8080
          envFrom:
            - configMapRef:
                name: {{ .name }}-config
            - secretRef:
                name: {{ .name }}-secrets
---
{{- end }}
```

## HPA FOR P2 AND P6

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: p2-voiceemergency-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: p2-voiceemergency
  minReplicas: 2
  maxReplicas: 20
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 60
```

## GITHUB ACTIONS CI PATTERN

```yaml
name: CI
on:
  pull_request:
    branches: [main, develop]
jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal
```

## WHEN DONE

Commit all changes with message:
```
feat(devops): add Dockerfiles, docker-compose, Helm charts, GitHub Actions CI/CD

Items 106-120: Multi-stage Dockerfiles for 12 projects, docker-compose with
SQL/Kafka/PostGIS/Redis, Helm chart with HPA/ConfigMaps/Secrets/Ingress,
GitHub Actions for build/test/deploy/security
```
