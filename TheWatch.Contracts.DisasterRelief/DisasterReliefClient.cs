using TheWatch.Contracts.Abstractions;
using TheWatch.Contracts.DisasterRelief.Models;

namespace TheWatch.Contracts.DisasterRelief;

public class DisasterReliefClient(HttpClient http) : ServiceClientBase(http, "DisasterRelief"), IDisasterReliefClient
{
    public Task<DisasterEventDto> GetEventAsync(Guid id, CancellationToken ct)
        => GetAsync<DisasterEventDto>($"/api/disasters/{id}", ct);

    public Task<DisasterEventListResponse> ListEventsAsync(int page, int pageSize, EventStatus? status, CancellationToken ct)
    {
        var query = $"/api/disasters?page={page}&pageSize={pageSize}";
        if (status.HasValue) query += $"&status={status.Value}";
        return GetAsync<DisasterEventListResponse>(query, ct);
    }

    public Task<DisasterEventDto> CreateEventAsync(CreateDisasterEventRequest request, CancellationToken ct)
        => PostAsync<DisasterEventDto>("/api/disasters", request, ct);

    public Task<DisasterEventDto> UpdateEventStatusAsync(Guid id, UpdateEventStatusRequest request, CancellationToken ct)
        => PutAsync<DisasterEventDto>($"/api/disasters/{id}/status", request, ct);

    public Task<ShelterDto> GetShelterAsync(Guid id, CancellationToken ct)
        => GetAsync<ShelterDto>($"/api/shelters/{id}", ct);

    public Task<ShelterListResponse> ListSheltersAsync(Guid? eventId, CancellationToken ct)
    {
        var query = eventId.HasValue ? $"/api/shelters?eventId={eventId}" : "/api/shelters";
        return GetAsync<ShelterListResponse>(query, ct);
    }

    public Task<ShelterDto> CreateShelterAsync(CreateShelterRequest request, CancellationToken ct)
        => PostAsync<ShelterDto>("/api/shelters", request, ct);

    public Task UpdateOccupancyAsync(Guid shelterId, UpdateOccupancyRequest request, CancellationToken ct)
        => PutAsync($"/api/shelters/{shelterId}/occupancy", request, ct);

    public Task<ResourceItemDto> DonateResourceAsync(DonateResourceRequest request, CancellationToken ct)
        => PostAsync<ResourceItemDto>("/api/resources/donate", request, ct);

    public Task<ResourceRequestDto> CreateResourceRequestAsync(CreateResourceRequestRecord request, CancellationToken ct)
        => PostAsync<ResourceRequestDto>("/api/resources/request", request, ct);

    public Task<EvacuationRouteDto> CreateEvacRouteAsync(CreateEvacuationRouteRequest request, CancellationToken ct)
        => PostAsync<EvacuationRouteDto>("/api/evacuation-routes", request, ct);

    public Task<List<EvacuationRouteDto>> ListEvacRoutesAsync(Guid eventId, CancellationToken ct)
        => GetAsync<List<EvacuationRouteDto>>($"/api/evacuation-routes?eventId={eventId}", ct);
}
