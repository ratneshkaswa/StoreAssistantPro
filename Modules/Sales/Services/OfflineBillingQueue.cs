using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Modules.Sales.Models;

namespace StoreAssistantPro.Modules.Sales.Services;

/// <summary>
/// File-based implementation of <see cref="IOfflineBillingQueue"/>.
/// Each bill is stored as a JSON file named
/// <c>{IdempotencyKey}.json</c> inside the queue directory.
/// <para>
/// <b>Why files and not SQLite?</b>
/// <list type="bullet">
///   <item>Zero additional dependencies — no new NuGet packages.</item>
///   <item>Each bill is an independent file — easy to inspect,
///         debug, and manually recover.</item>
///   <item>The queue is typically small (tens of bills at most
///         during a short outage) — no need for indexed queries.</item>
///   <item>Atomic write via temp-file + rename prevents corruption
///         on crash.</item>
/// </list>
/// </para>
/// </summary>
public sealed class OfflineBillingQueue : IOfflineBillingQueue
{
    private readonly string _queueDirectory;
    private readonly ILogger<OfflineBillingQueue> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public OfflineBillingQueue(ILogger<OfflineBillingQueue> logger)
        : this(DefaultQueueDirectory(), logger)
    {
    }

    /// <summary>
    /// Test-friendly constructor that accepts an explicit queue directory.
    /// </summary>
    public OfflineBillingQueue(string queueDirectory, ILogger<OfflineBillingQueue> logger)
    {
        _queueDirectory = queueDirectory;
        _logger = logger;
        Directory.CreateDirectory(_queueDirectory);
    }

    // ── Public API ─────────────────────────────────────────────────

    public async Task EnqueueAsync(OfflineBill bill, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await WriteFileAsync(bill, ct).ConfigureAwait(false);
            _logger.LogInformation(
                "Enqueued offline bill {Key}", bill.IdempotencyKey);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<OfflineBill>> GetAllAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await ReadAllAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<OfflineBill>> GetPendingAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct).ConfigureAwait(false);
        return all
            .Where(b => b.Status is OfflineBillStatus.PendingSync
                                  or OfflineBillStatus.Failed)
            .ToList()
            .AsReadOnly();
    }

    public async Task UpdateAsync(OfflineBill bill, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await WriteFileAsync(bill, ct).ConfigureAwait(false);
            _logger.LogDebug(
                "Updated offline bill {Key} → {Status}",
                bill.IdempotencyKey, bill.Status);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(Guid idempotencyKey, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var path = GetFilePath(idempotencyKey);
            if (File.Exists(path))
            {
                File.Delete(path);
                _logger.LogInformation(
                    "Removed synced offline bill {Key}", idempotencyKey);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct).ConfigureAwait(false);
        return all.Count;
    }

    public async Task<int> PendingCountAsync(CancellationToken ct = default)
    {
        var pending = await GetPendingAsync(ct).ConfigureAwait(false);
        return pending.Count;
    }

    // ── Internals ──────────────────────────────────────────────────

    private async Task WriteFileAsync(OfflineBill bill, CancellationToken ct)
    {
        var targetPath = GetFilePath(bill.IdempotencyKey);
        var tempPath = targetPath + ".tmp";

        // Atomic write: serialize to temp file, then rename.
        // Prevents corrupt JSON if the process crashes mid-write.
        await using (var stream = new FileStream(
            tempPath, FileMode.Create, FileAccess.Write,
            FileShare.None, 4096, useAsync: true))
        {
            await JsonSerializer.SerializeAsync(stream, bill, JsonOptions, ct)
                .ConfigureAwait(false);
        }

        File.Move(tempPath, targetPath, overwrite: true);
    }

    private async Task<List<OfflineBill>> ReadAllAsync(CancellationToken ct)
    {
        var files = Directory.GetFiles(_queueDirectory, "*.json");
        var bills = new List<OfflineBill>(files.Length);

        foreach (var file in files)
        {
            try
            {
                await using var stream = new FileStream(
                    file, FileMode.Open, FileAccess.Read,
                    FileShare.Read, 4096, useAsync: true);

                var bill = await JsonSerializer
                    .DeserializeAsync<OfflineBill>(stream, ReadOptions, ct)
                    .ConfigureAwait(false);

                if (bill is not null)
                    bills.Add(bill);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Skipping corrupt offline bill file: {File}",
                    Path.GetFileName(file));
            }
        }

        bills.Sort((a, b) => a.CreatedTime.CompareTo(b.CreatedTime));
        return bills;
    }

    private string GetFilePath(Guid idempotencyKey) =>
        Path.Combine(_queueDirectory, $"{idempotencyKey}.json");

    private static string DefaultQueueDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "StoreAssistantPro", "OfflineQueue");
}
