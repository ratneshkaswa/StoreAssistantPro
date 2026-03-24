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

    /// <summary>Mark GRN items as Accepted/Rejected during quality check (#368).</summary>
    Task QualityCheckAsync(int grnId, IReadOnlyList<GRNQualityLine> lines, CancellationToken ct = default);

    /// <summary>Create a purchase return to supplier (#374).</summary>
    Task<PurchaseReturn> CreatePurchaseReturnAsync(CreatePurchaseReturnDto dto, CancellationToken ct = default);

    /// <summary>Get all purchase returns.</summary>
    Task<IReadOnlyList<PurchaseReturn>> GetPurchaseReturnsAsync(CancellationToken ct = default);

    /// <summary>Export GRN data to CSV lines (#373).</summary>
    Task<IReadOnlyList<string>> ExportToCsvLinesAsync(DateTime? from, DateTime? to, CancellationToken ct = default);

    /// <summary>Get formatted print data for a GRN document (#371).</summary>
    Task<GRNPrintData?> GetPrintDataAsync(int grnId, CancellationToken ct = default);
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

/// <summary>Quality check result per GRN item (#368).</summary>
public record GRNQualityLine(
    int GRNItemId,
    int QtyAccepted,
    int QtyRejected,
    string? RejectionReason);

/// <summary>Purchase return to supplier (#374).</summary>
public record CreatePurchaseReturnDto(
    int SupplierId,
    string? Notes,
    IReadOnlyList<PurchaseReturnLineDto> Items);

public record PurchaseReturnLineDto(
    int ProductId,
    int Quantity,
    decimal UnitCost,
    string Reason);

/// <summary>Print data for a GRN document (#371).</summary>
public record GRNPrintData(
    string GRNNumber,
    DateTime ReceivedDate,
    string SupplierName,
    string? SupplierPhone,
    string? SupplierGSTIN,
    string? PurchaseOrderNumber,
    string? Notes,
    string Status,
    IReadOnlyList<GRNPrintLine> Lines,
    decimal TotalAmount);

public record GRNPrintLine(
    string ProductName,
    string? HSNCode,
    int QtyExpected,
    int QtyReceived,
    int QtyRejected,
    decimal UnitCost,
    decimal LineTotal);
