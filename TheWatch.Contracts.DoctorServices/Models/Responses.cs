namespace TheWatch.Contracts.DoctorServices.Models;

public record DoctorListResponse(List<DoctorProfileDto> Items, int TotalCount, int Page, int PageSize);
public record DoctorSummary(Guid Id, string Name, List<string> Specializations, double Rating, double? DistanceKm);
