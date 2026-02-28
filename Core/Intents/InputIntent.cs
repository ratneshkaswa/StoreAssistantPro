namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Classifies the type of user input detected by the
/// <see cref="IIntentDetectionService"/>.
/// </summary>
public enum InputIntent
{
    /// <summary>Input does not match any recognizable intent.</summary>
    Unknown,

    /// <summary>
    /// Input matches barcode format — rapid numeric/alpha entry
    /// typically from a hardware scanner (EAN-13, Code128, etc.).
    /// </summary>
    BarcodeScan,

    /// <summary>
    /// Input matches an exact product name, SKU, or barcode in the catalog.
    /// </summary>
    ExactProductMatch,

    /// <summary>
    /// Input completes a PIN entry — 4 digits (user PIN) or 6 digits
    /// (master PIN) of all-numeric content.
    /// </summary>
    PinCompleted,

    /// <summary>
    /// Input has reached the minimum threshold for auto-complete
    /// suggestion display (≥ 2 characters, non-numeric, during search).
    /// </summary>
    AutoCompleteTrigger
}
