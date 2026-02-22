using StoreAssistantPro.Core.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Sales.Services;

public interface ISalesService
{
    Task<PagedResult<Sale>> GetPagedAsync(PagedQuery query, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<IEnumerable<Sale>> GetAllAsync(CancellationToken ct = default);
    Task<Sale?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateSaleAsync(Sale sale, CancellationToken ct = default);
    Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
