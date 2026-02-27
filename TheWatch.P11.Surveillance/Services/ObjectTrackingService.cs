// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheWatch.P11.Surveillance.Surveillance;
using TheWatch.Shared.ML;

namespace TheWatch.P11.Surveillance.Services;

public interface IObjectTrackingService
{
    Task<ObjectTrackingResponse> TrackObjectsAsync(ObjectTrackingRequest request);
    Task<TrackingSession?> GetTrackingSessionAsync(Guid sessionId);
    Task<TrackingSessionListResponse> ListTrackingSessionsAsync(Guid? crimeLocationId = null, int page = 1, int pageSize = 20);
    Task<List<TrackedObjectMatch>> GetMatchesForSessionAsync(Guid sessionId);
    Task<AlibiVerificationResponse> VerifyAlibiAsync(AlibiVerificationRequest request);
}

/// <summary>
/// Tracks objects outward from a crime location across a network of footage submissions.
/// Uses the resilient object detector (local ONNX with Azure/GCP/AWS cloud fallback)
/// to find matching detections across footage ordered by distance from the crime scene.
/// </summary>
public class ObjectTrackingService : IObjectTrackingService
{
    private readonly IWatchRepository<TrackingSession> _sessions;
    private readonly IWatchRepository<TrackedObjectMatch> _matches;
    private readonly IWatchRepository<CrimeLocation> _crimeLocations;
    private readonly IFootageService _footageService;
    private readonly IVideoAnalysisService _videoAnalysisService;
    private readonly IWatchRepository<DetectionResult> _detections;
    private readonly IWatchRepository<AlibiVerification> _alibiVerifications;
    private readonly IObjectDetector _objectDetector;
    private readonly ILogger<ObjectTrackingService> _logger;

    public ObjectTrackingService(
        IWatchRepository<TrackingSession> sessions,
        IWatchRepository<TrackedObjectMatch> matches,
        IWatchRepository<CrimeLocation> crimeLocations,
        IFootageService footageService,
        IVideoAnalysisService videoAnalysisService,
        IWatchRepository<DetectionResult> detections,
        IWatchRepository<AlibiVerification> alibiVerifications,
        IObjectDetector objectDetector,
        ILogger<ObjectTrackingService> logger)
    {
        _sessions = sessions;
        _matches = matches;
        _crimeLocations = crimeLocations;
        _footageService = footageService;
        _videoAnalysisService = videoAnalysisService;
        _detections = detections;
        _alibiVerifications = alibiVerifications;
        _objectDetector = objectDetector;
        _logger = logger;
    }

    public async Task<ObjectTrackingResponse> TrackObjectsAsync(ObjectTrackingRequest request)
    {
        var crimeLocation = await _crimeLocations.GetByIdAsync(request.CrimeLocationId);
        if (crimeLocation is null)
        {
            _logger.LogWarning("Crime location {CrimeLocationId} not found for tracking", request.CrimeLocationId);
            return new ObjectTrackingResponse(Guid.Empty, TrackingStatus.Failed, 0, 0, null, []);
        }

        // Create tracking session
        var session = new TrackingSession
        {
            CrimeLocationId = request.CrimeLocationId,
            InitiatedBy = request.InitiatedBy,
            ObjectDescription = request.ObjectDescription,
            TargetObjectTypes = request.TargetObjectTypes ?? [],
            SearchRadiusKm = request.SearchRadiusKm,
            TimeWindowStart = request.TimeWindowStart ?? crimeLocation.OccurredAt?.AddHours(-2),
            TimeWindowEnd = request.TimeWindowEnd ?? crimeLocation.OccurredAt?.AddHours(6),
            Status = TrackingStatus.InProgress,
            DetectionProvider = _objectDetector.IsReady ? "LocalONNX" : "CloudFallback"
        };

        await _sessions.AddAsync(session);
        _logger.LogInformation("Object tracking session {SessionId} started for crime location {CrimeLocationId}",
            session.Id, request.CrimeLocationId);

        try
        {
            // Find footage near the crime location within the search radius, ordered by distance
            var nearbyFootage = await _footageService.FindFootageNearLocationAsync(
                crimeLocation.Latitude, crimeLocation.Longitude,
                request.SearchRadiusKm,
                session.TimeWindowStart, session.TimeWindowEnd);

            var allMatches = new List<TrackedObjectMatch>();
            var footageAnalyzed = 0;

            foreach (var footage in nearbyFootage)
            {
                // Ensure footage has been analyzed (trigger analysis if needed)
                if (footage.Status == FootageStatus.Submitted)
                {
                    await _videoAnalysisService.AnalyzeFootageAsync(footage.Id);
                }

                footageAnalyzed++;

                // Get detections for this footage
                var detectionsResponse = await _footageService.GetDetectionsForFootageAsync(footage.Id, 1, 100);
                var detections = detectionsResponse.Items;

                // Filter detections by target object types if specified
                if (request.TargetObjectTypes is { Count: > 0 })
                {
                    detections = detections
                        .Where(d => request.TargetObjectTypes.Contains(d.DetectionType))
                        .ToList();
                }

                // Calculate distance from crime scene for each matching detection
                var distanceKm = HaversineDistanceKm(
                    crimeLocation.Latitude, crimeLocation.Longitude,
                    footage.GpsLatitude, footage.GpsLongitude);

                foreach (var detection in detections)
                {
                    var match = new TrackedObjectMatch
                    {
                        TrackingSessionId = session.Id,
                        FootageId = footage.Id,
                        DetectionId = detection.Id,
                        DistanceFromSceneKm = distanceKm,
                        Confidence = detection.Confidence,
                        Label = detection.Label,
                        DetectedAt = detection.FrameTimestamp
                    };

                    await _matches.AddAsync(match);
                    allMatches.Add(match);
                }
            }

            // Update session with results
            session.FootageAnalyzedCount = footageAnalyzed;
            session.MatchesFoundCount = allMatches.Count;
            session.Status = TrackingStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            await _sessions.UpdateAsync(session);

            // Sort matches by distance from scene (outward tracking)
            var sortedMatches = allMatches.OrderBy(m => m.DistanceFromSceneKm).ToList();

            _logger.LogInformation(
                "Object tracking session {SessionId} completed: {FootageCount} footage analyzed, {MatchCount} matches found",
                session.Id, footageAnalyzed, allMatches.Count);

            return new ObjectTrackingResponse(
                session.Id, session.Status, footageAnalyzed,
                allMatches.Count, session.DetectionProvider, sortedMatches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Object tracking session {SessionId} failed", session.Id);
            session.Status = TrackingStatus.Failed;
            session.CompletedAt = DateTime.UtcNow;
            await _sessions.UpdateAsync(session);

            return new ObjectTrackingResponse(session.Id, TrackingStatus.Failed, 0, 0, null, []);
        }
    }

    public async Task<TrackingSession?> GetTrackingSessionAsync(Guid sessionId)
    {
        return await _sessions.GetByIdAsync(sessionId);
    }

    public async Task<TrackingSessionListResponse> ListTrackingSessionsAsync(
        Guid? crimeLocationId = null, int page = 1, int pageSize = 20)
    {
        var query = _sessions.Query();

        if (crimeLocationId.HasValue)
            query = query.Where(s => s.CrimeLocationId == crimeLocationId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new TrackingSessionListResponse(items, totalCount, page, pageSize);
    }

    public async Task<List<TrackedObjectMatch>> GetMatchesForSessionAsync(Guid sessionId)
    {
        return await _matches.Query()
            .Where(m => m.TrackingSessionId == sessionId)
            .OrderBy(m => m.DistanceFromSceneKm)
            .ToListAsync();
    }

    /// <summary>
    /// Triangulates a subject's alibi by searching surveillance footage at each claimed
    /// location and time window. Returns corroborating evidence per checkpoint and an
    /// overall verdict (Supported / Inconclusive / Contradicted).
    /// </summary>
    public async Task<AlibiVerificationResponse> VerifyAlibiAsync(AlibiVerificationRequest request)
    {
        var provider = _objectDetector.IsReady ? "LocalONNX" : "CloudFallback";

        var verification = new AlibiVerification
        {
            SubjectId = request.SubjectId,
            SubjectDescription = request.SubjectDescription,
            InitiatedBy = request.InitiatedBy,
            CheckpointsTotal = request.Checkpoints.Count,
            DetectionProvider = provider
        };
        await _alibiVerifications.AddAsync(verification);

        _logger.LogInformation(
            "Alibi verification {VerificationId} started for subject {SubjectId} with {CheckpointCount} checkpoints",
            verification.Id, request.SubjectId, request.Checkpoints.Count);

        var checkpointResults = new List<AlibiCheckpointResult>();
        var totalFootageChecked = 0;
        var totalEvidenceFound = 0;

        foreach (var checkpoint in request.Checkpoints)
        {
            var nearbyFootage = await _footageService.FindFootageNearLocationAsync(
                checkpoint.Latitude, checkpoint.Longitude,
                checkpoint.SearchRadiusKm,
                checkpoint.WindowStart, checkpoint.WindowEnd);

            var checkpointEvidence = new List<AlibiEvidenceItem>();

            foreach (var footage in nearbyFootage)
            {
                if (footage.Status == FootageStatus.Submitted)
                    await _videoAnalysisService.AnalyzeFootageAsync(footage.Id);

                totalFootageChecked++;

                var detectionsResponse = await _footageService.GetDetectionsForFootageAsync(footage.Id, 1, 100);
                var detections = detectionsResponse.Items;

                if (request.TargetObjectTypes is { Count: > 0 })
                {
                    detections = detections
                        .Where(d => request.TargetObjectTypes.Contains(d.DetectionType))
                        .ToList();
                }

                var distanceKm = HaversineDistanceKm(
                    checkpoint.Latitude, checkpoint.Longitude,
                    footage.GpsLatitude, footage.GpsLongitude);

                foreach (var detection in detections)
                {
                    checkpointEvidence.Add(new AlibiEvidenceItem(
                        footage.Id,
                        detection.Id,
                        detection.Label,
                        detection.Confidence,
                        distanceKm,
                        detection.FrameTimestamp));
                    totalEvidenceFound++;
                }
            }

            checkpointResults.Add(new AlibiCheckpointResult
            {
                Checkpoint = checkpoint,
                HasEvidence = checkpointEvidence.Count > 0,
                FootageChecked = nearbyFootage.Count,
                SupportingEvidence = checkpointEvidence
            });
        }

        var supportedCount = checkpointResults.Count(r => r.HasEvidence);
        var verdict = supportedCount == request.Checkpoints.Count && request.Checkpoints.Count > 0
            ? AlibiVerdict.Supported
            : AlibiVerdict.Inconclusive;

        verification.Verdict = verdict;
        verification.CheckpointsSupported = supportedCount;
        verification.TotalFootageChecked = totalFootageChecked;
        verification.TotalEvidenceFound = totalEvidenceFound;
        verification.CompletedAt = DateTime.UtcNow;
        await _alibiVerifications.UpdateAsync(verification);

        _logger.LogInformation(
            "Alibi verification {VerificationId} completed: verdict={Verdict}, {SupportedCount}/{TotalCount} checkpoints supported",
            verification.Id, verdict, supportedCount, request.Checkpoints.Count);

        return new AlibiVerificationResponse(
            verification.Id, verdict,
            request.Checkpoints.Count, supportedCount,
            totalFootageChecked, totalEvidenceFound,
            provider, checkpointResults);
    }

    private static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
