using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

public interface ISaleHistoryService
{
    Task<IReadOnlyList<Sale>> GetSalesAsync(DateTime? from, DateTime? to, string? invoiceSearch, CancellationToken ct = default);
    Task<Sale?> GetSaleDetailAsync(int saleId, CancellationToken ct = default);
}
