namespace StoreAssistantPro.Models.Hardware;

/// <summary>
/// Represents a configured hardware device with its connection parameters.
/// Stored in the database for persistence across restarts.
/// </summary>
public class HardwareDeviceConfig
{
    public int Id { get; set; }

    /// <summary>User-friendly name (e.g., "Counter 1 Scanner").</summary>
    public string DeviceName { get; set; } = string.Empty;

    public HardwareDeviceType DeviceType { get; set; }

    /// <summary>COM port or USB path (e.g., "COM3", "USB\\VID_05E0").</summary>
    public string? PortName { get; set; }

    /// <summary>Serial baud rate — used for COM port devices.</summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>Connection type: "USB", "Serial", "Bluetooth", "Network".</summary>
    public string ConnectionType { get; set; } = "USB";

    /// <summary>IP address for network-connected devices.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Network port for TCP/IP connected devices.</summary>
    public int? NetworkPort { get; set; }

    /// <summary>Device-specific model identifier (e.g., "Zebra ZD421", "Epson TM-T82").</summary>
    public string? ModelName { get; set; }

    /// <summary>Whether this device should auto-connect on startup.</summary>
    public bool AutoConnect { get; set; } = true;

    /// <summary>Whether this device is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>JSON blob for device-specific settings.</summary>
    public string? ExtraSettingsJson { get; set; }
}
