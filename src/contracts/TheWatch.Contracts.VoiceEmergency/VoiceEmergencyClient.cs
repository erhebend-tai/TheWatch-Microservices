using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.VoiceEmergency.Models;

namespace TheWatch.Contracts.VoiceEmergency;

public class VoiceEmergencyClient(HttpClient http) : ServiceClientBase(http, "VoiceEmergency"), IVoiceEmergencyClient
{
    public Task<IncidentDto> GetIncidentAsync(Guid id, CancellationToken ct)
        => GetAsync<IncidentDto>($"/api/incidents/{id}", ct);

    public Task<IncidentListResponse> ListIncidentsAsync(int page, int pageSize, IncidentStatus? status, CancellationToken ct)
    {
        var query = $"/api/incidents?page={page}&pageSize={pageSize}";
        if (status.HasValue) query += $"&status={status.Value}";
        return GetAsync<IncidentListResponse>(query, ct);
    }

    public Task<IncidentDto> CreateIncidentAsync(CreateIncidentRequest request, CancellationToken ct)
        => PostAsync<IncidentDto>("/api/incidents", request, ct);

    public Task<IncidentDto> UpdateIncidentStatusAsync(Guid id, UpdateIncidentStatusRequest request, CancellationToken ct)
        => PutAsync<IncidentDto>($"/api/incidents/{id}/status", request, ct);

    public Task<DispatchDto> GetDispatchAsync(Guid id, CancellationToken ct)
        => GetAsync<DispatchDto>($"/api/dispatches/{id}", ct);

    public Task<DispatchDto> CreateDispatchAsync(CreateDispatchRequest request, CancellationToken ct)
        => PostAsync<DispatchDto>("/api/dispatches", request, ct);

    public Task<DispatchDto> ExpandRadiusAsync(Guid dispatchId, ExpandRadiusRequest request, CancellationToken ct)
        => PostAsync<DispatchDto>($"/api/dispatches/{dispatchId}/expand", request, ct);
}
