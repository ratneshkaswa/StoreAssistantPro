namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Describes the context in which input was received.
/// Drives intent classification rules — the same text may resolve to
/// different intents depending on the active context.
/// </summary>
public enum InputContext
{
    /// <summary>General search / text field — no special context.</summary>
    General,

    /// <summary>PIN entry field (login, master PIN, approval).</summary>
    PinEntry,

    /// <summary>Billing mode — cart search / barcode entry.</summary>
    BillingSearch,

    /// <summary>Product catalog search field.</summary>
    ProductSearch
}
