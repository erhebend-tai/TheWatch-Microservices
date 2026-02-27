# TheWatch.P11.Surveillance — Multi-stage Docker build
# renovate: datasource=docker depName=mcr.microsoft.com/dotnet/aspnet
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-noble AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# renovate: datasource=docker depName=mcr.microsoft.com/dotnet/sdk
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-noble AS build
WORKDIR /repo

# Copy nuget config and data files needed by generators
COPY nuget.config .

# Copy project files for restore
COPY src/shared/TheWatch.Shared/TheWatch.Shared.csproj src/shared/TheWatch.Shared/
COPY src/shared/TheWatch.Generators/TheWatch.Generators.csproj src/shared/TheWatch.Generators/
COPY src/services/TheWatch.P11.Surveillance/TheWatch.P11.Surveillance.csproj src/services/TheWatch.P11.Surveillance/

# Restore
COPY meta/ meta/

RUN dotnet restore src/services/TheWatch.P11.Surveillance/TheWatch.P11.Surveillance.csproj

# Copy source
COPY src/shared/TheWatch.Shared/ src/shared/TheWatch.Shared/
COPY src/shared/TheWatch.Generators/ src/shared/TheWatch.Generators/
COPY src/services/TheWatch.P11.Surveillance/ src/services/TheWatch.P11.Surveillance/

# Build
RUN dotnet publish src/services/TheWatch.P11.Surveillance/TheWatch.P11.Surveillance.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app

# STIG V-222425: Run as non-root user
RUN adduser --disabled-password --no-create-home --uid 1001 appuser
USER appuser

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TheWatch.P11.Surveillance.dll"]
