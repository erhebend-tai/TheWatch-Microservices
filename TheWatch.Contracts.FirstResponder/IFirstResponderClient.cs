using TheWatch.Contracts.FirstResponder.Models;

namespace TheWatch.Contracts.FirstResponder;

public interface IFirstResponderClient
{
    Task<ResponderDto> GetResponderAsync(Guid id, CancellationToken ct = default);
    Task<ResponderListResponse> ListRespondersAsync(int page = 1, int pageSize = 20, ResponderType? type = null, CancellationToken ct = default);
    Task<ResponderDto> RegisterResponderAsync(RegisterResponderRequest request, CancellationToken ct = default);
    Task UpdateLocationAsync(Guid id, UpdateLocationRequest request, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct = default);
    Task<CheckInDto> CreateCheckInAsync(Guid responderId, CreateCheckInRequest request, CancellationToken ct = default);
    Task<List<ResponderSummary>> FindNearbyAsync(NearbyResponderQuery query, CancellationToken ct = default);
}
