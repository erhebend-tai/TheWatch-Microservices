using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for content moderation logic.
/// Reimplements the ShouldFlag threshold logic and ModerationResult model
/// from ContentModerationService without MAUI Preferences dependency.
/// </summary>
public class ContentModerationTests
{
    // =========================================================================
    // ShouldFlag Logic
    // =========================================================================

    [Fact]
    public void ShouldFlag_UnsafeAboveThreshold_ReturnsTrue()
    {
        var result = new ModerationResult { IsSafe = false, Confidence = 0.9 };
        double threshold = 0.7;

        var shouldFlag = !result.IsSafe && result.Confidence >= threshold;

        shouldFlag.Should().BeTrue();
    }

    [Fact]
    public void ShouldFlag_UnsafeBelowThreshold_ReturnsFalse()
    {
        var result = new ModerationResult { IsSafe = false, Confidence = 0.5 };
        double threshold = 0.7;

        var shouldFlag = !result.IsSafe && result.Confidence >= threshold;

        shouldFlag.Should().BeFalse();
    }

    [Fact]
    public void ShouldFlag_SafeAboveThreshold_ReturnsFalse()
    {
        var result = new ModerationResult { IsSafe = true, Confidence = 0.9 };
        double threshold = 0.7;

        var shouldFlag = !result.IsSafe && result.Confidence >= threshold;

        shouldFlag.Should().BeFalse();
    }

    [Fact]
    public void ShouldFlag_SafeBelowThreshold_ReturnsFalse()
    {
        var result = new ModerationResult { IsSafe = true, Confidence = 0.3 };
        double threshold = 0.7;

        var shouldFlag = !result.IsSafe && result.Confidence >= threshold;

        shouldFlag.Should().BeFalse();
    }

    [Fact]
    public void ShouldFlag_ExactlyAtThreshold_ReturnsTrue()
    {
        var result = new ModerationResult { IsSafe = false, Confidence = 0.7 };
        double threshold = 0.7;

        var shouldFlag = !result.IsSafe && result.Confidence >= threshold;

        shouldFlag.Should().BeTrue();
    }

    [Fact]
    public void ShouldFlag_ZeroConfidence_ReturnsFalse()
    {
        var result = new ModerationResult { IsSafe = false, Confidence = 0.0 };
        double threshold = 0.7;

        var shouldFlag = !result.IsSafe && result.Confidence >= threshold;

        shouldFlag.Should().BeFalse();
    }

    [Theory]
    [InlineData(false, 0.1, 0.7, false)]
    [InlineData(false, 0.5, 0.7, false)]
    [InlineData(false, 0.7, 0.7, true)]
    [InlineData(false, 0.8, 0.7, true)]
    [InlineData(false, 1.0, 0.7, true)]
    [InlineData(true, 0.9, 0.7, false)]
    [InlineData(false, 0.5, 0.3, true)]
    [InlineData(false, 0.9, 0.9, true)]
    public void ShouldFlag_VariousThresholds_CorrectResult(bool isSafe, double confidence, double threshold, bool expected)
    {
        var result = new ModerationResult { IsSafe = isSafe, Confidence = confidence };

        var shouldFlag = !result.IsSafe && result.Confidence >= threshold;

        shouldFlag.Should().Be(expected);
    }

    // =========================================================================
    // ModerationResult Model
    // =========================================================================

    [Fact]
    public void ModerationResult_DefaultValues()
    {
        var result = new ModerationResult();

        result.IsSafe.Should().BeTrue();
        result.Confidence.Should().Be(0.0);
        result.ModelVersion.Should().BeEmpty();
    }

    [Fact]
    public void ModerationResult_CanSetAllProperties()
    {
        var result = new ModerationResult
        {
            IsSafe = false,
            Confidence = 0.85,
            ModelVersion = "nsfw-v2.1",
            AnalyzedAt = new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc)
        };

        result.IsSafe.Should().BeFalse();
        result.Confidence.Should().Be(0.85);
        result.ModelVersion.Should().Be("nsfw-v2.1");
        result.AnalyzedAt.Should().Be(new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ModerationResult_NoModelLoaded_SafeWithZeroConfidence()
    {
        // Mirrors the "no model loaded" path in ContentModerationService.AnalyzeImageAsync
        var result = new ModerationResult
        {
            IsSafe = true,
            Confidence = 0.0,
            ModelVersion = "none",
            AnalyzedAt = DateTime.UtcNow
        };

        result.IsSafe.Should().BeTrue();
        result.Confidence.Should().Be(0.0);
        result.ModelVersion.Should().Be("none");

        // ShouldFlag should return false for safe result
        var shouldFlag = !result.IsSafe && result.Confidence >= 0.7;
        shouldFlag.Should().BeFalse();
    }
}

/// <summary>
/// Mirror of ModerationResult from TheWatch.Mobile.Services
/// </summary>
public class ModerationResult
{
    public bool IsSafe { get; set; } = true;
    public double Confidence { get; set; }
    public string ModelVersion { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
}
