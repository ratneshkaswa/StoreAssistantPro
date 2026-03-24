using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.PurchaseOrders.Services;

public interface IPurchaseOrderService
{
    Task<IReadOnlyList<PurchaseOrder>> GetAllAsync(CancellationToken ct = default);
    Task<PagedResult<PurchaseOrder>> GetPagedAsync(PagedQuery query, string? search = null, PurchaseOrderStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<IReadOnlyList<PurchaseOrder>> SearchAsync(string? query, PurchaseOrderStatus? status, DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PurchaseOrder> CreateAsync(CreatePurchaseOrderDto dto, CancellationToken ct = default);
    Task UpdateStatusAsync(int id, PurchaseOrderStatus status, CancellationToken ct = default);
    Task ReceiveItemsAsync(int poId, IReadOnlyList<ReceiveLineDto> lines, CancellationToken ct = default);
    Task<IReadOnlyList<Supplier>> GetActiveSuppliersAsync(CancellationToken ct = default);

    /// <summary>Clone a previous PO to quickly reorder same products from same supplier (#221).</summary>
    Task<PurchaseOrder> DuplicateAsync(int purchaseOrderId, CancellationToken ct = default);

    /// <summary>Suggest PO lines for products below min stock level, grouped by supplier (#222).</summary>
    Task<IReadOnlyList<LowStockPOSuggestion>> GetLowStockSuggestionsAsync(CancellationToken ct = default);

    /// <summary>Create PO from CSV data: ProductName, Quantity, UnitCost (#223).</summary>
    Task<PurchaseOrder> ImportFromCsvAsync(int supplierId, IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default);

    /// <summary>Export PO details to CSV lines (#224).</summary>
    Task<IReadOnlyList<string>> ExportToCsvLinesAsync(DateTime? from, DateTime? to, CancellationToken ct = default);

    /// <summary>Get formatted print data for a purchase order (#219/#451).</summary>
    Task<PurchaseOrderPrintData?> GetPrintDataAsync(int purchaseOrderId, CancellationToken ct = default);
}

public record LowStockPOSuggestion(
    int SupplierId,
    string SupplierName,
    int ProductId,
    string ProductName,
    int CurrentStock,
    int MinStockLevel,
    int SuggestedQty,
    decimal LastCostPrice);

public record CreatePurchaseOrderDto(
    int SupplierId,
    DateTime? ExpectedDate,
    string? Notes,
    IReadOnlyList<PurchaseOrderLineDto> Items);

public record PurchaseOrderLineDto(
    int ProductId,
    int Quantity,
    decimal UnitCost);

public record ReceiveLineDto(
    int PurchaseOrderItemId,
    int QuantityReceived);

/// <summary>Print data for a purchase order document (#219/#451).</summary>
public record PurchaseOrderPrintData(
    string OrderNumber,
    DateTime OrderDate,
    DateTime? ExpectedDate,
    string SupplierName,
    string? SupplierPhone,
    string? SupplierGSTIN,
    string? SupplierAddress,
    string? Notes,
    string Status,
    IReadOnlyList<PurchaseOrderPrintLine> Lines,
    decimal TotalAmount);

public record PurchaseOrderPrintLine(
    string ProductName,
    string? HSNCode,
    int Quantity,
    decimal UnitCost,
    decimal LineTotal);
