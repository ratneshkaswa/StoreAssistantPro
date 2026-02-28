using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Inward.Services;

public interface IInwardService
{
    /// <summary>
    /// Gets the next parcel number sequence for the given month/year.
    /// Returns the next available sequence number (1-based).
    /// </summary>
    Task<int> GetNextSequenceAsync(int month, int year, CancellationToken ct = default);

    /// <summary>
    /// Generates the parcel number string in MM-NN format.
    /// </summary>
    string FormatParcelNumber(int month, int sequence);

    /// <summary>
    /// Generates the inward entry number in MM-NN format for the first parcel.
    /// </summary>
    string FormatInwardNumber(int month, int firstSequence);

    Task<InwardEntry> SaveInwardEntryAsync(InwardEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<InwardEntry>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InwardEntry>> GetByMonthAsync(int month, int year, CancellationToken ct = default);
    Task<InwardEntry?> GetByIdAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default);
}
