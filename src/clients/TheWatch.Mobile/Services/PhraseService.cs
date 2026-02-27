using System.Text.Json;

namespace TheWatch.Mobile.Services;

public enum PhraseAction
{
    EmergencySOS,
    CheckIn,
    SilentAlert
}

public record ActivationPhrase(string Text, PhraseAction Action);

public class PhraseService
{
    private const string StorageKey = "watch_activation_phrases";
    private const string ThresholdKey = "watch_phrase_threshold";

    private List<ActivationPhrase> _phrases;
    private double _threshold;

    public double Threshold
    {
        get => _threshold;
        set
        {
            _threshold = Math.Clamp(value, 0.0, 1.0);
            Preferences.Set(ThresholdKey, _threshold);
        }
    }

    public PhraseService()
    {
        _threshold = Preferences.Get(ThresholdKey, 0.7);
        _phrases = LoadPhrases();

        if (_phrases.Count == 0)
        {
            // Seed defaults
            _phrases =
            [
                new("Help me", PhraseAction.EmergencySOS),
                new("Call for help", PhraseAction.EmergencySOS),
                new("Emergency", PhraseAction.EmergencySOS),
                new("I need help", PhraseAction.EmergencySOS),
                new("I'm safe", PhraseAction.CheckIn),
                new("Check in", PhraseAction.CheckIn),
            ];
            SavePhrases();
        }
    }

    public IReadOnlyList<ActivationPhrase> GetPhrases() => _phrases.AsReadOnly();

    public void AddPhrase(string text, PhraseAction action)
    {
        if (_phrases.Any(p => p.Text.Equals(text, StringComparison.OrdinalIgnoreCase)))
            return;
        _phrases.Add(new ActivationPhrase(text, action));
        SavePhrases();
    }

    public void RemovePhrase(string text)
    {
        _phrases.RemoveAll(p => p.Text.Equals(text, StringComparison.OrdinalIgnoreCase));
        SavePhrases();
    }

    public (string phrase, PhraseAction action)? MatchPhrase(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript)) return null;

        var input = transcript.ToLowerInvariant().Trim();

        // Exact substring match first
        foreach (var p in _phrases)
        {
            if (input.Contains(p.Text.ToLowerInvariant()))
                return (p.Text, p.Action);
        }

        // Fuzzy match using Levenshtein distance
        foreach (var p in _phrases)
        {
            var phraseWords = p.Text.ToLowerInvariant();
            var similarity = CalculateSimilarity(input, phraseWords);
            if (similarity >= _threshold)
                return (p.Text, p.Action);

            // Also check word-level: if the transcript contains words that fuzzy-match the phrase
            var inputWords = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var phraseWordList = phraseWords.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int matchedWords = 0;
            foreach (var pw in phraseWordList)
            {
                foreach (var iw in inputWords)
                {
                    if (CalculateSimilarity(iw, pw) >= _threshold)
                    {
                        matchedWords++;
                        break;
                    }
                }
            }

            if (phraseWordList.Length > 0 &&
                (double)matchedWords / phraseWordList.Length >= _threshold)
                return (p.Text, p.Action);
        }

        return null;
    }

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

    private List<ActivationPhrase> LoadPhrases()
    {
        var json = Preferences.Get(StorageKey, "");
        if (string.IsNullOrEmpty(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<ActivationPhrase>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void SavePhrases()
    {
        var json = JsonSerializer.Serialize(_phrases);
        Preferences.Set(StorageKey, json);
    }
}
