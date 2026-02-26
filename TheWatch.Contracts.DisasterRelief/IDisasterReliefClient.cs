using TheWatch.Contracts.DisasterRelief.Models;

namespace TheWatch.Contracts.DisasterRelief;

public interface IDisasterReliefClient
{
    Task<DisasterEventDto> GetEventAsync(Guid id, CancellationToken ct = default);
    Task<DisasterEventListResponse> ListEventsAsync(int page = 1, int pageSize = 20, EventStatus? status = null, CancellationToken ct = default);
    Task<DisasterEventDto> CreateEventAsync(CreateDisasterEventRequest request, CancellationToken ct = default);
    Task<DisasterEventDto> UpdateEventStatusAsync(Guid id, UpdateEventStatusRequest request, CancellationToken ct = default);
    Task<ShelterDto> GetShelterAsync(Guid id, CancellationToken ct = default);
    Task<ShelterListResponse> ListSheltersAsync(Guid? eventId = null, CancellationToken ct = default);
    Task<ShelterDto> CreateShelterAsync(CreateShelterRequest request, CancellationToken ct = default);
    Task UpdateOccupancyAsync(Guid shelterId, UpdateOccupancyRequest request, CancellationToken ct = default);
    Task<ResourceItemDto> DonateResourceAsync(DonateResourceRequest request, CancellationToken ct = default);
    Task<ResourceRequestDto> CreateResourceRequestAsync(CreateResourceRequestRecord request, CancellationToken ct = default);
    Task<EvacuationRouteDto> CreateEvacRouteAsync(CreateEvacuationRouteRequest request, CancellationToken ct = default);
    Task<List<EvacuationRouteDto>> ListEvacRoutesAsync(Guid eventId, CancellationToken ct = default);
}
