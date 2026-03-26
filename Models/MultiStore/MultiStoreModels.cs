namespace StoreAssistantPro.Models.MultiStore;

/// <summary>Store entity (#591).</summary>
public sealed class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? GSTIN { get; set; }
    public int? ParentStoreId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Inter-store stock transfer (#601-603).</summary>
public sealed class StockTransfer
{
    public int Id { get; set; }
    public int FromStoreId { get; set; }
    public int ToStoreId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; } = "Requested"; // Requested, Approved, InTransit, Received, Rejected
    public int? RequestedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Sync state record (#621-639).</summary>
public sealed record SyncStatus(
    int StoreId,
    string StoreName,
    DateTime LastSyncAt,
    int PendingChanges,
    bool IsSyncing,
    string? LastError);
