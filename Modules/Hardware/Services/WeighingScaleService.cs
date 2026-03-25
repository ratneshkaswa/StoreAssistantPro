using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.Hardware;
using StoreAssistantPro.Modules.Hardware.Events;

namespace StoreAssistantPro.Modules.Hardware.Services;

/// <summary>
/// Weighing scale service — reads weight via serial/USB protocols
/// (common protocols: CAS, Mettler-Toledo, A&amp;D continuous output).
/// </summary>
public sealed class WeighingScaleService(
    IEventBus eventBus,
    ILogger<WeighingScaleService> logger) : IWeighingScaleService
{
    private HardwareDeviceConfig? _config;
    private decimal _tareWeight;
    private readonly List<DeviceStatus> _scales = [];

    public DeviceConnectionStatus Status { get; private set; } = DeviceConnectionStatus.Disconnected;
    public WeightReading? CurrentReading { get; private set; }

    public event Action<WeightReading>? StableWeightRead;

    public Task<bool> DetectAndConnectAsync(CancellationToken ct = default)
    {
        // Real implementation: enumerate COM ports, send identification query,
        // parse response to detect scale model.
        logger.LogInformation("Detecting USB/serial weighing scales…");

        var scale = new DeviceStatus
        {
            DeviceName = "Default Weighing Scale",
            DeviceType = HardwareDeviceType.WeighingScale,
            ConnectionStatus = DeviceConnectionStatus.Connected,
            LastSeenUtc = DateTime.UtcNow
        };

        _scales.Add(scale);
        Status = DeviceConnectionStatus.Connected;

        eventBus.PublishAsync(new DeviceStatusChangedEvent(scale));
        logger.LogInformation("Weighing scale connected");
        return Task.FromResult(true);
    }

    public Task<WeightReading?> ReadWeightAsync(CancellationToken ct = default)
    {
        // Real implementation: read from serial port, parse weight string.
        // Common format: "ST,GS, +  0.000kg" (stable, gross, weight, unit).
        // Simulated reading for development:
        var reading = new WeightReading
        {
            Weight = 0.0m,
            Unit = "kg",
            IsStable = true,
            TareWeight = _tareWeight
        };

        CurrentReading = reading;

        if (reading.IsStable)
        {
            StableWeightRead?.Invoke(reading);
            _ = eventBus.PublishAsync(new WeightReadingEvent(reading));
        }

        return Task.FromResult<WeightReading?>(reading);
    }

    public decimal ConvertWeightToQuantity(WeightReading reading, string uom, decimal conversionFactor)
    {
        // E.g., 2.5 kg of fabric at 1 meter per kg → 2.5 meters.
        var netWeight = reading.NetWeight;
        return uom.ToLowerInvariant() switch
        {
            "kg" or "g" or "lb" => netWeight, // weight IS the quantity
            "meters" or "m" => netWeight * conversionFactor,
            "pcs" => Math.Ceiling(netWeight * conversionFactor),
            _ => netWeight * conversionFactor
        };
    }

    public Task<bool> TareAsync(CancellationToken ct = default)
    {
        if (CurrentReading is not null)
        {
            _tareWeight = CurrentReading.Weight;
            logger.LogInformation("Scale tared at {Weight} {Unit}", _tareWeight, CurrentReading.Unit);
        }
        return Task.FromResult(true);
    }

    public Task<bool> ConfigureAsync(HardwareDeviceConfig config, CancellationToken ct = default)
    {
        _config = config;
        logger.LogInformation("Scale configured: {Name} on {Port} at {Baud} baud",
            config.DeviceName, config.PortName, config.BaudRate);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<DeviceStatus>> GetAllScalesAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<DeviceStatus>>(_scales.AsReadOnly());
    }

    public decimal CalculateWeightBasedPrice(WeightReading reading, decimal pricePerUnit)
    {
        return reading.NetWeight * pricePerUnit;
    }
}
