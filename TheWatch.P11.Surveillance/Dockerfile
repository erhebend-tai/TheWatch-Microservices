# TheWatch.P11.Surveillance — Multi-stage Docker build
# renovate: datasource=docker depName=mcr.microsoft.com/dotnet/aspnet
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-noble AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# renovate: datasource=docker depName=mcr.microsoft.com/dotnet/sdk
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-noble AS build
WORKDIR /src

# Copy nuget config and data files needed by generators
COPY nuget.config .
COPY Models.json .
COPY Interfaces.json .
COPY _mapping.json .
COPY Controllers.json .

# Copy project files for restore
COPY TheWatch.Shared/TheWatch.Shared.csproj TheWatch.Shared/
COPY TheWatch.Generators/TheWatch.Generators.csproj TheWatch.Generators/
COPY TheWatch.P11.Surveillance/TheWatch.P11.Surveillance.csproj TheWatch.P11.Surveillance/

# Restore
RUN dotnet restore TheWatch.P11.Surveillance/TheWatch.P11.Surveillance.csproj

# Copy source
COPY TheWatch.Shared/ TheWatch.Shared/
COPY TheWatch.Generators/ TheWatch.Generators/
COPY TheWatch.P11.Surveillance/ TheWatch.P11.Surveillance/

# Build
RUN dotnet publish TheWatch.P11.Surveillance/TheWatch.P11.Surveillance.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app

# STIG V-222425: Run as non-root user
RUN adduser --disabled-password --no-create-home --uid 1001 appuser
USER appuser

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TheWatch.P11.Surveillance.dll"]
