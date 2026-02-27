# TheWatch P3 MeshNetwork — Multi-stage Dockerfile
# Build context: solution root (e.g., docker build -f src/services/TheWatch.P3.MeshNetwork/Dockerfile .)

# renovate: datasource=docker depName=mcr.microsoft.com/dotnet/aspnet
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-noble AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# renovate: datasource=docker depName=mcr.microsoft.com/dotnet/sdk
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /repo

COPY nuget.config .

COPY src/shared/TheWatch.Shared/TheWatch.Shared.csproj src/shared/TheWatch.Shared/
COPY src/shared/TheWatch.Generators/TheWatch.Generators.csproj src/shared/TheWatch.Generators/
COPY src/services/TheWatch.P3.MeshNetwork/TheWatch.P3.MeshNetwork.csproj src/services/TheWatch.P3.MeshNetwork/


COPY meta/ meta/

RUN dotnet restore src/services/TheWatch.P3.MeshNetwork/TheWatch.P3.MeshNetwork.csproj

COPY src/shared/TheWatch.Shared/ src/shared/TheWatch.Shared/
COPY src/shared/TheWatch.Generators/ src/shared/TheWatch.Generators/
COPY src/services/TheWatch.P3.MeshNetwork/ src/services/TheWatch.P3.MeshNetwork/

WORKDIR /repo/src/services/TheWatch.P3.MeshNetwork
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

ENTRYPOINT ["dotnet", "TheWatch.P3.MeshNetwork.dll"]
