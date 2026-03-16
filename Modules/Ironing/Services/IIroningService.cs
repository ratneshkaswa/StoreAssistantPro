using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Ironing.Services;

public interface IIroningService
{
    // ── Single entries ──
    Task<IReadOnlyList<IroningEntry>> GetAllEntriesAsync(CancellationToken ct = default);
    Task CreateEntryAsync(IroningEntryDto dto, CancellationToken ct = default);
    Task UpdateEntryAsync(int id, IroningEntryDto dto, CancellationToken ct = default);
    Task DeleteEntryAsync(int id, CancellationToken ct = default);
    Task MarkPaidAsync(int id, CancellationToken ct = default);

    // ── Batches ──
    Task<IReadOnlyList<IroningBatch>> GetAllBatchesAsync(CancellationToken ct = default);
    Task<IroningBatch?> GetBatchByIdAsync(int id, CancellationToken ct = default);
    Task CreateBatchAsync(IroningBatchDto dto, CancellationToken ct = default);
    Task UpdateBatchAsync(int id, IroningBatchDto dto, CancellationToken ct = default);
    Task DeleteBatchAsync(int id, CancellationToken ct = default);
    Task SetBatchStatusAsync(int id, string status, CancellationToken ct = default);

    // ── Cloths master ──
    Task<IReadOnlyList<Cloth>> GetClothsAsync(CancellationToken ct = default);
    Task CreateClothAsync(string category, decimal price, CancellationToken ct = default);
    Task DeleteClothAsync(int id, CancellationToken ct = default);

    Task<IroningStats> GetStatsAsync(CancellationToken ct = default);
}

public record IroningEntryDto(
    DateTime Date,
    string CustomerName,
    string? Items,
    int Quantity,
    decimal Rate,
    decimal Amount,
    bool IsPaid);

public record IroningBatchDto(
    DateTime Date,
    string? Note,
    IReadOnlyList<IroningBatchItemDto> Items);

public record IroningBatchItemDto(
    string ClothName,
    int Quantity,
    int ReceivedQty,
    decimal Rate,
    decimal Amount);

public record IroningStats(
    int TotalEntries,
    int UnpaidEntries,
    decimal TotalAmount,
    decimal PaidAmount,
    int ActiveBatches);
