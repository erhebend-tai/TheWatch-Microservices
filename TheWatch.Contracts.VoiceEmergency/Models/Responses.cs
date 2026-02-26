namespace TheWatch.Contracts.VoiceEmergency.Models;

public record IncidentListResponse(List<IncidentDto> Items, int TotalCount, int Page, int PageSize);
