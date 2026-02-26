using TheWatch.Contracts.Abstractions;

namespace TheWatch.Contracts.CoreGateway.Models;

public record ProfileListResponse(List<UserProfileDto> Items, int TotalCount, int Page, int PageSize);
public record ServiceHealthSummary(List<ServiceRegistrationDto> Services, int HealthyCount, int UnhealthyCount);
