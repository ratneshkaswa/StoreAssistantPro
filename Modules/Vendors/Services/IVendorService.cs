using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Vendors.Services;

public interface IVendorService
{
    Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Vendor>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Vendor>> SearchAsync(string query, CancellationToken ct = default);
    Task<Vendor?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(VendorDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, VendorDto dto, CancellationToken ct = default);
    Task ToggleActiveAsync(int id, CancellationToken ct = default);
    Task<int> ImportBulkAsync(IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default);
}

public record VendorDto(
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? AddressLine2,
    string? City,
    string? State,
    string? PinCode,
    string? GSTIN,
    string? PAN,
    string? TransportPreference,
    string? PaymentTerms,
    decimal CreditLimit,
    decimal OpeningBalance,
    string? Notes);
