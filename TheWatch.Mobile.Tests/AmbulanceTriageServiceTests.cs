using FluentAssertions;
using Xunit;

namespace TheWatch.Mobile.Tests;

/// <summary>
/// Unit tests for the ambulance pre-arrival triage service logic.
/// Mirrors the pure assessment/matching algorithms from AmbulanceTriageService
/// without MAUI or SQLite dependencies.
/// </summary>
public class AmbulanceTriageServiceTests
{
    // =========================================================================
    // Keyword Extraction + Matching Logic
    // =========================================================================

    [Theory]
    [InlineData("I took too many tylenol pills", "Acetaminophen")]
    [InlineData("patient swallowed acetaminophen", "Acetaminophen")]
    [InlineData("smells like chlorine in the building", "Chlorine Gas")]
    [InlineData("carbon monoxide leak in apartment", "Carbon Monoxide")]
    [InlineData("opioid overdose, person not responding", "Opioid Overdose")]
    [InlineData("heroin overdose pinpoint pupils", "Opioid Overdose")]
    [InlineData("severe allergic reaction, throat swelling", "Anaphylaxis")]
    [InlineData("heart stopped not breathing", "Cardiac Arrest")]
    [InlineData("face drooping arm weakness speech slurred", "Stroke")]
    [InlineData("person choking on food", "Choking")]
    [InlineData("pesticide ingested, excessive salivation", "Pesticide")]
    [InlineData("methanol ingestion, visual disturbances", "Methanol")]
    [InlineData("diabetic emergency low blood sugar shaking", "Diabetic Emergency")]
    [InlineData("severe bleeding from wound", "Severe Bleeding")]
    [InlineData("seizure convulsions jerking", "Seizure")]
    [InlineData("alcohol poisoning unconscious", "Alcohol Poisoning")]
    [InlineData("bleach swallowed burning throat", "Bleach")]
    [InlineData("benzodiazepine xanax overdose slurred", "Benzodiazepine")]
    public void FindBestMatch_KnownKeyword_ReturnsCorrectEntry(string input, string expectedName)
    {
        var entries = GetSeedEntries();

        var match = FindBestMatch(input, entries);

        match.Should().NotBeNull($"'{input}' should match '{expectedName}'");
        match!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FindBestMatch_EmptyInput_ReturnsNull()
    {
        var entries = GetSeedEntries();

        var match = FindBestMatch("", entries);

        match.Should().BeNull();
    }

    [Fact]
    public void FindBestMatch_WhitespaceOnly_ReturnsNull()
    {
        var entries = GetSeedEntries();

        var match = FindBestMatch("   ", entries);

        match.Should().BeNull();
    }

    [Fact]
    public void FindBestMatch_UnrecognizedText_ReturnsNull()
    {
        var entries = GetSeedEntries();

        var match = FindBestMatch("the sky is blue and the weather is nice today", entries);

        match.Should().BeNull();
    }

    [Fact]
    public void FindBestMatch_StopWordOnlyInput_ReturnsNull()
    {
        var entries = GetSeedEntries();

        // "help" and "pain" are stop words in the matcher
        var match = FindBestMatch("help pain the and but", entries);

        match.Should().BeNull();
    }

    // =========================================================================
    // UrgencyToSeverity Mapping
    // =========================================================================

    [Theory]
    [InlineData("Critical", 5)]
    [InlineData("High", 4)]
    [InlineData("Medium", 3)]
    [InlineData("Low", 1)]
    [InlineData("Unknown", 0)]
    [InlineData("", 0)]
    public void UrgencyToSeverity_ReturnsCorrectMapping(string urgency, int expectedSeverity)
    {
        var severity = UrgencyToSeverity(urgency);

        severity.Should().Be(expectedSeverity);
    }

    // =========================================================================
    // TriageResult shape
    // =========================================================================

    [Fact]
    public void TriageResult_MatchedEntry_HasGuidanceFromEntry()
    {
        var entries = GetSeedEntries();
        var match = FindBestMatch("carbon monoxide exposure headache dizziness", entries);

        match.Should().NotBeNull();
        match!.FirstAidGuidance.Should().NotBeEmpty();
        match.UrgencyLevel.Should().Be("Critical");
    }

    [Fact]
    public void TriageResult_AllEntries_HaveNonEmptyGuidance()
    {
        foreach (var entry in GetSeedEntries())
        {
            entry.FirstAidGuidance.Should().NotBeEmpty(
                $"Entry '{entry.Name}' must have first-aid guidance");
        }
    }

    [Fact]
    public void TriageResult_AllEntries_HaveValidUrgencyLevel()
    {
        var validLevels = new[] { "Critical", "High", "Medium", "Low" };
        foreach (var entry in GetSeedEntries())
        {
            entry.UrgencyLevel.Should().BeOneOf(validLevels,
                $"Entry '{entry.Name}' must have a valid urgency level");
        }
    }

    // =========================================================================
    // Seed data integrity
    // =========================================================================

    [Fact]
    public void SeedData_AllEntries_HaveUniqueIds()
    {
        var ids = GetSeedEntries().Select(e => e.Id).ToList();
        ids.Distinct().Should().HaveCount(ids.Count, "all seed entry IDs must be unique");
    }

    [Fact]
    public void SeedData_AllEntries_HaveNonEmptyKeywords()
    {
        foreach (var entry in GetSeedEntries())
        {
            entry.Keywords.Should().NotBeEmpty($"Entry '{entry.Name}' must have keywords");
        }
    }

    [Fact]
    public void SeedData_CountIs18()
    {
        GetSeedEntries().Should().HaveCount(18);
    }

    [Fact]
    public void SeedData_ContainsPoisonEntries()
    {
        var entries = GetSeedEntries();
        entries.Where(e => e.Category == "Poison").Should().NotBeEmpty();
    }

    [Fact]
    public void SeedData_ContainsChemicalEntries()
    {
        var entries = GetSeedEntries();
        entries.Where(e => e.Category == "Chemical").Should().NotBeEmpty();
    }

    [Fact]
    public void SeedData_ContainsMedicalEntries()
    {
        var entries = GetSeedEntries();
        entries.Where(e => e.Category == "Medical").Should().NotBeEmpty();
    }

    // =========================================================================
    // LocalTriageLog model
    // =========================================================================

    [Fact]
    public void LocalTriageLog_Defaults_SyncedFalse()
    {
        var log = new LocalTriageLog();

        log.SyncedToServer.Should().BeFalse();
        log.InputMethod.Should().Be("text");
    }

    [Fact]
    public void LocalTriageLog_CanSetAllFields()
    {
        var now = DateTime.UtcNow;
        var log = new LocalTriageLog
        {
            Id = Guid.NewGuid(),
            ReporterId = Guid.NewGuid(),
            IncidentId = Guid.NewGuid(),
            Symptoms = "chest pain",
            InputMethod = "stt",
            MatchedEntryName = "Cardiac Arrest",
            Guidance = "Call 911. Start CPR.",
            SyncedToServer = true,
            CreatedAt = now
        };

        log.Symptoms.Should().Be("chest pain");
        log.InputMethod.Should().Be("stt");
        log.MatchedEntryName.Should().Be("Cardiac Arrest");
        log.SyncedToServer.Should().BeTrue();
    }

    // =========================================================================
    // Mirrors of AmbulanceTriageService logic (no MAUI dependencies)
    // =========================================================================

    private static MedicalReferenceEntry? FindBestMatch(string symptoms, List<MedicalReferenceEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(symptoms)) return null;

        var words = symptoms
            .ToLowerInvariant()
            .Split([' ', ',', '.', '!', '?', ';', ':'], StringSplitOptions.RemoveEmptyEntries);

        var candidates = new List<string>();
        for (int i = 0; i <= words.Length - 3; i++)
            candidates.Add($"{words[i]} {words[i + 1]} {words[i + 2]}");
        for (int i = 0; i <= words.Length - 2; i++)
            candidates.Add($"{words[i]} {words[i + 1]}");
        candidates.AddRange(words);

        foreach (var candidate in candidates)
        {
            if (candidate.Length < 3 || StopWords.Contains(candidate)) continue;
            var lower = candidate.ToLowerInvariant();
            var match = entries.FirstOrDefault(e =>
                e.Name.ToLowerInvariant().Contains(lower) ||
                e.Keywords.ToLowerInvariant().Contains(lower));
            if (match is not null) return match;
        }

        return null;
    }

    private static int UrgencyToSeverity(string urgencyLevel) => urgencyLevel switch
    {
        "Critical" => 5,
        "High" => 4,
        "Medium" => 3,
        "Low" => 1,
        _ => 0
    };

    private static readonly HashSet<string> StopWords =
    [
        "the", "and", "but", "for", "not", "are", "was", "had", "has",
        "have", "him", "her", "his", "its", "they", "them", "their",
        "this", "that", "with", "from", "who", "what", "when", "where",
        "how", "why", "can", "just", "help", "pain", "feel", "very",
        "some", "also", "been", "more", "will", "may", "you", "your",
        "him", "man", "old", "she", "get", "got", "lot"
    ];

    // ── Mirrors from AmbulanceTriageService.cs seed data ──────────────────────
    private static List<MedicalReferenceEntry> GetSeedEntries() =>
    [
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Acetaminophen",    Category = "Poison",   Keywords = "tylenol,acetaminophen,paracetamol,overdose,pills",                                           Symptoms = "Nausea, vomiting, abdominal pain, jaundice",                              FirstAidGuidance = "Call Poison Control.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Aspirin",           Category = "Poison",   Keywords = "aspirin,salicylate,salicylic acid,overdose",                                                 Symptoms = "Ringing in ears, rapid breathing, vomiting",                              FirstAidGuidance = "Call Poison Control.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Carbon Monoxide",   Category = "Chemical", Keywords = "carbon monoxide,CO,gas leak,exhaust,fumes,smoke inhalation",                                 Symptoms = "Headache, dizziness, weakness, nausea",                                   FirstAidGuidance = "Move to fresh air immediately. Call 911.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Bleach",            Category = "Poison",   Keywords = "bleach,sodium hypochlorite,chlorine,cleaning product,household chemical",                    Symptoms = "Burning mouth/throat, coughing, vomiting",                                FirstAidGuidance = "Rinse mouth with water. Call Poison Control.", UrgencyLevel = "High" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Opioid Overdose",   Category = "Poison",   Keywords = "opioid,heroin,fentanyl,morphine,oxycodone,overdose,narcotics,opiate",                        Symptoms = "Pinpoint pupils, unconscious, slow breathing",                             FirstAidGuidance = "Call 911. Give naloxone if available.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Methanol",          Category = "Poison",   Keywords = "methanol,methyl alcohol,wood alcohol,antifreeze ingested,windshield washer fluid",           Symptoms = "Visual disturbances, headache, dizziness",                                FirstAidGuidance = "Call 911 immediately.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Benzodiazepine",    Category = "Poison",   Keywords = "benzodiazepine,xanax,valium,ativan,diazepam,alprazolam,lorazepam,overdose,benzo",           Symptoms = "Drowsiness, confusion, slurred speech",                                   FirstAidGuidance = "Call 911. Recovery position.", UrgencyLevel = "High" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Battery Acid",      Category = "Chemical", Keywords = "battery acid,sulfuric acid,acid burn,corrosive,chemical burn",                               Symptoms = "Severe burns to mouth/skin, pain",                                        FirstAidGuidance = "Rinse with water 20 min. Call 911.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Pesticide",         Category = "Poison",   Keywords = "pesticide,insecticide,organophosphate,rat poison,rodenticide,herbicide,weed killer,bug spray", Symptoms = "Excessive salivation, muscle twitching, seizures",                      FirstAidGuidance = "Call 911. Remove contaminated clothing.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Alcohol Poisoning", Category = "Poison",   Keywords = "alcohol,ethanol,drunk,intoxication,too much alcohol,spirits,vodka,whiskey",                  Symptoms = "Confusion, vomiting, seizures, pale skin",                                FirstAidGuidance = "Call 911. Recovery position.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Cardiac Arrest",    Category = "Medical",  Keywords = "cardiac arrest,heart attack,heart stopped,not breathing,no pulse,CPR,chest compressions,collapse", Symptoms = "Sudden collapse, no pulse, no breathing",                           FirstAidGuidance = "Call 911. Begin CPR immediately.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Stroke",            Category = "Medical",  Keywords = "stroke,brain attack,face drooping,arm weakness,speech difficulty,sudden headache,FAST",      Symptoms = "Face drooping, arm weakness, speech difficulty",                          FirstAidGuidance = "Call 911. Note time symptoms started.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Anaphylaxis",       Category = "Medical",  Keywords = "anaphylaxis,allergic reaction,epinephrine,epipen,bee sting,peanut allergy,severe allergy,throat swelling,hives", Symptoms = "Throat swelling, hives, breathing difficulty",                 FirstAidGuidance = "Use EpiPen. Call 911.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Choking",           Category = "Medical",  Keywords = "choking,heimlich,airway obstruction,can not breathe,food stuck,swallowed object,blockage",   Symptoms = "Cannot speak/breathe/cough, blue lips",                                   FirstAidGuidance = "Perform Heimlich maneuver. Call 911.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Diabetic Emergency",Category = "Medical",  Keywords = "diabetic,diabetes,low blood sugar,hypoglycemia,insulin,blood sugar,glucose,diabetic shock",  Symptoms = "Shakiness, sweating, confusion",                                          FirstAidGuidance = "Give sugar if conscious. Call 911.", UrgencyLevel = "High" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Chlorine Gas",      Category = "Chemical", Keywords = "chlorine,chlorine gas,pool chemical,industrial gas,chemical exposure,chemical spill",         Symptoms = "Eye/nose/throat burning, chest tightness",                                FirstAidGuidance = "Move to fresh air. Call 911.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Severe Bleeding",   Category = "Medical",  Keywords = "bleeding,blood loss,hemorrhage,cut,wound,laceration,trauma,arterial bleed",                  Symptoms = "Rapid blood loss, pale skin, rapid pulse",                                FirstAidGuidance = "Apply direct pressure. Call 911.", UrgencyLevel = "Critical" },
        new MedicalReferenceEntry { Id = Guid.NewGuid(), Name = "Seizure",           Category = "Medical",  Keywords = "seizure,epilepsy,convulsion,fit,jerking,twitching,loss of consciousness",                    Symptoms = "Uncontrolled jerking, loss of consciousness",                             FirstAidGuidance = "Clear area, time the seizure, call 911 if >5 min.", UrgencyLevel = "High" }
    ];
}

// ── Mirror types ────────────────────────────────────────────────────────────────

public class MedicalReferenceEntry
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string FirstAidGuidance { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty;
    public string? PoisonControlNote { get; set; }
}

public class LocalTriageLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReporterId { get; set; }
    public Guid? IncidentId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string InputMethod { get; set; } = "text";
    public string? MatchedEntryName { get; set; }
    public string? Guidance { get; set; }
    public bool SyncedToServer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
