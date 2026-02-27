# TheWatch P5 AuthSecurity — Multi-stage Dockerfile
# Build context: solution root (e.g., docker build -f TheWatch.P5.AuthSecurity/Dockerfile .)

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

COPY TheWatch.Shared/TheWatch.Shared.csproj TheWatch.Shared/
COPY TheWatch.Generators/TheWatch.Generators.csproj TheWatch.Generators/
COPY TheWatch.P5.AuthSecurity/TheWatch.P5.AuthSecurity.csproj TheWatch.P5.AuthSecurity/

COPY Models.json .
COPY Interfaces.json .
COPY _mapping.json .

RUN dotnet restore TheWatch.P5.AuthSecurity/TheWatch.P5.AuthSecurity.csproj

COPY TheWatch.Shared/ TheWatch.Shared/
COPY TheWatch.Generators/ TheWatch.Generators/
COPY TheWatch.P5.AuthSecurity/ TheWatch.P5.AuthSecurity/

WORKDIR /src/TheWatch.P5.AuthSecurity
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

ENTRYPOINT ["dotnet", "TheWatch.P5.AuthSecurity.dll"]
