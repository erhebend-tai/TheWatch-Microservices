namespace TheWatch.Contracts.FirstResponder.Models;

public record ResponderListResponse(List<ResponderDto> Items, int TotalCount, int Page, int PageSize);
public record ResponderSummary(Guid Id, string Name, ResponderType Type, ResponderStatus Status, double? DistanceKm, GeoLocationDto? Location);
