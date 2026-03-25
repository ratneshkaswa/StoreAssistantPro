namespace StoreAssistantPro.Models.Hardware;

/// <summary>Thermal printer status flags.</summary>
[Flags]
public enum PrinterStatusFlags
{
    None = 0,
    Online = 1,
    PaperLow = 2,
    PaperOut = 4,
    CoverOpen = 8,
    CutterError = 16,
    Offline = 32
}
