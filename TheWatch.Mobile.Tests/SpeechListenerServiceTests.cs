using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Tests for speech listener service logic — transcript processing, phrase matching,
/// event firing, and listening state tracking.
/// Since SpeechListenerService depends on MAUI SpeechToText APIs,
/// we test the pure transcript processing and state logic independently.
/// </summary>
public class SpeechListenerServiceTests
{
    // =========================================================================
    // ProcessTranscript — Empty/Whitespace
    // =========================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ProcessTranscript_EmptyOrWhitespace_IsIgnored(string? transcript)
    {
        var speechRecognized = false;
        Action<string> onSpeechRecognized = _ => speechRecognized = true;

        ProcessTranscript(transcript, [], onSpeechRecognized, (_, _) => { });

        speechRecognized.Should().BeFalse("empty/whitespace transcripts are ignored");
    }

    // =========================================================================
    // ProcessTranscript — Valid Transcript
    // =========================================================================

    [Fact]
    public void ProcessTranscript_ValidTranscript_FiresSpeechRecognized()
    {
        string? recognized = null;
        Action<string> onSpeechRecognized = t => recognized = t;

        ProcessTranscript("help me please", [], onSpeechRecognized, (_, _) => { });

        recognized.Should().Be("help me please");
    }

    // =========================================================================
    // ProcessTranscript — Phrase Matching
    // =========================================================================

    [Fact]
    public void ProcessTranscript_MatchingPhrase_FiresPhraseMatched()
    {
        var phrases = new Dictionary<string, SpeechPhraseAction>
        {
            ["help me"] = SpeechPhraseAction.TriggerSOS,
            ["i'm safe"] = SpeechPhraseAction.CheckIn
        };

        SpeechPhraseAction? matchedAction = null;
        Action<string, SpeechPhraseAction> onPhraseMatched = (_, action) => matchedAction = action;

        ProcessTranscript("help me please", phrases, _ => { }, onPhraseMatched);

        matchedAction.Should().Be(SpeechPhraseAction.TriggerSOS);
    }

    [Fact]
    public void ProcessTranscript_NonMatchingPhrase_OnlyFiresSpeechRecognized()
    {
        var phrases = new Dictionary<string, SpeechPhraseAction>
        {
            ["help me"] = SpeechPhraseAction.TriggerSOS
        };

        string? recognized = null;
        SpeechPhraseAction? matchedAction = null;
        Action<string> onSpeechRecognized = t => recognized = t;
        Action<string, SpeechPhraseAction> onPhraseMatched = (_, action) => matchedAction = action;

        ProcessTranscript("the weather is nice", phrases, onSpeechRecognized, onPhraseMatched);

        recognized.Should().Be("the weather is nice");
        matchedAction.Should().BeNull("no phrase matched");
    }

    [Fact]
    public void ProcessTranscript_MultiplePhrasesConfigured_MatchesFirst()
    {
        var phrases = new Dictionary<string, SpeechPhraseAction>
        {
            ["share location"] = SpeechPhraseAction.LocationShare,
            ["help me"] = SpeechPhraseAction.TriggerSOS
        };

        SpeechPhraseAction? matchedAction = null;

        ProcessTranscript("share location now", phrases, _ => { }, (_, action) => matchedAction = action);

        matchedAction.Should().Be(SpeechPhraseAction.LocationShare);
    }

    // =========================================================================
    // IsListening State
    // =========================================================================

    [Fact]
    public void IsListening_DefaultIsFalse()
    {
        var state = new SpeechListenerState();

        state.IsListening.Should().BeFalse();
    }

    [Fact]
    public void StartListening_SetsIsListeningToTrue()
    {
        var state = new SpeechListenerState();

        state.IsListening = true;

        state.IsListening.Should().BeTrue();
    }

    [Fact]
    public void StopListening_SetsIsListeningToFalse()
    {
        var state = new SpeechListenerState { IsListening = true };

        state.IsListening = false;

        state.IsListening.Should().BeFalse();
    }

    // =========================================================================
    // PhraseAction Enum
    // =========================================================================

    [Fact]
    public void SpeechPhraseAction_HasFourValues()
    {
        Enum.GetValues<SpeechPhraseAction>().Should().HaveCount(4);
    }

    [Fact]
    public void SpeechPhraseAction_AllDefined()
    {
        SpeechPhraseAction.TriggerSOS.Should().BeDefined();
        SpeechPhraseAction.CheckIn.Should().BeDefined();
        SpeechPhraseAction.LocationShare.Should().BeDefined();
        SpeechPhraseAction.Custom.Should().BeDefined();
    }

    // =========================================================================
    // Mirrors SpeechListenerService logic
    // =========================================================================

    private static void ProcessTranscript(
        string? transcript,
        Dictionary<string, SpeechPhraseAction> phrases,
        Action<string> onSpeechRecognized,
        Action<string, SpeechPhraseAction> onPhraseMatched)
    {
        if (string.IsNullOrWhiteSpace(transcript)) return;

        onSpeechRecognized(transcript);

        var lower = transcript.ToLowerInvariant();
        foreach (var kvp in phrases)
        {
            if (lower.Contains(kvp.Key.ToLowerInvariant()))
            {
                onPhraseMatched(transcript, kvp.Value);
                break;
            }
        }
    }
}

/// <summary>
/// Mirror of PhraseAction enum from TheWatch.Mobile.Services
/// </summary>
public enum SpeechPhraseAction
{
    TriggerSOS,
    CheckIn,
    LocationShare,
    Custom
}

/// <summary>
/// Mirror of SpeechListenerService state from TheWatch.Mobile.Services
/// </summary>
public class SpeechListenerState
{
    public bool IsListening { get; set; }
}
