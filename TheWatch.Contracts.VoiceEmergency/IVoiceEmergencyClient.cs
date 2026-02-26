using TheWatch.Contracts.VoiceEmergency.Models;

namespace TheWatch.Contracts.VoiceEmergency;

public interface IVoiceEmergencyClient
{
    Task<IncidentDto> GetIncidentAsync(Guid id, CancellationToken ct = default);
    Task<IncidentListResponse> ListIncidentsAsync(int page = 1, int pageSize = 20, IncidentStatus? status = null, CancellationToken ct = default);
    Task<IncidentDto> CreateIncidentAsync(CreateIncidentRequest request, CancellationToken ct = default);
    Task<IncidentDto> UpdateIncidentStatusAsync(Guid id, UpdateIncidentStatusRequest request, CancellationToken ct = default);
    Task<DispatchDto> GetDispatchAsync(Guid id, CancellationToken ct = default);
    Task<DispatchDto> CreateDispatchAsync(CreateDispatchRequest request, CancellationToken ct = default);
    Task<DispatchDto> ExpandRadiusAsync(Guid dispatchId, ExpandRadiusRequest request, CancellationToken ct = default);
}
