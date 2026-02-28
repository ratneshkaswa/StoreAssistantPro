using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Suppliers.Services;

public interface ISupplierService
{
    Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Supplier>> GetActiveAsync(CancellationToken ct = default);
    Task<Supplier?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Supplier supplier, CancellationToken ct = default);
    Task UpdateAsync(Supplier supplier, CancellationToken ct = default);
    Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default);
}
