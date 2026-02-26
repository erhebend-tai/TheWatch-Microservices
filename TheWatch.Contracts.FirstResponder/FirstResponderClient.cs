using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.FirstResponder.Models;

namespace TheWatch.Contracts.FirstResponder;

public class FirstResponderClient(HttpClient http) : ServiceClientBase(http, "FirstResponder"), IFirstResponderClient
{
    public Task<ResponderDto> GetResponderAsync(Guid id, CancellationToken ct)
        => GetAsync<ResponderDto>($"/api/responders/{id}", ct);

    public Task<ResponderListResponse> ListRespondersAsync(int page, int pageSize, ResponderType? type, CancellationToken ct)
    {
        var query = $"/api/responders?page={page}&pageSize={pageSize}";
        if (type.HasValue) query += $"&type={type.Value}";
        return GetAsync<ResponderListResponse>(query, ct);
    }

    public Task<ResponderDto> RegisterResponderAsync(RegisterResponderRequest request, CancellationToken ct)
        => PostAsync<ResponderDto>("/api/responders", request, ct);

    public Task UpdateLocationAsync(Guid id, UpdateLocationRequest request, CancellationToken ct)
        => PutAsync($"/api/responders/{id}/location", request, ct);

    public Task UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct)
        => PutAsync($"/api/responders/{id}/status", request, ct);

    public Task<CheckInDto> CreateCheckInAsync(Guid responderId, CreateCheckInRequest request, CancellationToken ct)
        => PostAsync<CheckInDto>($"/api/responders/{responderId}/checkins", request, ct);

    public Task<List<ResponderSummary>> FindNearbyAsync(NearbyResponderQuery query, CancellationToken ct)
        => GetAsync<List<ResponderSummary>>($"/api/responders/nearby?lat={query.Latitude}&lon={query.Longitude}&radius={query.RadiusKm}&availableOnly={query.AvailableOnly}" + (query.Type.HasValue ? $"&type={query.Type.Value}" : ""), ct);
}
