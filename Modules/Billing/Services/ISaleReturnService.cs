using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

public interface ISaleReturnService
{
    /// <summary>Process a full or partial return against an existing sale.</summary>
    Task<SaleReturn> ProcessReturnAsync(SaleReturnDto dto, CancellationToken ct = default);

    /// <summary>Get all returns for a specific sale.</summary>
    Task<IReadOnlyList<SaleReturn>> GetReturnsBySaleAsync(int saleId, CancellationToken ct = default);

    /// <summary>Get recent returns across all sales.</summary>
    Task<IReadOnlyList<SaleReturn>> GetRecentReturnsAsync(int count = 100, CancellationToken ct = default);
}

public record SaleReturnDto(
    int SaleId,
    int SaleItemId,
    int QuantityReturned,
    string Reason,
    string? Notes);
