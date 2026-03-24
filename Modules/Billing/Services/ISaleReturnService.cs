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

    /// <summary>Process an exchange: return item(s) from original sale, apply credit to new sale (#147).</summary>
    Task<ExchangeResult> ExchangeAsync(ExchangeDto dto, CancellationToken ct = default);

    /// <summary>Accept return without original invoice. Admin-only, logged in audit trail (#153).</summary>
    Task<SaleReturn> NoBillReturnAsync(NoBillReturnDto dto, CancellationToken ct = default);
}

public record SaleReturnDto(
    int SaleId,
    int SaleItemId,
    int QuantityReturned,
    string Reason,
    string? Notes,
    string? ApproverPin);

public record ExchangeDto(
    SaleReturnDto Return,
    CompleteSaleDto NewSale);

public record ExchangeResult(
    SaleReturn Return,
    Sale NewSale,
    decimal CreditApplied);

/// <summary>Return without original invoice — admin only (#153).</summary>
public record NoBillReturnDto(
    int ProductId,
    int? ProductVariantId,
    int QuantityReturned,
    decimal RefundAmount,
    string Reason,
    string? Notes,
    string ApproverPin);
