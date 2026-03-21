using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Persists and retrieves held (parked) bills (#336-#346).
/// </summary>
public interface IHeldBillService
{
    /// <summary>Hold (park) the current cart for later recall (#336).</summary>
    Task<HeldBill> HoldAsync(HoldBillDto dto, CancellationToken ct = default);

    /// <summary>Recall a held bill and mark it inactive (#337).</summary>
    Task<HeldBill?> RecallAsync(int heldBillId, CancellationToken ct = default);

    /// <summary>Get all currently active held bills (#338).</summary>
    Task<IReadOnlyList<HeldBill>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Count of active held bills (#340).</summary>
    Task<int> GetActiveCountAsync(CancellationToken ct = default);

    /// <summary>Discard a held bill without recalling (#346).</summary>
    Task DiscardAsync(int heldBillId, CancellationToken ct = default);

    /// <summary>Archive stale held bills from previous sessions (#346).</summary>
    Task<int> CleanupStaleAsync(DateTime cutoff, CancellationToken ct = default);
}

public record HoldBillDto(
    string Label,
    string? CustomerTag,
    string? Notes,
    string? CashierRole,
    decimal Total,
    IReadOnlyList<HoldBillItemDto> Items);

public record HoldBillItemDto(
    int ProductId,
    int? ProductVariantId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TaxRate,
    bool IsTaxInclusive,
    decimal ItemDiscountRate,
    decimal ItemDiscountAmount,
    decimal CessRate = 0);
