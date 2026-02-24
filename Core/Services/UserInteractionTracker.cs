using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Lock-free, file-backed singleton implementation of
/// <see cref="IUserInteractionTracker"/>.
///
/// <para><b>Performance characteristics:</b></para>
/// <list type="bullet">
///   <item><b>Record path</b> — single <see cref="Interlocked.Increment(ref long)"/>
///         plus one <see cref="ConcurrentDictionary{TKey,TValue}.AddOrUpdate"/>
///         per call. No heap allocation, no lock.</item>
///   <item><b>Flush</b> — a <see cref="Timer"/> fires every
///         <see cref="FlushIntervalSeconds"/> seconds. The timer
///         callback snapshots the dictionaries (one allocation) and
///         writes to disk on the thread-pool. If nothing changed since
///         the last flush, the callback is a no-op.</item>
///   <item><b>Read path</b> — <see cref="Volatile.Read(ref long)"/>
///         for scalar counters; dictionary reads are naturally
///         lock-free on <see cref="ConcurrentDictionary{TKey,TValue}"/>.</item>
/// </list>
///
/// <para><b>Auto-observation:</b></para>
/// <list type="bullet">
///   <item><see cref="IAppStateService.PropertyChanged"/> — detects
///         <see cref="BillingSessionState.Completed"/> transitions.</item>
///   <item><see cref="TransactionCommittedEvent"/> — detects product
///         creates by matching <c>OperationScope == "CreateProduct"</c>.</item>
/// </list>
///
/// <para><b>Storage:</b>
/// <c>%USERPROFILE%\Documents\StoreAssistantPro\Settings\interaction-counters.json</c>.
/// Atomic temp-file + rename, identical to <see cref="TipStateService"/>.</para>
///
/// <para><b>Thread safety:</b> Fully lock-free on the hot path.
/// The flush timer serialises writes to disk via a flag —
/// overlapping timer ticks are skipped.</para>
/// </summary>
public sealed class UserInteractionTracker : IUserInteractionTracker
{
    private readonly IAppStateService _appState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<UserInteractionTracker> _logger;
    private readonly string _filePath;
    private readonly Timer _flushTimer;

    // ── Scalar counters (Interlocked) ──────────────────────────────

    private long _totalWindowOpens;
    private long _totalBillingCompleted;
    private long _totalProductsCreated;

    // ── Per-key counters (ConcurrentDictionary) ────────────────────

    private readonly ConcurrentDictionary<string, long> _windowCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _featureCounts = new(StringComparer.OrdinalIgnoreCase);

    // ── Dirty tracking (avoids no-op flushes) ──────────────────────

    private long _changeCounter;
    private long _lastFlushedChange;

    // ── Flush guard (prevents overlapping timer callbacks) ──────────

    private int _flushing;

    // ── Configuration ──────────────────────────────────────────────

    /// <summary>
    /// Interval between automatic flushes to disk. Kept long to
    /// minimise I/O — counters are best-effort telemetry, not
    /// transactional state. 30 seconds balances freshness with cost.
    /// </summary>
    internal const int FlushIntervalSeconds = 30;

    private bool _loaded;
    private readonly Lock _loadLock = new();

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    // ── Constructor ────────────────────────────────────────────────

    public UserInteractionTracker(
        IAppStateService appState,
        IEventBus eventBus,
        ILogger<UserInteractionTracker> logger)
        : this(appState, eventBus, logger, DefaultFilePath())
    {
    }

    /// <summary>
    /// Test-friendly constructor that accepts an explicit file path.
    /// </summary>
    public UserInteractionTracker(
        IAppStateService appState,
        IEventBus eventBus,
        ILogger<UserInteractionTracker> logger,
        string filePath)
    {
        _appState = appState;
        _eventBus = eventBus;
        _logger = logger;
        _filePath = filePath;

        // Auto-track billing completions.
        _appState.PropertyChanged += OnAppStatePropertyChanged;

        // Auto-track product creates.
        _eventBus.Subscribe<TransactionCommittedEvent>(OnTransactionCommittedAsync);

        // Coalesced flush timer — fires every N seconds.
        _flushTimer = new Timer(
            _ => FlushIfDirty(),
            state: null,
            dueTime: TimeSpan.FromSeconds(FlushIntervalSeconds),
            period: TimeSpan.FromSeconds(FlushIntervalSeconds));
    }

    // ── Recording ──────────────────────────────────────────────────

    public void RecordWindowOpen(string windowName)
    {
        EnsureLoaded();

        Interlocked.Increment(ref _totalWindowOpens);
        _windowCounts.AddOrUpdate(windowName, 1, static (_, prev) => prev + 1);
        MarkDirty();
    }

    public void RecordBillingCompleted()
    {
        EnsureLoaded();

        Interlocked.Increment(ref _totalBillingCompleted);
        MarkDirty();
    }

    public void RecordProductCreated()
    {
        EnsureLoaded();

        Interlocked.Increment(ref _totalProductsCreated);
        MarkDirty();
    }

    public void RecordFeatureUsed(string featureKey)
    {
        EnsureLoaded();

        _featureCounts.AddOrUpdate(featureKey, 1, static (_, prev) => prev + 1);
        MarkDirty();
    }

    // ── Queries ────────────────────────────────────────────────────

    public long TotalWindowOpens
    {
        get { EnsureLoaded(); return Volatile.Read(ref _totalWindowOpens); }
    }

    public long TotalBillingCompleted
    {
        get { EnsureLoaded(); return Volatile.Read(ref _totalBillingCompleted); }
    }

    public long TotalProductsCreated
    {
        get { EnsureLoaded(); return Volatile.Read(ref _totalProductsCreated); }
    }

    public int DistinctWindowCount
    {
        get { EnsureLoaded(); return _windowCounts.Count; }
    }

    public long GetWindowOpenCount(string windowName)
    {
        EnsureLoaded();
        return _windowCounts.GetValueOrDefault(windowName, 0);
    }

    public long GetFeatureUsageCount(string featureKey)
    {
        EnsureLoaded();
        return _featureCounts.GetValueOrDefault(featureKey, 0);
    }

    public IReadOnlyDictionary<string, long> GetAllWindowCounts()
    {
        EnsureLoaded();
        return new Dictionary<string, long>(_windowCounts, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, long> GetAllFeatureCounts()
    {
        EnsureLoaded();
        return new Dictionary<string, long>(_featureCounts, StringComparer.OrdinalIgnoreCase);
    }

    public void Reset()
    {
        EnsureLoaded();

        _windowCounts.Clear();
        _featureCounts.Clear();
        Interlocked.Exchange(ref _totalWindowOpens, 0);
        Interlocked.Exchange(ref _totalBillingCompleted, 0);
        Interlocked.Exchange(ref _totalProductsCreated, 0);
        MarkDirty();

        _logger.LogInformation("Interaction counters reset");

        // Flush immediately so the file reflects the reset.
        FlushIfDirty();
    }

    // ── Auto-observation ───────────────────────────────────────────

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppStateService.CurrentBillingSession)
            && _appState.CurrentBillingSession == BillingSessionState.Completed)
        {
            RecordBillingCompleted();
        }
    }

    private Task OnTransactionCommittedAsync(TransactionCommittedEvent evt)
    {
        if (string.Equals(evt.OperationScope, "CreateProduct", StringComparison.OrdinalIgnoreCase))
        {
            RecordProductCreated();
        }

        return Task.CompletedTask;
    }

    // ── Dirty tracking ─────────────────────────────────────────────

    private void MarkDirty() =>
        Interlocked.Increment(ref _changeCounter);

    // ── Lazy load ──────────────────────────────────────────────────

    private void EnsureLoaded()
    {
        if (Volatile.Read(ref _loaded))
            return;

        lock (_loadLock)
        {
            if (_loaded)
                return;

            LoadFromFile();
            _loaded = true;
        }
    }

    private void LoadFromFile()
    {
        try
        {
            if (!File.Exists(_filePath))
                return;

            var json = File.ReadAllText(_filePath);
            var dto = JsonSerializer.Deserialize<TrackerDto>(json);

            if (dto is null)
                return;

            Interlocked.Exchange(ref _totalWindowOpens, dto.TotalWindowOpens);
            Interlocked.Exchange(ref _totalBillingCompleted, dto.TotalBillingCompleted);
            Interlocked.Exchange(ref _totalProductsCreated, dto.TotalProductsCreated);

            if (dto.WindowCounts is not null)
            {
                foreach (var (key, value) in dto.WindowCounts)
                    _windowCounts[key] = value;
            }

            if (dto.FeatureCounts is not null)
            {
                foreach (var (key, value) in dto.FeatureCounts)
                    _featureCounts[key] = value;
            }

            _logger.LogDebug(
                "Loaded interaction counters from {Path} — Windows={WinTotal}, Billing={Bill}, Products={Prod}, Features={Feat}",
                _filePath, dto.TotalWindowOpens, dto.TotalBillingCompleted,
                dto.TotalProductsCreated, dto.FeatureCounts?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to load interaction counters from {Path} — starting fresh",
                _filePath);
        }
    }

    // ── Coalesced flush ────────────────────────────────────────────

    private void FlushIfDirty()
    {
        var current = Volatile.Read(ref _changeCounter);
        if (current == Volatile.Read(ref _lastFlushedChange))
            return;

        // Guard against overlapping flushes.
        if (Interlocked.CompareExchange(ref _flushing, 1, 0) != 0)
            return;

        try
        {
            WriteFile();
            Volatile.Write(ref _lastFlushedChange, current);
        }
        finally
        {
            Volatile.Write(ref _flushing, 0);
        }
    }

    private void WriteFile()
    {
        var dto = new TrackerDto
        {
            TotalWindowOpens = Volatile.Read(ref _totalWindowOpens),
            TotalBillingCompleted = Volatile.Read(ref _totalBillingCompleted),
            TotalProductsCreated = Volatile.Read(ref _totalProductsCreated),
            WindowCounts = new Dictionary<string, long>(_windowCounts, StringComparer.OrdinalIgnoreCase),
            FeatureCounts = new Dictionary<string, long>(_featureCounts, StringComparer.OrdinalIgnoreCase)
        };

        AtomicFileWriter.Write(_filePath, dto, WriteOptions, _logger, "interaction counters");
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        _flushTimer.Dispose();
        _appState.PropertyChanged -= OnAppStatePropertyChanged;
        _eventBus.Unsubscribe<TransactionCommittedEvent>(OnTransactionCommittedAsync);

        // Final flush to avoid losing recent counters.
        FlushIfDirty();
    }

    // ── Defaults ───────────────────────────────────────────────────

    private static string DefaultFilePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "StoreAssistantPro", "Settings", "interaction-counters.json");

    // ═════════════════════════════════════════════════════════════════
    //  Serializable DTO for JSON persistence
    // ═════════════════════════════════════════════════════════════════

    private sealed class TrackerDto
    {
        public long TotalWindowOpens { get; set; }
        public long TotalBillingCompleted { get; set; }
        public long TotalProductsCreated { get; set; }
        public Dictionary<string, long>? WindowCounts { get; set; }
        public Dictionary<string, long>? FeatureCounts { get; set; }
    }
}
