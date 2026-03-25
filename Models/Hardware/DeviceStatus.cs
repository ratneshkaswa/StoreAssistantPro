namespace StoreAssistantPro.Models.Hardware;

/// <summary>Runtime state of a connected hardware device.</summary>
public sealed class DeviceStatus
{
    public required string DeviceName { get; init; }
    public required HardwareDeviceType DeviceType { get; init; }
    public DeviceConnectionStatus ConnectionStatus { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastSeenUtc { get; set; }
}
