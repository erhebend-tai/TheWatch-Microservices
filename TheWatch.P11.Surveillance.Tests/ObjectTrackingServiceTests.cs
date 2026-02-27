using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TheWatch.P11.Surveillance.Surveillance;
using TheWatch.Shared.ML;

namespace TheWatch.P11.Surveillance.Tests;

public class ObjectTrackingServiceTests
{
    private readonly IWatchRepository<TrackingSession> _sessions = Substitute.For<IWatchRepository<TrackingSession>>();
    private readonly IWatchRepository<TrackedObjectMatch> _matches = Substitute.For<IWatchRepository<TrackedObjectMatch>>();
    private readonly IWatchRepository<CrimeLocation> _crimeLocations = Substitute.For<IWatchRepository<CrimeLocation>>();
    private readonly IWatchRepository<DetectionResult> _detections = Substitute.For<IWatchRepository<DetectionResult>>();
    private readonly Services.IFootageService _footageService = Substitute.For<Services.IFootageService>();
    private readonly Services.IVideoAnalysisService _videoAnalysisService = Substitute.For<Services.IVideoAnalysisService>();
    private readonly IObjectDetector _objectDetector = Substitute.For<IObjectDetector>();
    private readonly ILogger<Services.ObjectTrackingService> _logger = Substitute.For<ILogger<Services.ObjectTrackingService>>();

    private Services.ObjectTrackingService CreateService() => new(
        _sessions, _matches, _crimeLocations, _footageService,
        _videoAnalysisService, _detections, _objectDetector, _logger);

    [Fact]
    public async Task TrackObjects_CrimeLocationNotFound_ReturnsFailed()
    {
        _crimeLocations.GetByIdAsync(Arg.Any<Guid>()).Returns((CrimeLocation?)null);
        var svc = CreateService();

        var result = await svc.TrackObjectsAsync(new ObjectTrackingRequest(
            Guid.NewGuid(), Guid.NewGuid(), "Red sedan"));

        result.Status.Should().Be(TrackingStatus.Failed);
        result.TrackingSessionId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task TrackObjects_WithMatchingDetections_ReturnsCompletedWithMatches()
    {
        var crimeId = Guid.NewGuid();
        var crimeLocation = new CrimeLocation
        {
            Id = crimeId,
            Latitude = 34.0522,
            Longitude = -118.2437,
            Description = "Reported break-in",
            CrimeType = "Burglary",
            OccurredAt = DateTime.UtcNow.AddHours(-1)
        };

        _crimeLocations.GetByIdAsync(crimeId).Returns(crimeLocation);
        _objectDetector.IsReady.Returns(true);
        _sessions.AddAsync(Arg.Any<TrackingSession>()).Returns(ci => ci.Arg<TrackingSession>());

        var footageId = Guid.NewGuid();
        var footage = new FootageSubmission
        {
            Id = footageId,
            GpsLatitude = 34.0530,
            GpsLongitude = -118.2440,
            Status = FootageStatus.Analyzed
        };

        _footageService.FindFootageNearLocationAsync(
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>())
            .Returns([footage]);

        var detection = new DetectionResult
        {
            Id = Guid.NewGuid(),
            FootageId = footageId,
            DetectionType = DetectionType.Vehicle,
            Label = "vehicle",
            Confidence = 0.92f,
            FrameTimestamp = DateTime.UtcNow.AddMinutes(-30)
        };

        _footageService.GetDetectionsForFootageAsync(footageId, 1, 100)
            .Returns(new DetectionListResponse([detection], 1, 1, 100));

        _matches.AddAsync(Arg.Any<TrackedObjectMatch>()).Returns(ci => ci.Arg<TrackedObjectMatch>());

        var svc = CreateService();

        var result = await svc.TrackObjectsAsync(new ObjectTrackingRequest(
            crimeId, Guid.NewGuid(), "Red sedan",
            [DetectionType.Vehicle]));

        result.Status.Should().Be(TrackingStatus.Completed);
        result.FootageAnalyzedCount.Should().Be(1);
        result.MatchesFoundCount.Should().Be(1);
        result.Matches.Should().HaveCount(1);
        result.Matches[0].Label.Should().Be("vehicle");
        result.Matches[0].Confidence.Should().Be(0.92f);
    }

    [Fact]
    public async Task TrackObjects_AnalyzesUnprocessedFootage()
    {
        var crimeId = Guid.NewGuid();
        var crimeLocation = new CrimeLocation
        {
            Id = crimeId,
            Latitude = 34.0522,
            Longitude = -118.2437,
            Description = "Theft in progress",
            CrimeType = "Theft",
            OccurredAt = DateTime.UtcNow.AddHours(-1)
        };

        _crimeLocations.GetByIdAsync(crimeId).Returns(crimeLocation);
        _objectDetector.IsReady.Returns(true);
        _sessions.AddAsync(Arg.Any<TrackingSession>()).Returns(ci => ci.Arg<TrackingSession>());

        var footageId = Guid.NewGuid();
        var footage = new FootageSubmission
        {
            Id = footageId,
            GpsLatitude = 34.0530,
            GpsLongitude = -118.2440,
            Status = FootageStatus.Submitted // Not yet analyzed
        };

        _footageService.FindFootageNearLocationAsync(
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>())
            .Returns([footage]);

        _videoAnalysisService.AnalyzeFootageAsync(footageId, Arg.Any<float>())
            .Returns(new List<DetectionResult>());

        _footageService.GetDetectionsForFootageAsync(footageId, 1, 100)
            .Returns(new DetectionListResponse([], 0, 1, 100));

        var svc = CreateService();

        var result = await svc.TrackObjectsAsync(new ObjectTrackingRequest(
            crimeId, Guid.NewGuid(), "Suspicious person"));

        // Should have triggered analysis for unprocessed footage
        await _videoAnalysisService.Received(1).AnalyzeFootageAsync(footageId, Arg.Any<float>());
        result.Status.Should().Be(TrackingStatus.Completed);
        result.FootageAnalyzedCount.Should().Be(1);
    }

    [Fact]
    public async Task TrackObjects_MatchesOrderedByDistance()
    {
        var crimeId = Guid.NewGuid();
        var crimeLocation = new CrimeLocation
        {
            Id = crimeId,
            Latitude = 34.0522,
            Longitude = -118.2437,
            Description = "Break-in",
            CrimeType = "Burglary",
            OccurredAt = DateTime.UtcNow.AddHours(-1)
        };

        _crimeLocations.GetByIdAsync(crimeId).Returns(crimeLocation);
        _objectDetector.IsReady.Returns(false);
        _sessions.AddAsync(Arg.Any<TrackingSession>()).Returns(ci => ci.Arg<TrackingSession>());

        // Close footage
        var closeFootageId = Guid.NewGuid();
        var closeFootage = new FootageSubmission
        {
            Id = closeFootageId,
            GpsLatitude = 34.0525, // Very close
            GpsLongitude = -118.2440,
            Status = FootageStatus.Analyzed
        };

        // Far footage
        var farFootageId = Guid.NewGuid();
        var farFootage = new FootageSubmission
        {
            Id = farFootageId,
            GpsLatitude = 34.0700, // Further away
            GpsLongitude = -118.2600,
            Status = FootageStatus.Analyzed
        };

        _footageService.FindFootageNearLocationAsync(
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>())
            .Returns([farFootage, closeFootage]); // Far footage returned first

        var closeDetection = new DetectionResult
        {
            Id = Guid.NewGuid(),
            FootageId = closeFootageId,
            DetectionType = DetectionType.Person,
            Label = "person",
            Confidence = 0.85f,
            FrameTimestamp = DateTime.UtcNow
        };

        var farDetection = new DetectionResult
        {
            Id = Guid.NewGuid(),
            FootageId = farFootageId,
            DetectionType = DetectionType.Person,
            Label = "person",
            Confidence = 0.75f,
            FrameTimestamp = DateTime.UtcNow
        };

        _footageService.GetDetectionsForFootageAsync(closeFootageId, 1, 100)
            .Returns(new DetectionListResponse([closeDetection], 1, 1, 100));
        _footageService.GetDetectionsForFootageAsync(farFootageId, 1, 100)
            .Returns(new DetectionListResponse([farDetection], 1, 1, 100));

        _matches.AddAsync(Arg.Any<TrackedObjectMatch>()).Returns(ci => ci.Arg<TrackedObjectMatch>());

        var svc = CreateService();

        var result = await svc.TrackObjectsAsync(new ObjectTrackingRequest(
            crimeId, Guid.NewGuid(), "Suspicious person"));

        result.Status.Should().Be(TrackingStatus.Completed);
        result.Matches.Should().HaveCount(2);
        // Results should be ordered by distance (closest first)
        result.Matches[0].DistanceFromSceneKm.Should().BeLessThan(result.Matches[1].DistanceFromSceneKm);
    }
}
