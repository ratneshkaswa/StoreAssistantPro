using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.Hardware;

namespace StoreAssistantPro.Modules.Hardware.Events;

/// <summary>Published when a barcode scanner reads a barcode.</summary>
public sealed class BarcodeScanEvent(BarcodeScanResult result) : IEvent
{
    public BarcodeScanResult Result { get; } = result;
}

/// <summary>Published when a hardware device connects or disconnects.</summary>
public sealed class DeviceStatusChangedEvent(DeviceStatus status) : IEvent
{
    public DeviceStatus Status { get; } = status;
}

/// <summary>Published when the cash drawer opens or closes.</summary>
public sealed class CashDrawerStateChangedEvent(CashDrawerState state) : IEvent
{
    public CashDrawerState State { get; } = state;
}

/// <summary>Published when a stable weight reading is obtained.</summary>
public sealed class WeightReadingEvent(WeightReading reading) : IEvent
{
    public WeightReading Reading { get; } = reading;
}

/// <summary>Published when a receipt finishes printing.</summary>
public sealed class ReceiptPrintedEvent(string invoiceNumber, bool success, string? error = null) : IEvent
{
    public string InvoiceNumber { get; } = invoiceNumber;
    public bool Success { get; } = success;
    public string? Error { get; } = error;
}
