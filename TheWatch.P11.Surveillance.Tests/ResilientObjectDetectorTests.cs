using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TheWatch.Shared.ML;

namespace TheWatch.P11.Surveillance.Tests;

public class ResilientObjectDetectorTests
{
    private readonly IObjectDetector _localDetector = Substitute.For<IObjectDetector>();
    private readonly ICloudObjectDetector _azureDetector = Substitute.For<ICloudObjectDetector>();
    private readonly ICloudObjectDetector _gcpDetector = Substitute.For<ICloudObjectDetector>();
    private readonly ILogger<ResilientObjectDetector> _logger = Substitute.For<ILogger<ResilientObjectDetector>>();

    private ResilientObjectDetector CreateDetector(CloudObjectDetectorOptions? options = null)
    {
        var opts = options ?? new CloudObjectDetectorOptions
        {
            EnableCloudFallback = true,
            ProviderPriority = ["Azure", "GoogleCloud", "AWS"]
        };

        _azureDetector.ProviderName.Returns("Azure");
        _gcpDetector.ProviderName.Returns("GoogleCloud");

        return new ResilientObjectDetector(
            _localDetector,
            [_azureDetector, _gcpDetector],
            Options.Create(opts),
            _logger);
    }

    [Fact]
    public async Task DetectObjects_LocalReady_ReturnsLocalResults()
    {
        _localDetector.IsReady.Returns(true);
        var expected = new List<ObjectDetectionResult>
        {
            new() { Label = "person", Confidence = 0.9f, DetectionType = DetectedObjectType.Person }
        };
        _localDetector.DetectObjectsAsync(Arg.Any<byte[]>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var detector = CreateDetector();
        var results = await detector.DetectObjectsAsync([], 0.5f);

        results.Should().BeEquivalentTo(expected);
        // Should not call cloud detectors
        await _azureDetector.DidNotReceive().DetectObjectsAsync(Arg.Any<byte[]>(), Arg.Any<float>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DetectObjects_LocalFails_FallsBackToCloud()
    {
        _localDetector.IsReady.Returns(true);
        _localDetector.DetectObjectsAsync(Arg.Any<byte[]>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("ONNX model failed"));

        _azureDetector.IsConfigured.Returns(true);
        var cloudResults = new List<ObjectDetectionResult>
        {
            new() { Label = "vehicle", Confidence = 0.85f, DetectionType = DetectedObjectType.Vehicle }
        };
        _azureDetector.DetectObjectsAsync(Arg.Any<byte[]>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns(cloudResults);

        var detector = CreateDetector();
        var results = await detector.DetectObjectsAsync([], 0.5f);

        results.Should().BeEquivalentTo(cloudResults);
    }

    [Fact]
    public async Task DetectObjects_LocalNotReady_FallsBackToCloud()
    {
        _localDetector.IsReady.Returns(false);

        _azureDetector.IsConfigured.Returns(true);
        var cloudResults = new List<ObjectDetectionResult>
        {
            new() { Label = "person", Confidence = 0.8f, DetectionType = DetectedObjectType.Person }
        };
        _azureDetector.DetectObjectsAsync(Arg.Any<byte[]>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns(cloudResults);

        var detector = CreateDetector();
        var results = await detector.DetectObjectsAsync([], 0.5f);

        results.Should().BeEquivalentTo(cloudResults);
    }

    [Fact]
    public async Task DetectObjects_FirstCloudFails_FallsBackToNext()
    {
        _localDetector.IsReady.Returns(false);

        _azureDetector.IsConfigured.Returns(true);
        _azureDetector.DetectObjectsAsync(Arg.Any<byte[]>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Azure timeout"));

        _gcpDetector.IsConfigured.Returns(true);
        var gcpResults = new List<ObjectDetectionResult>
        {
            new() { Label = "weapon", Confidence = 0.95f, DetectionType = DetectedObjectType.Weapon }
        };
        _gcpDetector.DetectObjectsAsync(Arg.Any<byte[]>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns(gcpResults);

        var detector = CreateDetector();
        var results = await detector.DetectObjectsAsync([], 0.5f);

        results.Should().BeEquivalentTo(gcpResults);
    }

    [Fact]
    public async Task DetectObjects_CloudFallbackDisabled_ReturnsEmpty()
    {
        _localDetector.IsReady.Returns(false);

        var opts = new CloudObjectDetectorOptions { EnableCloudFallback = false };
        var detector = CreateDetector(opts);
        var results = await detector.DetectObjectsAsync([], 0.5f);

        results.Should().BeEmpty();
    }

    [Fact]
    public void IsReady_LocalReady_ReturnsTrue()
    {
        _localDetector.IsReady.Returns(true);
        var detector = CreateDetector();
        detector.IsReady.Should().BeTrue();
    }

    [Fact]
    public void IsReady_LocalNotReadyButCloudAvailable_ReturnsTrue()
    {
        _localDetector.IsReady.Returns(false);
        _azureDetector.IsConfigured.Returns(true);

        var detector = CreateDetector();
        detector.IsReady.Should().BeTrue();
    }

    [Fact]
    public void IsReady_NothingAvailable_ReturnsFalse()
    {
        _localDetector.IsReady.Returns(false);
        _azureDetector.IsConfigured.Returns(false);
        _gcpDetector.IsConfigured.Returns(false);

        var opts = new CloudObjectDetectorOptions { EnableCloudFallback = false };
        var detector = CreateDetector(opts);
        detector.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task DetectObjects_RespectsProviderPriority()
    {
        _localDetector.IsReady.Returns(false);

        // Both configured, but GCP should be tried first per custom priority
        _azureDetector.IsConfigured.Returns(true);
        _gcpDetector.IsConfigured.Returns(true);

        var gcpResults = new List<ObjectDetectionResult>
        {
            new() { Label = "person", Confidence = 0.88f, DetectionType = DetectedObjectType.Person }
        };
        _gcpDetector.DetectObjectsAsync(Arg.Any<byte[]>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns(gcpResults);

        var opts = new CloudObjectDetectorOptions
        {
            EnableCloudFallback = true,
            ProviderPriority = ["GoogleCloud", "Azure"] // GCP first
        };

        var detector = CreateDetector(opts);
        var results = await detector.DetectObjectsAsync([], 0.5f);

        results.Should().BeEquivalentTo(gcpResults);
        // Azure should NOT have been called since GCP succeeded
        await _azureDetector.DidNotReceive().DetectObjectsAsync(Arg.Any<byte[]>(), Arg.Any<float>(), Arg.Any<CancellationToken>());
    }
}
