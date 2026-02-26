namespace TheWatch.Shared.Gcp;

/// <summary>
/// Central configuration for GCP service toggles.
/// Bind from "Gcp" section in appsettings.json.
///
/// Each toggle enables a GCP-managed service:
///   - UseSpeechToText: Google Cloud Speech-to-Text for server-side voice recognition
///   - UseVisionApi: Google Cloud Vision for evidence analysis / content moderation
///   - UseFirebaseMessaging: Firebase Cloud Messaging for push notifications (already wired, this gates it)
///   - UseHealthcareApi: Google Healthcare API (FHIR) for P7/P9 health data interoperability
///
/// When a toggle is false, a NoOp implementation is registered (safe for dev/test).
/// When true + credentials configured, the real GCP client is registered.
/// </summary>
public class GcpServiceOptions
{
    public const string SectionName = "Gcp";

    // ─── Speech-to-Text (Item 132) ───

    public bool UseSpeechToText { get; set; }

    /// <summary>
    /// Path to Google Cloud service account JSON key file.
    /// If not set, falls back to GOOGLE_APPLICATION_CREDENTIALS env var.
    /// </summary>
    public string? CredentialPath { get; set; }

    /// <summary>
    /// Speech recognition language code (e.g., "en-US").
    /// </summary>
    public string SpeechLanguageCode { get; set; } = "en-US";

    /// <summary>
    /// Enable automatic punctuation in transcription.
    /// </summary>
    public bool SpeechEnablePunctuation { get; set; } = true;

    /// <summary>
    /// Enable enhanced model for better accuracy (costs more).
    /// </summary>
    public bool SpeechUseEnhancedModel { get; set; }

    // ─── Vision API (Item 133) ───

    public bool UseVisionApi { get; set; }

    /// <summary>
    /// Confidence threshold for content moderation flags (0.0–1.0).
    /// </summary>
    public float VisionModerationThreshold { get; set; } = 0.7f;

    /// <summary>
    /// Enable SafeSearch detection for uploaded evidence.
    /// </summary>
    public bool VisionEnableSafeSearch { get; set; } = true;

    /// <summary>
    /// Enable label detection for evidence categorization.
    /// </summary>
    public bool VisionEnableLabelDetection { get; set; } = true;

    /// <summary>
    /// Enable OCR text detection in evidence images.
    /// </summary>
    public bool VisionEnableTextDetection { get; set; }

    // ─── Firebase Cloud Messaging (Item 134) ───

    public bool UseFirebaseMessaging { get; set; }

    /// <summary>
    /// Path to Firebase service account JSON or google-services.json.
    /// Already consumed by NotificationGenerator — this centralizes the config.
    /// </summary>
    public string? FirebaseCredentialPath { get; set; }

    // ─── Healthcare API / FHIR (Item 135) ───

    public bool UseHealthcareApi { get; set; }

    /// <summary>
    /// Google Cloud project ID for Healthcare API.
    /// </summary>
    public string? HealthcareProjectId { get; set; }

    /// <summary>
    /// Healthcare API location (e.g., "us-central1").
    /// </summary>
    public string HealthcareLocation { get; set; } = "us-central1";

    /// <summary>
    /// FHIR store dataset ID.
    /// </summary>
    public string? HealthcareDatasetId { get; set; }

    /// <summary>
    /// FHIR store ID within the dataset.
    /// </summary>
    public string? HealthcareFhirStoreId { get; set; }

    /// <summary>
    /// FHIR version (R4 recommended for US healthcare).
    /// </summary>
    public string FhirVersion { get; set; } = "R4";
}
