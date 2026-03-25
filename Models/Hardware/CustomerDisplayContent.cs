namespace StoreAssistantPro.Models.Hardware;

/// <summary>Content to display on a customer-facing screen or pole display.</summary>
public sealed class CustomerDisplayContent
{
    /// <summary>Primary line — product name or store name.</summary>
    public string Line1 { get; set; } = string.Empty;

    /// <summary>Secondary line — price or promo text.</summary>
    public string Line2 { get; set; } = string.Empty;

    /// <summary>Grand total (for end-of-transaction display).</summary>
    public decimal? GrandTotal { get; set; }

    /// <summary>Whether this is an idle/promotional message vs transactional.</summary>
    public bool IsIdle { get; set; }
}
