// Copyright (c) 2018 Barton Milnor Mallory. All rights reserved.

using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.Surveillance.Models;

namespace TheWatch.Contracts.Surveillance;

public class SurveillanceClient(HttpClient http) : ServiceClientBase(http, "Surveillance"), ISurveillanceClient
{
    // Cameras

    public Task<CameraRegistrationDto> RegisterCameraAsync(RegisterCameraRequest request, CancellationToken ct)
        => PostAsync<CameraRegistrationDto>("/api/cameras", request, ct);

    public Task<CameraListResponse> ListCamerasAsync(int page, int pageSize, CameraStatus? status, CancellationToken ct)
    {
        var query = $"/api/cameras?page={page}&pageSize={pageSize}";
        if (status.HasValue) query += $"&status={status.Value}";
        return GetAsync<CameraListResponse>(query, ct);
    }

    public Task<CameraRegistrationDto> GetCameraAsync(Guid id, CancellationToken ct)
        => GetAsync<CameraRegistrationDto>($"/api/cameras/{id}", ct);

    public Task<CameraRegistrationDto> VerifyCameraAsync(Guid id, CancellationToken ct)
        => PutAsync<CameraRegistrationDto>($"/api/cameras/{id}/verify", null, ct);

    public Task DeactivateCameraAsync(Guid id, CancellationToken ct)
        => DeleteAsync($"/api/cameras/{id}", ct);

    // Footage

    public Task<FootageSubmissionDto> SubmitFootageAsync(SubmitFootageRequest request, CancellationToken ct)
        => PostAsync<FootageSubmissionDto>("/api/footage", request, ct);

    public Task<FootageListResponse> ListFootageAsync(int page, int pageSize, FootageStatus? status, CancellationToken ct)
    {
        var query = $"/api/footage?page={page}&pageSize={pageSize}";
        if (status.HasValue) query += $"&status={status.Value}";
        return GetAsync<FootageListResponse>(query, ct);
    }

    public Task<FootageSubmissionDto> GetFootageAsync(Guid id, CancellationToken ct)
        => GetAsync<FootageSubmissionDto>($"/api/footage/{id}", ct);

    public Task<DetectionListResponse> GetFootageDetectionsAsync(Guid footageId, int page, int pageSize, CancellationToken ct)
        => GetAsync<DetectionListResponse>($"/api/footage/{footageId}/detections?page={page}&pageSize={pageSize}", ct);

    // Crime Locations

    public Task<CrimeLocationDto> ReportCrimeLocationAsync(ReportCrimeLocationRequest request, CancellationToken ct)
        => PostAsync<CrimeLocationDto>("/api/crime-locations", request, ct);

    public Task<CrimeLocationListResponse> ListCrimeLocationsAsync(int page, int pageSize, bool? activeOnly, CancellationToken ct)
    {
        var query = $"/api/crime-locations?page={page}&pageSize={pageSize}";
        if (activeOnly.HasValue) query += $"&activeOnly={activeOnly.Value}";
        return GetAsync<CrimeLocationListResponse>(query, ct);
    }

    public Task<CrimeLocationDto> GetCrimeLocationAsync(Guid id, CancellationToken ct)
        => GetAsync<CrimeLocationDto>($"/api/crime-locations/{id}", ct);

    public Task<List<FootageSubmissionDto>> GetFootageNearCrimeLocationAsync(Guid crimeLocationId, double radiusKm, CancellationToken ct)
        => GetAsync<List<FootageSubmissionDto>>($"/api/crime-locations/{crimeLocationId}/footage?radiusKm={radiusKm}", ct);

    // Search & Stats

    public Task<List<SurveillanceSearchResultDto>> SearchAsync(SurveillanceSearchRequest request, CancellationToken ct)
        => PostAsync<List<SurveillanceSearchResultDto>>("/api/surveillance/search", request, ct);

    public Task<SurveillanceStatsDto> GetStatsAsync(CancellationToken ct)
        => GetAsync<SurveillanceStatsDto>("/api/surveillance/stats", ct);
}
