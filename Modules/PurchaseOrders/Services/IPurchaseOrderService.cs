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
}

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
