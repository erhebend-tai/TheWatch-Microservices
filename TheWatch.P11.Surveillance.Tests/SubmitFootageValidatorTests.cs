using FluentAssertions;
using FluentValidation.TestHelper;
using TheWatch.P11.Surveillance.Surveillance;
using TheWatch.P11.Surveillance.Validators;

namespace TheWatch.P11.Surveillance.Tests;

public class SubmitFootageValidatorTests
{
    private readonly SubmitFootageRequestValidator _validator = new();

    private SubmitFootageRequest CreateValidRequest() => new(
        CameraId: Guid.NewGuid(),
        SubmitterId: Guid.NewGuid(),
        GpsLatitude: 34.0522,
        GpsLongitude: -118.2437,
        StartTime: DateTime.UtcNow.AddHours(-1),
        EndTime: DateTime.UtcNow,
        MediaUrl: "https://storage.example.com/footage/test.mp4",
        MediaType: MediaType.Video
    );

    // === Completeness ===

    [Fact]
    public void Valid_Request_Passes()
    {
        var result = _validator.TestValidate(CreateValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_CameraId_Fails()
    {
        var request = CreateValidRequest() with { CameraId = Guid.Empty };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CameraId);
    }

    [Fact]
    public void Empty_SubmitterId_Fails()
    {
        var request = CreateValidRequest() with { SubmitterId = Guid.Empty };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.SubmitterId);
    }

    // === Timeliness ===

    [Fact]
    public void StartTime_InFuture_Fails()
    {
        var request = CreateValidRequest() with
        {
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(3)
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.StartTime);
    }

    [Fact]
    public void StartTime_OlderThan30Days_Fails()
    {
        var request = CreateValidRequest() with
        {
            StartTime = DateTime.UtcNow.AddDays(-31),
            EndTime = DateTime.UtcNow.AddDays(-30)
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.StartTime);
    }

    [Fact]
    public void EndTime_BeforeStartTime_Fails()
    {
        var request = CreateValidRequest() with
        {
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow.AddHours(-2)
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.EndTime);
    }

    // === Relevance (duration) ===

    [Fact]
    public void Duration_Exceeds24Hours_Fails()
    {
        var request = CreateValidRequest() with
        {
            StartTime = DateTime.UtcNow.AddDays(-2),
            EndTime = DateTime.UtcNow
        };
        var result = _validator.TestValidate(request);
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("24 hours"));
    }

    [Fact]
    public void Duration_Within24Hours_Passes()
    {
        var request = CreateValidRequest() with
        {
            StartTime = DateTime.UtcNow.AddHours(-12),
            EndTime = DateTime.UtcNow
        };
        var result = _validator.TestValidate(request);
        result.Errors.Should().NotContain(e => e.ErrorMessage.Contains("24 hours"));
    }

    // === Accuracy (media URL) ===

    [Fact]
    public void MediaUrl_InvalidUri_Fails()
    {
        var request = CreateValidRequest() with { MediaUrl = "not-a-url" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MediaUrl);
    }

    [Theory]
    [InlineData("https://storage.example.com/footage/clip.mp4")]
    [InlineData("https://storage.example.com/footage/clip.avi")]
    [InlineData("https://storage.example.com/footage/clip.mov")]
    [InlineData("https://storage.example.com/audio/recording.wav")]
    [InlineData("https://storage.example.com/audio/recording.mp3")]
    [InlineData("https://storage.example.com/images/photo.jpg")]
    [InlineData("https://storage.example.com/images/photo.png")]
    public void MediaUrl_ValidExtension_Passes(string url)
    {
        var request = CreateValidRequest() with { MediaUrl = url };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.MediaUrl);
    }

    [Fact]
    public void MediaUrl_UnrecognizedExtension_Fails()
    {
        var request = CreateValidRequest() with
        {
            MediaUrl = "https://storage.example.com/footage/file.xyz"
        };
        var result = _validator.TestValidate(request);
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("recognized media file format"));
    }

    // === Accuracy (file hash) ===

    [Fact]
    public void FileHash_Valid64CharHex_Passes()
    {
        var request = CreateValidRequest() with
        {
            FileHashSha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
        };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.FileHashSha256);
    }

    [Fact]
    public void FileHash_InvalidFormat_Fails()
    {
        var request = CreateValidRequest() with { FileHashSha256 = "not-a-valid-hash" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FileHashSha256);
    }

    [Fact]
    public void FileHash_Null_Passes()
    {
        var request = CreateValidRequest() with { FileHashSha256 = null };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.FileHashSha256);
    }

    // === MediaType ===

    [Theory]
    [InlineData(MediaType.Video)]
    [InlineData(MediaType.Audio)]
    [InlineData(MediaType.Image)]
    public void MediaType_ValidValues_Pass(MediaType mediaType)
    {
        var request = CreateValidRequest() with { MediaType = mediaType };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.MediaType);
    }

    [Fact]
    public void MediaType_InvalidEnum_Fails()
    {
        var request = CreateValidRequest() with { MediaType = (MediaType)99 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MediaType);
    }
}
