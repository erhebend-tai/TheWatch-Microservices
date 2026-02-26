// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TheWatch.P11.Surveillance.Services;
using TheWatch.Shared.Events;

namespace TheWatch.P11.Surveillance.Consumers;

/// <summary>
/// Consumes FootageSubmitted events to trigger GPS verification + ML analysis pipeline.
/// </summary>
public sealed class FootageSubmittedConsumer : KafkaEventConsumer<FootageSubmittedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FootageSubmittedConsumer> _logger;

    public FootageSubmittedConsumer(
        IConfiguration configuration,
        IDeadLetterHandler deadLetterHandler,
        IEventAuditLogger auditLogger,
        IServiceProvider serviceProvider,
        ILogger<FootageSubmittedConsumer> logger)
        : base(
            KafkaSetup.CreateConsumer(configuration),
            deadLetterHandler,
            auditLogger,
            logger,
            WatchTopics.FootageSubmitted,
            KafkaSetup.ConsumerGroup)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task HandleAsync(FootageSubmittedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("Processing footage submitted event: {FootageId} from camera {CameraId}",
            evt.FootageId, evt.CameraId);

        using var scope = _serviceProvider.CreateScope();
        var analysisService = scope.ServiceProvider.GetRequiredService<IVideoAnalysisService>();

        var detections = await analysisService.AnalyzeFootageAsync(evt.FootageId);

        _logger.LogInformation("Footage {FootageId} analysis complete: {DetectionCount} detections",
            evt.FootageId, detections.Count);
    }
}
