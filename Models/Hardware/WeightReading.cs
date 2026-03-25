namespace StoreAssistantPro.Models.Hardware;

/// <summary>A weight reading from a connected scale.</summary>
public sealed class WeightReading
{
    /// <summary>Weight in the unit configured on the scale (kg, g, lb).</summary>
    public decimal Weight { get; init; }

    /// <summary>Unit of measurement: "kg", "g", "lb".</summary>
    public string Unit { get; init; } = "kg";

    /// <summary>Whether the reading is stable (not fluctuating).</summary>
    public bool IsStable { get; init; }

    /// <summary>Tare weight subtracted.</summary>
    public decimal TareWeight { get; init; }

    /// <summary>Net weight after tare.</summary>
    public decimal NetWeight => Weight - TareWeight;

    public DateTime ReadAtUtc { get; init; } = DateTime.UtcNow;
}
