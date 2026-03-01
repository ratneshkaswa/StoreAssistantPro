using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Sales.Models;

/// <summary>
/// Sync status of an offline-queued bill.
/// </summary>
public enum OfflineBillStatus
{
    /// <summary>Bill is queued and waiting to be synced to the server.</summary>
    PendingSync = 0,

    /// <summary>Sync is currently in progress.</summary>
    Syncing = 1,

    /// <summary>Bill was successfully synced to the server.</summary>
    Synced = 2,

    /// <summary>Sync failed after all retry attempts.</summary>
    Failed = 3
}

/// <summary>
/// Represents a complete bill queued for offline storage. Contains all
/// data needed to reconstruct and persist the <see cref="Sale"/> to the
/// server database once connectivity is restored.
/// <para>
/// <b>Design rule:</b> The same billing model is used online and
/// offline. This DTO is the JSON-serializable envelope that wraps
/// a <see cref="CompleteSaleSnapshot"/> with queue metadata.
/// </para>
/// </summary>
public sealed class OfflineBill
{
    /// <summary>
    /// Client-generated idempotency key. Matches <see cref="Sale.IdempotencyKey"/>
    /// and is used as the queue file name to prevent duplicates.
    /// </summary>
    public Guid IdempotencyKey { get; init; }

    /// <summary>Current sync status of this queued bill.</summary>
    public OfflineBillStatus Status { get; set; } = OfflineBillStatus.PendingSync;

    /// <summary>When the bill was originally created (offline).</summary>
    public DateTime CreatedTime { get; init; }

    /// <summary>Last time a sync attempt was made.</summary>
    public DateTime? LastSyncAttempt { get; set; }

    /// <summary>Number of sync attempts made so far.</summary>
    public int SyncAttemptCount { get; set; }

    /// <summary>Error message from the last failed sync attempt.</summary>
    public string? LastError { get; set; }

    /// <summary>The full sale data to be persisted when online.</summary>
    public CompleteSaleSnapshot Sale { get; init; } = null!;
}

/// <summary>
/// Complete snapshot of a sale at the time of creation. Contains all
/// fields needed to construct a <see cref="Sale"/> entity without
/// any further lookups.
/// </summary>
public sealed class CompleteSaleSnapshot
{
    public decimal TotalAmount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string? CashierRole { get; init; }
    public DateTime SaleDate { get; init; }
    public int? CustomerId { get; init; }
    public int? StaffId { get; init; }
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal DiscountAmount { get; init; }
    public string? DiscountReason { get; init; }
    public string? CouponCode { get; init; }
    public string? VoucherCode { get; init; }
    public decimal VoucherRedeemAmount { get; init; }
    public List<SaleItemSnapshot> Items { get; init; } = [];
    public List<ExtraChargeSnapshot> ExtraCharges { get; init; } = [];
}

/// <summary>
/// Snapshot of a single line item within an offline bill.
/// </summary>
public sealed class SaleItemSnapshot
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal ItemDiscountRate { get; init; }
    public int? StaffId { get; init; }
}

/// <summary>
/// Snapshot of an extra charge within an offline bill.
/// </summary>
public sealed class ExtraChargeSnapshot
{
    public string Name { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public bool IsTaxable { get; init; }
}
