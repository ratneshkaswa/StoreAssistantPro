using StoreAssistantPro.Models.Hardware;

namespace StoreAssistantPro.Modules.Hardware.Services;

/// <summary>
/// Manages USB/serial weighing scales.
/// Features #495–502: integration, auto-read, weight-to-quantity,
/// tare, configuration, multi-scale, weight display, weight-based pricing.
/// </summary>
public interface IWeighingScaleService
{
    /// <summary>Current connection status.</summary>
    DeviceConnectionStatus Status { get; }

    /// <summary>Detect and connect to a USB weighing scale. (#495)</summary>
    Task<bool> DetectAndConnectAsync(CancellationToken ct = default);

    /// <summary>Read the current weight. When stable, publishes WeightReadingEvent. (#496)</summary>
    Task<WeightReading?> ReadWeightAsync(CancellationToken ct = default);

    /// <summary>
    /// Convert a weight reading to quantity based on the product's UOM.
    /// E.g., 2.5 kg fabric at 1 meter/kg = 2.5 meters. (#497)
    /// </summary>
    decimal ConvertWeightToQuantity(WeightReading reading, string uom, decimal conversionFactor);

    /// <summary>Zero/tare the scale for packaging weight. (#498)</summary>
    Task<bool> TareAsync(CancellationToken ct = default);

    /// <summary>Configure scale COM port, baud rate, protocol. (#499)</summary>
    Task<bool> ConfigureAsync(HardwareDeviceConfig config, CancellationToken ct = default);

    /// <summary>Get all connected scales. (#500)</summary>
    Task<IReadOnlyList<DeviceStatus>> GetAllScalesAsync(CancellationToken ct = default);

    /// <summary>Get the current live weight for display. (#501)</summary>
    WeightReading? CurrentReading { get; }

    /// <summary>
    /// Calculate price from weight using the product's per-unit price.
    /// E.g., 2.5 kg × ₹400/kg = ₹1,000. (#502)
    /// </summary>
    decimal CalculateWeightBasedPrice(WeightReading reading, decimal pricePerUnit);

    /// <summary>Raised when a stable reading is obtained.</summary>
    event Action<WeightReading>? StableWeightRead;
}
