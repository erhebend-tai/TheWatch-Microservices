using Microsoft.Extensions.Logging;
using TheWatch.Mobile.Data;
using TheWatch.Shared.Contracts.Mobile;

namespace TheWatch.Mobile.Services;

/// <summary>
/// Result of an ambulance pre-arrival triage assessment.
/// </summary>
public record TriageResult(
    string RawInput,
    string InputMethod,
    string? MatchedEntryName,
    string Guidance,
    string UrgencyLevel,
    Guid LogId);

/// <summary>
/// Pre-arrival triage service for ambulance callers.
/// Accepts symptom descriptions via typed text or speech-to-text (STT),
/// queries the on-device medical reference database (including poison entries),
/// logs the triage locally, and optionally syncs to the P2 backend.
///
/// The on-device database is available offline so callers can receive guidance
/// even without network connectivity.
/// </summary>
public interface IAmbulanceTriageService
{
    /// <summary>
    /// Assess symptoms entered as free text (typed by the user).
    /// Returns first-aid guidance from the on-device database and logs the intake.
    /// </summary>
    Task<TriageResult> AssessTextAsync(string symptoms, Guid reporterId, Guid? incidentId = null);

    /// <summary>
    /// Assess symptoms captured via speech-to-text.
    /// Identical to AssessTextAsync but marks InputMethod as "stt".
    /// </summary>
    Task<TriageResult> AssessSttAsync(string transcript, Guid reporterId, Guid? incidentId = null);

    /// <summary>
    /// Speak the guidance text aloud using platform TTS so injured users
    /// can hear it without looking at the screen.
    /// </summary>
    Task SpeakGuidanceAsync(string guidance, CancellationToken ct = default);

    /// <summary>
    /// Seed the on-device medical reference database on first run.
    /// Safe to call every startup — skips if already seeded.
    /// </summary>
    Task EnsureSeededAsync();
}

public class AmbulanceTriageService : IAmbulanceTriageService
{
    private readonly WatchLocalDbContext _db;
    private readonly WatchApiClient _api;
    private readonly ILogger<AmbulanceTriageService> _logger;

    public AmbulanceTriageService(
        WatchLocalDbContext db,
        WatchApiClient api,
        ILogger<AmbulanceTriageService> logger)
    {
        _db = db;
        _api = api;
        _logger = logger;
    }

    // ── Core Assessment ──────────────────────────────────────────────────────

    public Task<TriageResult> AssessTextAsync(string symptoms, Guid reporterId, Guid? incidentId = null)
        => AssessAsync(symptoms, "text", reporterId, incidentId);

    public Task<TriageResult> AssessSttAsync(string transcript, Guid reporterId, Guid? incidentId = null)
        => AssessAsync(transcript, "stt", reporterId, incidentId);

    private async Task<TriageResult> AssessAsync(
        string symptoms,
        string inputMethod,
        Guid reporterId,
        Guid? incidentId)
    {
        // 1. Extract keywords from the symptom text and search the on-device DB
        var (entry, matchedKeyword) = await FindBestMatchAsync(symptoms);

        var guidance = entry?.FirstAidGuidance
            ?? "Keep the person calm and still. Do not give food or water. Help is on the way.";
        var urgencyLevel = entry?.UrgencyLevel ?? "Unknown";
        var matchedName = entry?.Name;
        var severity = UrgencyToSeverity(urgencyLevel);

        // 2. Log locally (works offline)
        var log = new LocalTriageLog
        {
            ReporterId = reporterId,
            IncidentId = incidentId,
            Symptoms = symptoms,
            InputMethod = inputMethod,
            MatchedEntryName = matchedName,
            Guidance = guidance,
            SyncedToServer = false
        };
        await _db.SaveTriageLogAsync(log);

        _logger.LogInformation(
            "Triage assessed. LogId={LogId} InputMethod={InputMethod} Match={Match} Urgency={Urgency}",
            log.Id, inputMethod, matchedName ?? "none", urgencyLevel);

        // 3. Sync to P2 backend (best-effort — non-blocking)
        _ = SyncIntakeToBackendAsync(log, entry, severity)
            .ContinueWith(t =>
            {
                if (t.Exception is not null)
                    _logger.LogWarning(t.Exception, "Triage sync to backend failed for LogId={LogId}", log.Id);
            }, TaskContinuationOptions.OnlyOnFaulted);

        return new TriageResult(symptoms, inputMethod, matchedName, guidance, urgencyLevel, log.Id);
    }

    // ── Matching Logic ───────────────────────────────────────────────────────

    /// <summary>
    /// Extracts candidate tokens from <paramref name="symptoms"/> and searches the on-device
    /// medical reference table, returning the best match and the keyword that matched.
    /// </summary>
    private async Task<(MedicalReferenceEntry? entry, string? keyword)> FindBestMatchAsync(string symptoms)
    {
        if (string.IsNullOrWhiteSpace(symptoms)) return (null, null);

        // Try progressively longer n-grams (3-word, 2-word, 1-word) so more specific phrases win
        var words = symptoms
            .ToLowerInvariant()
            .Split([' ', ',', '.', '!', '?', ';', ':'], StringSplitOptions.RemoveEmptyEntries);

        var candidates = new List<string>();

        // 3-word phrases
        for (int i = 0; i <= words.Length - 3; i++)
            candidates.Add($"{words[i]} {words[i + 1]} {words[i + 2]}");
        // 2-word phrases
        for (int i = 0; i <= words.Length - 2; i++)
            candidates.Add($"{words[i]} {words[i + 1]}");
        // single words
        candidates.AddRange(words);

        foreach (var candidate in candidates)
        {
            // Skip very short or common stop-words to reduce false positives
            if (candidate.Length < 3 || StopWords.Contains(candidate)) continue;

            var matches = await _db.SearchMedicalReferenceAsync(candidate, limit: 1);
            if (matches.Count > 0)
                return (matches[0], candidate);
        }

        return (null, null);
    }

    private static readonly HashSet<string> StopWords =
    [
        "the", "and", "but", "for", "not", "are", "was", "had", "has",
        "have", "him", "her", "his", "its", "they", "them", "their",
        "this", "that", "with", "from", "who", "what", "when", "where",
        "how", "why", "can", "just", "help", "pain", "feel", "very",
        "some", "also", "been", "more", "will", "may", "you", "your",
        "him", "man", "old", "she", "get", "got", "lot"
    ];

    private static int UrgencyToSeverity(string urgencyLevel) => urgencyLevel switch
    {
        "Critical" => 5,
        "High" => 4,
        "Medium" => 3,
        "Low" => 1,
        _ => 0
    };

    // ── TTS Output ───────────────────────────────────────────────────────────

    public async Task SpeakGuidanceAsync(string guidance, CancellationToken ct = default)
    {
        try
        {
            await TextToSpeech.SpeakAsync(guidance, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TTS guidance speech failed");
        }
    }

    // ── Sync to Backend ──────────────────────────────────────────────────────

    private async Task SyncIntakeToBackendAsync(LocalTriageLog log, MedicalReferenceEntry? entry, int severity)
    {
        var request = new LogTriageIntakeRequest(
            ReporterId: log.ReporterId,
            Symptoms: log.Symptoms,
            InputMethod: log.InputMethod,
            IncidentId: log.IncidentId,
            SubstanceName: entry?.Name,
            MatchedGuidance: log.Guidance,
            TriageSeverity: severity);

        await _api.LogTriageIntakeAsync(request);
        await _db.MarkTriageLogSyncedAsync(log.Id);
    }

    // ── Seed ─────────────────────────────────────────────────────────────────

    public async Task EnsureSeededAsync()
    {
        await _db.SeedMedicalReferenceAsync(MedicalReferenceSeedData.Entries);
    }
}

/// <summary>
/// Bundled medical reference seed data for on-device offline use.
/// Covers common poisoning agents (household, pharmaceutical, chemical) and
/// life-threatening medical emergencies.
///
/// Source guidance is based on standard first-aid / Poison Control recommendations
/// and is intended as dispatcher-assist only — not a substitute for clinical advice.
/// Poison Control hotline: 1-800-222-1222 (US).
/// </summary>
public static class MedicalReferenceSeedData
{
    private static Guid G(string s) => Guid.Parse(s);

    public static readonly List<MedicalReferenceEntry> Entries =
    [
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000001"),
            Name = "Acetaminophen",
            Category = "Poison",
            Keywords = "tylenol,acetaminophen,paracetamol,overdose,pills",
            Symptoms = "Nausea, vomiting, abdominal pain, jaundice (in later stages)",
            FirstAidGuidance = "Call Poison Control immediately (1-800-222-1222). Do NOT induce vomiting. If conscious, keep calm. N-acetylcysteine (NAC) is the antidote — requires hospital treatment urgently.",
            UrgencyLevel = "Critical",
            PoisonControlNote = "Call Poison Control: 1-800-222-1222"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000002"),
            Name = "Aspirin",
            Category = "Poison",
            Keywords = "aspirin,salicylate,salicylic acid,overdose",
            Symptoms = "Ringing in ears, rapid breathing, vomiting, confusion, high fever",
            FirstAidGuidance = "Call Poison Control (1-800-222-1222). Do NOT induce vomiting. Keep person still and calm. Activated charcoal may be used by medical staff. Transport to hospital immediately.",
            UrgencyLevel = "Critical",
            PoisonControlNote = "Call Poison Control: 1-800-222-1222"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000003"),
            Name = "Carbon Monoxide",
            Category = "Chemical",
            Keywords = "carbon monoxide,CO,gas leak,exhaust,fumes,smoke inhalation",
            Symptoms = "Headache, dizziness, weakness, nausea, confusion, cherry-red skin",
            FirstAidGuidance = "Move person to fresh air immediately. Do NOT re-enter the building. Call 911. Administer 100% oxygen if available. CPR if not breathing. Do not leave person alone.",
            UrgencyLevel = "Critical"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000004"),
            Name = "Bleach",
            Category = "Poison",
            Keywords = "bleach,sodium hypochlorite,chlorine,cleaning product,household chemical",
            Symptoms = "Burning mouth/throat, coughing, vomiting, difficulty breathing",
            FirstAidGuidance = "If ingested: rinse mouth with water, drink 1-2 glasses of water or milk. Do NOT induce vomiting. If inhaled: move to fresh air. Call Poison Control (1-800-222-1222) or 911.",
            UrgencyLevel = "High",
            PoisonControlNote = "Call Poison Control: 1-800-222-1222"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000005"),
            Name = "Opioid Overdose",
            Category = "Poison",
            Keywords = "opioid,heroin,fentanyl,morphine,oxycodone,overdose,narcotics,opiate",
            Symptoms = "Pinpoint pupils, unconscious, slow or stopped breathing, blue lips/fingertips, gurgling sounds",
            FirstAidGuidance = "Call 911 immediately. Administer naloxone (Narcan) if available — repeat every 2-3 minutes if no response. Place in recovery position. Perform rescue breathing if not breathing. Stay until EMS arrives.",
            UrgencyLevel = "Critical"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000006"),
            Name = "Methanol",
            Category = "Poison",
            Keywords = "methanol,methyl alcohol,wood alcohol,antifreeze ingested,windshield washer fluid",
            Symptoms = "Visual disturbances, headache, dizziness, nausea, acidosis (12-24 hrs after ingestion)",
            FirstAidGuidance = "Call Poison Control (1-800-222-1222) or 911 immediately. Do NOT induce vomiting. Hospital treatment with fomepizole or ethanol antidote is required. Time-critical — act immediately.",
            UrgencyLevel = "Critical",
            PoisonControlNote = "Call Poison Control: 1-800-222-1222"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000007"),
            Name = "Benzodiazepine",
            Category = "Poison",
            Keywords = "benzodiazepine,xanax,valium,ativan,diazepam,alprazolam,lorazepam,overdose,benzo",
            Symptoms = "Drowsiness, confusion, slurred speech, impaired coordination, slow breathing",
            FirstAidGuidance = "Call 911. Keep person awake if possible. Do NOT induce vomiting. Place in recovery position if unconscious. Flumazenil reversal available in hospital only.",
            UrgencyLevel = "High"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000008"),
            Name = "Battery Acid",
            Category = "Chemical",
            Keywords = "battery acid,sulfuric acid,acid burn,corrosive,chemical burn",
            Symptoms = "Severe burns to mouth/skin, pain, difficulty swallowing",
            FirstAidGuidance = "Do NOT induce vomiting. Rinse affected skin/eyes with large amounts of water for 20+ minutes. If ingested: rinse mouth, drink small amounts of water only. Call 911 and Poison Control immediately.",
            UrgencyLevel = "Critical",
            PoisonControlNote = "Call Poison Control: 1-800-222-1222"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000009"),
            Name = "Pesticide",
            Category = "Poison",
            Keywords = "pesticide,insecticide,organophosphate,rat poison,rodenticide,herbicide,weed killer,bug spray",
            Symptoms = "Excessive salivation, sweating, nausea, vomiting, muscle twitching, seizures, pinpoint pupils",
            FirstAidGuidance = "Call 911 and Poison Control (1-800-222-1222). Remove contaminated clothing. Flush skin/eyes with water. Do NOT induce vomiting. Atropine antidote — hospital required. Bring product label if possible.",
            UrgencyLevel = "Critical",
            PoisonControlNote = "Call Poison Control: 1-800-222-1222"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000010"),
            Name = "Alcohol Poisoning",
            Category = "Poison",
            Keywords = "alcohol,ethanol,drunk,intoxication,too much alcohol,spirits,vodka,whiskey",
            Symptoms = "Confusion, vomiting, seizures, slow breathing, pale/blue skin, unconscious",
            FirstAidGuidance = "Call 911 immediately if unconscious or not breathing. Turn person on their side (recovery position) to prevent choking on vomit. Keep warm. Do not give coffee or water. Do not leave alone.",
            UrgencyLevel = "Critical"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000011"),
            Name = "Cardiac Arrest",
            Category = "Medical",
            Keywords = "cardiac arrest,heart attack,heart stopped,not breathing,no pulse,CPR,chest compressions,collapse",
            Symptoms = "Sudden collapse, no pulse, no breathing, unconscious",
            FirstAidGuidance = "Call 911 immediately. Begin CPR: 30 chest compressions (hard and fast) then 2 rescue breaths. Use AED if available. Do not stop until EMS arrives or person shows signs of life.",
            UrgencyLevel = "Critical"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000012"),
            Name = "Stroke",
            Category = "Medical",
            Keywords = "stroke,brain attack,face drooping,arm weakness,speech difficulty,sudden headache,FAST",
            Symptoms = "Face drooping, arm weakness, speech difficulty, sudden severe headache, confusion, vision loss",
            FirstAidGuidance = "Call 911 immediately — time is critical (clot-busting treatment within 4.5 hours). FAST: Face drooping, Arm weakness, Speech difficulty, Time to call 911. Note the exact time symptoms started. Keep person calm and still.",
            UrgencyLevel = "Critical"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000013"),
            Name = "Anaphylaxis",
            Category = "Medical",
            Keywords = "anaphylaxis,allergic reaction,epinephrine,epipen,bee sting,peanut allergy,severe allergy,throat swelling,hives",
            Symptoms = "Throat swelling, hives, breathing difficulty, drop in blood pressure, rapid pulse, dizziness",
            FirstAidGuidance = "Use epinephrine auto-injector (EpiPen) immediately if available. Call 911. Lie person down with legs elevated (unless breathing difficulty — then sit up). Be ready for second reaction — further medical treatment required.",
            UrgencyLevel = "Critical"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000014"),
            Name = "Choking",
            Category = "Medical",
            Keywords = "choking,heimlich,airway obstruction,can not breathe,food stuck,swallowed object,blockage",
            Symptoms = "Cannot speak/breathe/cough, blue lips, clutching throat",
            FirstAidGuidance = "Perform Heimlich maneuver: stand behind person, wrap arms around waist, give 5 upward abdominal thrusts. For infants: 5 back blows + 5 chest thrusts. If unconscious, begin CPR and look for object before giving breaths. Call 911.",
            UrgencyLevel = "Critical"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000015"),
            Name = "Diabetic Emergency",
            Category = "Medical",
            Keywords = "diabetic,diabetes,low blood sugar,hypoglycemia,insulin,blood sugar,glucose,diabetic shock",
            Symptoms = "Shakiness, sweating, confusion, pale skin, rapid heartbeat, unconsciousness",
            FirstAidGuidance = "If conscious and able to swallow: give sugar (juice, glucose tablets, regular soda). If unconscious: do NOT give anything by mouth — call 911 immediately. Check for medical ID bracelet. Reassess in 15 minutes.",
            UrgencyLevel = "High"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000016"),
            Name = "Chlorine Gas",
            Category = "Chemical",
            Keywords = "chlorine,chlorine gas,pool chemical,industrial gas,chemical exposure,chemical spill",
            Symptoms = "Eye/nose/throat burning, coughing, chest tightness, difficulty breathing",
            FirstAidGuidance = "Move to fresh air immediately. Remove contaminated clothing. Flush eyes and skin with water for 15+ minutes. Do NOT induce vomiting if swallowed. Call 911 and Poison Control (1-800-222-1222) immediately.",
            UrgencyLevel = "Critical",
            PoisonControlNote = "Call Poison Control: 1-800-222-1222"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000017"),
            Name = "Severe Bleeding",
            Category = "Medical",
            Keywords = "bleeding,blood loss,hemorrhage,cut,wound,laceration,trauma,arterial bleed",
            Symptoms = "Rapid blood loss, pale/clammy skin, rapid pulse, dizziness, loss of consciousness",
            FirstAidGuidance = "Apply firm direct pressure with clean cloth. Do not remove cloth — add more on top. Elevate injured limb above heart level. Apply tourniquet 2-3 inches above wound if bleeding is life-threatening and uncontrolled. Call 911.",
            UrgencyLevel = "Critical"
        },
        new MedicalReferenceEntry
        {
            Id = G("a0000000-0000-0000-0000-000000000018"),
            Name = "Seizure",
            Category = "Medical",
            Keywords = "seizure,epilepsy,convulsion,fit,jerking,twitching,loss of consciousness",
            Symptoms = "Uncontrolled jerking movements, loss of consciousness, stiffening, confusion after",
            FirstAidGuidance = "Do NOT hold person down or put anything in mouth. Clear area of hazards. Time the seizure. Turn on side after jerking stops. Call 911 if: first seizure, lasts 5+ minutes, person does not regain consciousness, or injured.",
            UrgencyLevel = "High"
        }
    ];
}
