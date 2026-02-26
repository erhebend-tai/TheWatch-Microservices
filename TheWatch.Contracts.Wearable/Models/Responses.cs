namespace TheWatch.Contracts.Wearable.Models;

public record DeviceListResponse(List<WearableDeviceDto> Items, int TotalCount);
public record HeartbeatHistory(List<HeartbeatReadingDto> Readings, int TotalCount);
