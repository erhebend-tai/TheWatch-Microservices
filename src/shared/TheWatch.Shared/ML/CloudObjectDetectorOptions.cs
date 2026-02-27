// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

namespace TheWatch.Shared.ML;

/// <summary>
/// Configuration for multi-cloud object detection with fallback priority.
/// Bind from "CloudObjectDetection" section in appsettings.json.
///
/// The ResilientObjectDetector uses these settings to determine the fallback chain:
///   1. Local ONNX (always attempted first)
///   2. Cloud providers in priority order (Azure → GCP → AWS by default)
///
/// Toggle individual providers and set credentials per environment.
/// </summary>
public class CloudObjectDetectorOptions
{
    public const string SectionName = "CloudObjectDetection";

    /// <summary>
    /// Enable multi-cloud fallback when local ONNX detection is unavailable.
    /// </summary>
    public bool EnableCloudFallback { get; set; }

    /// <summary>
    /// Provider priority order. First configured and available provider is used.
    /// Valid values: "Azure", "GoogleCloud", "AWS".
    /// </summary>
    public List<string> ProviderPriority { get; set; } = ["Azure", "GoogleCloud", "AWS"];

    // ─── Azure Computer Vision ───

    /// <summary>
    /// Enable Azure Computer Vision as a backup detection provider.
    /// </summary>
    public bool UseAzureVision { get; set; }

    /// <summary>
    /// Azure Computer Vision endpoint URL (e.g., "https://myservice.cognitiveservices.azure.com/").
    /// </summary>
    public string? AzureVisionEndpoint { get; set; }

    /// <summary>
    /// Azure Computer Vision subscription key.
    /// </summary>
    public string? AzureVisionKey { get; set; }

    // ─── Google Cloud Vision ───

    /// <summary>
    /// Enable Google Cloud Vision as a backup detection provider.
    /// </summary>
    public bool UseGoogleVision { get; set; }

    /// <summary>
    /// Path to Google Cloud service account JSON key file.
    /// Falls back to GOOGLE_APPLICATION_CREDENTIALS if not set.
    /// </summary>
    public string? GoogleCredentialPath { get; set; }

    // ─── AWS Rekognition ───

    /// <summary>
    /// Enable AWS Rekognition as a backup detection provider.
    /// </summary>
    public bool UseAwsRekognition { get; set; }

    /// <summary>
    /// AWS region for Rekognition (e.g., "us-east-1").
    /// </summary>
    public string? AwsRegion { get; set; }

    /// <summary>
    /// AWS access key ID. Falls back to default credential chain if not set.
    /// </summary>
    public string? AwsAccessKeyId { get; set; }

    /// <summary>
    /// AWS secret access key. Falls back to default credential chain if not set.
    /// </summary>
    public string? AwsSecretAccessKey { get; set; }
}
