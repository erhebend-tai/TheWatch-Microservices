namespace TheWatch.Contracts.DisasterRelief.Models;

public record DisasterEventListResponse(List<DisasterEventDto> Items, int TotalCount, int Page, int PageSize);
public record ShelterListResponse(List<ShelterDto> Items, int TotalCount);
public record ShelterSummary(Guid Id, string Name, ShelterStatus Status, int Capacity, int CurrentOccupancy, double? DistanceKm);
