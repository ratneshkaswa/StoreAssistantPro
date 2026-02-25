using StoreAssistantPro.Core.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Commands;

namespace StoreAssistantPro.Modules.Products.Services;

public interface IProductService
{
    Task<PagedResult<Product>> GetPagedAsync(PagedQuery query, CancellationToken ct = default);
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task<int> AddRangeAsync(IReadOnlyList<Product> products, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default);
    Task<(int Deleted, IReadOnlyList<string> FailedNames)> DeleteRangeAsync(
        IReadOnlyList<BulkDeleteItem> items, CancellationToken ct = default);
    Task<int> GetLowStockCountAsync(CancellationToken ct = default);
    Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeProductId = null, CancellationToken ct = default);
}
