using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Quotations.Services;

public interface IQuotationService
{
    Task<PagedResult<Quotation>> GetPagedAsync(
        PagedQuery query, string? search = null, QuotationStatus? status = null,
        DateTime? from = null, DateTime? to = null, CancellationToken ct = default);

    Task<Quotation?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Quotation> CreateAsync(CreateQuotationDto dto, CancellationToken ct = default);

    Task UpdateStatusAsync(int id, QuotationStatus status, CancellationToken ct = default);

    /// <summary>Auto-expire quotations past their ValidUntil date (#355).</summary>
    Task ExpireOverdueAsync(CancellationToken ct = default);

    /// <summary>Convert an accepted quotation into cart items for billing (#351).</summary>
    Task<IReadOnlyList<QuotationCartLine>> GetCartLinesAsync(int quotationId, CancellationToken ct = default);

    /// <summary>Mark quotation as converted and link to the created sale.</summary>
    Task MarkConvertedAsync(int quotationId, int saleId, CancellationToken ct = default);
}

public record CreateQuotationDto(
    int? CustomerId,
    DateTime ValidUntil,
    string? Notes,
    IReadOnlyList<QuotationLineDto> Items);

public record QuotationLineDto(
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal TaxRate,
    decimal CessRate);

/// <summary>Line data for converting a quotation into a billing cart (#351).</summary>
public record QuotationCartLine(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal CessRate);
