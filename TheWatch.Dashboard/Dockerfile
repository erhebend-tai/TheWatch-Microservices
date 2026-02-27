# TheWatch Dashboard — Multi-stage Dockerfile (Blazor Server)
# Build context: solution root (e.g., docker build -f TheWatch.Dashboard/Dockerfile .)

# renovate: datasource=docker depName=mcr.microsoft.com/dotnet/aspnet
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-noble AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# renovate: datasource=docker depName=mcr.microsoft.com/dotnet/sdk
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY nuget.config .

# Dashboard depends on Shared, ServiceDefaults, and Generators
COPY TheWatch.Shared/TheWatch.Shared.csproj TheWatch.Shared/
COPY TheWatch.Generators/TheWatch.Generators.csproj TheWatch.Generators/
COPY TheWatch.Aspire.ServiceDefaults/TheWatch.Aspire.ServiceDefaults.csproj TheWatch.Aspire.ServiceDefaults/
COPY TheWatch.Dashboard/TheWatch.Dashboard.csproj TheWatch.Dashboard/

RUN dotnet restore TheWatch.Dashboard/TheWatch.Dashboard.csproj

COPY TheWatch.Shared/ TheWatch.Shared/
COPY TheWatch.Generators/ TheWatch.Generators/
COPY TheWatch.Aspire.ServiceDefaults/ TheWatch.Aspire.ServiceDefaults/
COPY TheWatch.Dashboard/ TheWatch.Dashboard/

WORKDIR /src/TheWatch.Dashboard
RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# STIG V-222425: Run as non-root user
RUN adduser --disabled-password --no-create-home --uid 1001 appuser
USER appuser

COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TheWatch.Dashboard.dll"]
