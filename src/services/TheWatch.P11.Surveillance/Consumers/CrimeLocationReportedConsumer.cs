// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TheWatch.P11.Surveillance.Surveillance;
using TheWatch.P11.Surveillance.Services;
using TheWatch.Shared.Events;

namespace TheWatch.P11.Surveillance.Consumers;

/// <summary>
/// Consumes CrimeLocationReported events to search nearby cameras and queue footage for analysis.
/// </summary>
public sealed class CrimeLocationReportedConsumer : KafkaEventConsumer<CrimeLocationReportedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CrimeLocationReportedConsumer> _logger;

    public CrimeLocationReportedConsumer(
        IConfiguration configuration,
        IDeadLetterHandler deadLetterHandler,
        IEventAuditLogger auditLogger,
        IServiceProvider serviceProvider,
        ILogger<CrimeLocationReportedConsumer> logger)
        : base(
            KafkaSetup.CreateConsumer(configuration),
            deadLetterHandler,
            auditLogger,
            logger,
            WatchTopics.CrimeLocationReported,
            KafkaSetup.ConsumerGroup)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task HandleAsync(CrimeLocationReportedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("Processing crime location reported: {CrimeLocationId} at ({Lat}, {Lon})",
            evt.CrimeLocationId, evt.Latitude, evt.Longitude);

        using var scope = _serviceProvider.CreateScope();
        var cameraService = scope.ServiceProvider.GetRequiredService<ICameraService>();
        var footageService = scope.ServiceProvider.GetRequiredService<IFootageService>();
        var analysisService = scope.ServiceProvider.GetRequiredService<IVideoAnalysisService>();

        // Find nearby cameras
        var nearbyCameras = await cameraService.FindNearbyAsync(evt.Latitude, evt.Longitude, 2.0);
        _logger.LogInformation("Found {Count} cameras near crime location {CrimeLocationId}",
            nearbyCameras.Count, evt.CrimeLocationId);

        // Find and analyze relevant footage near the crime location
        var nearbyFootage = await footageService.FindFootageNearLocationAsync(
            evt.Latitude, evt.Longitude, 2.0);

        var analyzedCount = 0;
        foreach (var footage in nearbyFootage.Where(f => f.Status == FootageStatus.Submitted))
        {
            await analysisService.AnalyzeFootageAsync(footage.Id);
            analyzedCount++;
        }

        _logger.LogInformation("Analyzed {Count} footage submissions near crime location {CrimeLocationId}",
            analyzedCount, evt.CrimeLocationId);
    }
}
