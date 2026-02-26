// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using TheWatch.Contracts.Surveillance.Models;

namespace TheWatch.Contracts.Surveillance;

public interface ISurveillanceClient
{
    // Cameras
    Task<CameraRegistrationDto> RegisterCameraAsync(RegisterCameraRequest request, CancellationToken ct = default);
    Task<CameraListResponse> ListCamerasAsync(int page = 1, int pageSize = 20, CameraStatus? status = null, CancellationToken ct = default);
    Task<CameraRegistrationDto> GetCameraAsync(Guid id, CancellationToken ct = default);
    Task<CameraRegistrationDto> VerifyCameraAsync(Guid id, CancellationToken ct = default);
    Task DeactivateCameraAsync(Guid id, CancellationToken ct = default);

    // Footage
    Task<FootageSubmissionDto> SubmitFootageAsync(SubmitFootageRequest request, CancellationToken ct = default);
    Task<FootageListResponse> ListFootageAsync(int page = 1, int pageSize = 20, FootageStatus? status = null, CancellationToken ct = default);
    Task<FootageSubmissionDto> GetFootageAsync(Guid id, CancellationToken ct = default);
    Task<DetectionListResponse> GetFootageDetectionsAsync(Guid footageId, int page = 1, int pageSize = 50, CancellationToken ct = default);

    // Crime Locations
    Task<CrimeLocationDto> ReportCrimeLocationAsync(ReportCrimeLocationRequest request, CancellationToken ct = default);
    Task<CrimeLocationListResponse> ListCrimeLocationsAsync(int page = 1, int pageSize = 20, bool? activeOnly = true, CancellationToken ct = default);
    Task<CrimeLocationDto> GetCrimeLocationAsync(Guid id, CancellationToken ct = default);
    Task<List<FootageSubmissionDto>> GetFootageNearCrimeLocationAsync(Guid crimeLocationId, double radiusKm = 2.0, CancellationToken ct = default);

    // Search & Stats
    Task<List<SurveillanceSearchResultDto>> SearchAsync(SurveillanceSearchRequest request, CancellationToken ct = default);
    Task<SurveillanceStatsDto> GetStatsAsync(CancellationToken ct = default);
}
