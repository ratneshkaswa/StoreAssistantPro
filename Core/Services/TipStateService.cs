using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// File-backed implementation of <see cref="ITipStateService"/>.
/// <para>
/// <b>Storage:</b> A single JSON file at
/// <c>%USERPROFILE%\Documents\StoreAssistantPro\Settings\dismissed-tips.json</c>
/// containing a sorted array of tip keys. The file is small (typically
/// &lt;1 KB) and written atomically via temp-file + rename — identical
/// to the pattern used by <c>OfflineBillingQueue</c>.
/// </para>
/// <para>
/// <b>Thread safety:</b> A <see cref="Lock"/> guards all mutations.
/// Reads after initial load are lock-free against the
/// <see cref="HashSet{T}"/> snapshot. Writes acquire the lock, update
/// the set, and flush asynchronously on a background thread.
/// </para>
/// <para>
/// <b>Lazy load:</b> The settings file is read on the first call to
/// <see cref="IsTipDismissed"/> or <see cref="DismissTip"/>, not at
/// construction time. This avoids blocking the DI container build.
/// </para>
/// </summary>
public sealed class TipStateService : ITipStateService
{
    private readonly string _filePath;
    private readonly ILogger<TipStateService> _logger;
    private readonly Lock _lock = new();

    private HashSet<string>? _dismissed;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    // ── Constructor ────────────────────────────────────────────────

    public TipStateService(ILogger<TipStateService> logger)
        : this(DefaultFilePath(), logger)
    {
    }

    /// <summary>
    /// Test-friendly constructor that accepts an explicit file path.
    /// </summary>
    public TipStateService(string filePath, ILogger<TipStateService> logger)
    {
        _filePath = filePath;
        _logger = logger;

        // Register as the global resolver so TipBannerAutoState can call
        // back into this service at load time without a DI reference.
        TipBannerAutoState.IsDismissedResolver = IsTipDismissed;
        TipBannerAutoState.DismissFunc = DismissTip;
    }

    // ── Public API ─────────────────────────────────────────────────

    public bool IsTipDismissed(string tipKey)
    {
        EnsureLoaded();
        lock (_lock)
        {
            return _dismissed!.Contains(tipKey);
        }
    }

    public void DismissTip(string tipKey)
    {
        EnsureLoaded();
        bool added;
        lock (_lock)
        {
            added = _dismissed!.Add(tipKey);
        }

        if (added)
        {
            _logger.LogDebug("Tip dismissed: {TipKey}", tipKey);
            FlushAsync();
        }
    }

    public void ResetTip(string tipKey)
    {
        EnsureLoaded();
        bool removed;
        lock (_lock)
        {
            removed = _dismissed!.Remove(tipKey);
        }

        if (removed)
        {
            _logger.LogDebug("Tip reset: {TipKey}", tipKey);
            FlushAsync();
        }
    }

    public void ResetAll()
    {
        EnsureLoaded();
        int count;
        lock (_lock)
        {
            count = _dismissed!.Count;
            _dismissed.Clear();
        }

        if (count > 0)
        {
            _logger.LogInformation("All {Count} dismissed tips reset", count);
            FlushAsync();
        }
    }

    // ── Lazy load ──────────────────────────────────────────────────

    private void EnsureLoaded()
    {
        if (_dismissed is not null)
            return;

        lock (_lock)
        {
            if (_dismissed is not null)
                return;

            _dismissed = LoadFromFile();
        }
    }

    private HashSet<string> LoadFromFile()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var json = File.ReadAllText(_filePath);
            var keys = JsonSerializer.Deserialize<string[]>(json);

            if (keys is not null)
            {
                _logger.LogDebug(
                    "Loaded {Count} dismissed tip(s) from {Path}",
                    keys.Length, _filePath);
                return new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to load dismissed tips from {Path} — starting fresh",
                _filePath);
        }

        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    // ── Async flush ────────────────────────────────────────────────

    private void FlushAsync()
    {
        // Snapshot the current state under the lock, then write on
        // a background thread so DismissTip() never blocks the UI.
        string[] snapshot;
        lock (_lock)
        {
            snapshot = [.. _dismissed!.Order()];
        }

        _ = Task.Run(() => WriteFile(snapshot));
    }

    private void WriteFile(string[] keys)
    {
        AtomicFileWriter.Write(_filePath, keys, WriteOptions, _logger, "dismissed tips");
    }

    // ── Defaults ───────────────────────────────────────────────────

    private static string DefaultFilePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "StoreAssistantPro", "Settings", "dismissed-tips.json");
}
