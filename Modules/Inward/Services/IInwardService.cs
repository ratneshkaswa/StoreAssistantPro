using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Inward.Services;

public interface IInwardService
{
    Task<IReadOnlyList<InwardEntry>> GetAllAsync(CancellationToken ct = default);
    Task<InwardEntry?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Generates the next available parcel number for the given date's month.
    /// Format: MM-NN (e.g., 03-01, 03-02). Resets each month.
    /// </summary>
    Task<string> GetNextParcelNumberAsync(DateTime date, CancellationToken ct = default);

    /// <summary>
    /// Generates a batch of sequential parcel numbers for the given count.
    /// </summary>
    Task<IReadOnlyList<string>> GenerateParcelNumbersAsync(DateTime date, int count, CancellationToken ct = default);

    /// <summary>
    /// Creates a complete inward entry with parcels and products in a single transaction.
    /// </summary>
    Task CreateAsync(InwardEntryDto dto, CancellationToken ct = default);
}

public record InwardEntryDto(
    DateTime InwardDate,
    int? VendorId,
    decimal TransportCharges,
    string? Notes,
    IReadOnlyList<InwardParcelDto> Parcels);

public record InwardParcelDto(
    int? VendorId,
    decimal TransportCharge,
    string? Description,
    IReadOnlyList<InwardProductDto> Products);

public record InwardProductDto(
    int ProductId,
    decimal Quantity,
    int? ColourId,
    int? SizeId,
    int? PatternId,
    int? VariantTypeId);
