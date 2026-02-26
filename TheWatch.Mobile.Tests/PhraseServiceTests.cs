using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

// PhraseService tests — test the phrase matching logic in isolation.
// Note: PhraseService uses MAUI Preferences internally, so we test the algorithm
// via a standalone Levenshtein implementation extracted for testability.

public class PhraseServiceTests
{
    [Theory]
    [InlineData("help me", "help me", 1.0)]
    [InlineData("help me", "help me please", 0.5)] // substring present
    [InlineData("emergency", "emergency", 1.0)]
    [InlineData("abc", "xyz", 0.0)]
    public void CalculateSimilarity_ReturnsExpectedValues(string a, string b, double minExpected)
    {
        var similarity = CalculateSimilarity(a, b);
        similarity.Should().BeGreaterThanOrEqualTo(minExpected);
    }

    [Theory]
    [InlineData("help me please someone", "help me", true)]
    [InlineData("I need emergency help", "emergency", true)]
    [InlineData("the weather is nice today", "help me", false)]
    [InlineData("call for help right now", "call for help", true)]
    public void SubstringMatch_DetectsPhrasesInTranscript(string transcript, string phrase, bool shouldMatch)
    {
        var input = transcript.ToLowerInvariant();
        var target = phrase.ToLowerInvariant();
        var contains = input.Contains(target);
        contains.Should().Be(shouldMatch);
    }

    [Fact]
    public void LevenshteinDistance_EmptyStrings_ReturnsZero()
    {
        LevenshteinDistance("", "").Should().Be(0);
    }

    [Fact]
    public void LevenshteinDistance_OneEmpty_ReturnsOtherLength()
    {
        LevenshteinDistance("hello", "").Should().Be(5);
        LevenshteinDistance("", "world").Should().Be(5);
    }

    [Fact]
    public void LevenshteinDistance_IdenticalStrings_ReturnsZero()
    {
        LevenshteinDistance("emergency", "emergency").Should().Be(0);
    }

    [Fact]
    public void LevenshteinDistance_SingleCharDifference_ReturnsOne()
    {
        LevenshteinDistance("help", "helps").Should().Be(1);
        LevenshteinDistance("help", "heap").Should().BeLessThanOrEqualTo(2);
    }

    [Theory]
    [InlineData("help me", "halp me", 0.7, true)]   // 1 char diff in 7-char string = ~0.86 similarity
    [InlineData("emergency", "emergancy", 0.7, true)] // 1 char diff in 9-char string = ~0.89 similarity
    [InlineData("help", "abcdefgh", 0.7, false)]      // Very different
    public void FuzzyMatch_WithThreshold_WorksCorrectly(string a, string b, double threshold, bool shouldMatch)
    {
        var similarity = CalculateSimilarity(a, b);
        (similarity >= threshold).Should().Be(shouldMatch,
            $"similarity was {similarity:F3}, threshold {threshold}");
    }

    // Standalone implementations for testing (mirrors PhraseService logic)
    private static double CalculateSimilarity(string a, string b)
    {
        if (a == b) return 1.0;
        if (a.Length == 0 || b.Length == 0) return 0.0;
        int maxLen = Math.Max(a.Length, b.Length);
        int distance = LevenshteinDistance(a, b);
        return 1.0 - (double)distance / maxLen;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int[,] d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[a.Length, b.Length];
    }
}
