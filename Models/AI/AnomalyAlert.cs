namespace StoreAssistantPro.Models.AI;

/// <summary>An anomalous transaction or pattern detected by the AI engine.</summary>
public sealed class AnomalyAlert
{
    public int Id { get; set; }

    /// <summary>Type: "UnusualTransaction", "TheftPattern", "DiscountAbuse",
    /// "VoidPattern", "InventoryShrinkage", "PriceAnomaly".</summary>
    public string AlertType { get; init; } = string.Empty;

    /// <summary>Severity: "Low", "Medium", "High", "Critical".</summary>
    public string Severity { get; init; } = "Medium";

    /// <summary>Human-readable description of the anomaly.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Entity involved — sale ID, product ID, or user ID.</summary>
    public int? RelatedEntityId { get; init; }

    /// <summary>Entity type: "Sale", "Product", "User".</summary>
    public string? RelatedEntityType { get; init; }

    /// <summary>Anomaly score 0.0–1.0 (higher = more anomalous).</summary>
    public double AnomalyScore { get; init; }

    /// <summary>When the anomaly was detected.</summary>
    public DateTime DetectedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Whether this alert has been reviewed by a manager.</summary>
    public bool IsReviewed { get; set; }

    /// <summary>Manager notes after review.</summary>
    public string? ReviewNotes { get; set; }
}
