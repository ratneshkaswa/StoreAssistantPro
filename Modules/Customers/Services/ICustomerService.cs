using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Customers.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> SearchAsync(string query, CancellationToken ct = default);
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(CustomerDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, CustomerDto dto, CancellationToken ct = default);
    Task ToggleActiveAsync(int id, CancellationToken ct = default);
    Task<int> ImportBulkAsync(IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default);
}

public record CustomerDto(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? GSTIN,
    string? Notes);
