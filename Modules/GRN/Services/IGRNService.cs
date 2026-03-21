using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.GRN.Services;

public interface IGRNService
{
    Task<PagedResult<GoodsReceivedNote>> GetPagedAsync(
        PagedQuery query, string? search = null, GRNStatus? status = null,
        DateTime? from = null, DateTime? to = null, CancellationToken ct = default);

    Task<GoodsReceivedNote?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Create GRN from a purchase order (#363). Pre-fills expected items.</summary>
    Task<GoodsReceivedNote> CreateFromPOAsync(int purchaseOrderId, string? notes, CancellationToken ct = default);

    /// <summary>Create GRN without a PO (direct receipt).</summary>
    Task<GoodsReceivedNote> CreateDirectAsync(CreateGRNDto dto, CancellationToken ct = default);

    /// <summary>Confirm GRN: updates stock (#366) and cost price (#367).</summary>
    Task ConfirmAsync(int grnId, IReadOnlyList<GRNReceiveLine> lines, CancellationToken ct = default);

    Task CancelAsync(int grnId, CancellationToken ct = default);
}

public record CreateGRNDto(
    int SupplierId,
    string? Notes,
    IReadOnlyList<GRNLineDto> Items);

public record GRNLineDto(
    int ProductId,
    int QtyExpected,
    decimal UnitCost);

public record GRNReceiveLine(
    int GRNItemId,
    int QtyReceived,
    int QtyRejected);
