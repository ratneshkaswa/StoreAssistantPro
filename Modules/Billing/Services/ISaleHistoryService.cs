using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

public interface ISaleHistoryService
{
    Task<IReadOnlyList<Sale>> GetSalesAsync(DateTime? from, DateTime? to, string? invoiceSearch, CancellationToken ct = default);
    Task<PagedResult<Sale>> GetPagedAsync(PagedQuery query, DateTime? from = null, DateTime? to = null, string? invoiceSearch = null, CancellationToken ct = default);
    Task<Sale?> GetSaleDetailAsync(int saleId, CancellationToken ct = default);
}
