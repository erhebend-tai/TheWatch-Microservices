namespace TheWatch.Contracts.Wearable.Models;

public enum DevicePlatform { AppleWatch, WearOS, Garmin, Samsung, Fitbit, Other }
public enum DeviceStatus { Connected, Syncing, Disconnected, LowBattery, Error }
public enum SyncDirection { DeviceToServer, ServerToDevice, Bidirectional }
