using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Vendors.Services;

public interface IVendorService
{
    Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Vendor>> GetActiveAsync(CancellationToken ct = default);
    Task<Vendor?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Vendor vendor, CancellationToken ct = default);
    Task UpdateAsync(Vendor vendor, CancellationToken ct = default);
    Task ToggleActiveAsync(int id, byte[]? rowVersion, CancellationToken ct = default);
    Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
}
