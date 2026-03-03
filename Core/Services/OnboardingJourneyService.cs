using System.ComponentModel;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// File-backed singleton implementation of <see cref="IOnboardingJourneyService"/>.
///
/// <para><b>Storage:</b> A single JSON file at
/// <c>%USERPROFILE%\Documents\StoreAssistantPro\Settings\onboarding-journey.json</c>
/// containing the milestone counters and current level. The file is
/// small (&lt;1 KB) and written atomically via temp-file + rename —
/// identical to the pattern used by <see cref="TipStateService"/>.</para>
///
/// <para><b>Event-driven tracking:</b> The service subscribes to
/// <see cref="IAppStateService.PropertyChanged"/> to automatically
/// record billing session completions — no manual calls needed from
/// billing code.</para>
///
/// <para><b>Promotion pipeline:</b> After every recorded action,
/// <see cref="EvaluatePromotion"/> runs the rule set against the
/// current counters. If a rule fires:
/// <list type="number">
///   <item>The profile is advanced to the next level.</item>
///   <item>The counters are flushed to disk.</item>
///   <item><see cref="ExperienceLevelPromotedEvent"/> is published
///         on the event bus.</item>
///   <item><see cref="ITipRotationService.InvalidateAll"/> is called
///         so banners refresh immediately.</item>
/// </list>
/// </para>
///
/// <para><b>Thread safety:</b> A <see cref="Lock"/> guards all
/// mutable state. The critical section is short — no I/O or event
/// publishing happens under the lock.</para>
/// </summary>
public sealed class OnboardingJourneyService : IOnboardingJourneyService
{
    private readonly IAppStateService _appState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OnboardingJourneyService> _logger;
    private readonly string _filePath;
    private readonly Lock _lock = new();

    private JourneyState _state;
    private UserExperienceProfile _profile;
    private volatile bool _loaded;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    // ── Promotion thresholds ───────────────────────────────────────

    /// <summary>Distinct windows the operator must open to be promoted from Beginner → Intermediate.</summary>
    internal const int BeginnerWindowThreshold = 5;

    /// <summary>Completed sessions required to be promoted from Beginner → Intermediate.</summary>
    internal const int BeginnerSessionThreshold = 3;

    /// <summary>Completed billing sessions required to be promoted from Intermediate → Advanced.</summary>
    internal const int IntermediateBillingThreshold = 5;

    /// <summary>Completed sessions required to be promoted from Intermediate → Advanced.</summary>
    internal const int IntermediateSessionThreshold = 10;

    // ── Constructor ────────────────────────────────────────────────

    public OnboardingJourneyService(
        IAppStateService appState,
        IEventBus eventBus,
        ILogger<OnboardingJourneyService> logger)
        : this(appState, eventBus, logger, DefaultFilePath())
    {
    }

    /// <summary>
    /// Test-friendly constructor that accepts an explicit file path.
    /// </summary>
    public OnboardingJourneyService(
        IAppStateService appState,
        IEventBus eventBus,
        ILogger<OnboardingJourneyService> logger,
        string filePath)
    {
        _appState = appState;
        _eventBus = eventBus;
        _logger = logger;
        _filePath = filePath;

        _state = new JourneyState();
        _profile = UserExperienceProfile.Default;

        // Auto-track billing session completions.
        _appState.PropertyChanged += OnAppStatePropertyChanged;
    }

    // ── Public API ─────────────────────────────────────────────────

    public UserExperienceProfile CurrentProfile
    {
        get
        {
            EnsureLoaded();
            lock (_lock) return _profile;
        }
    }

    public int TotalSessions
    {
        get { EnsureLoaded(); lock (_lock) return _state.Sessions; }
    }

    public int DistinctWindowsOpened
    {
        get { EnsureLoaded(); lock (_lock) return _state.DistinctWindows.Count; }
    }

    public int TotalWindowOpens
    {
        get { EnsureLoaded(); lock (_lock) return _state.TotalWindowOpens; }
    }

    public int TotalBillingCompleted
    {
        get { EnsureLoaded(); lock (_lock) return _state.BillingCompleted; }
    }

    public void RecordWindowOpen(string windowName)
    {
        EnsureLoaded();

        int distinct, total;
        lock (_lock)
        {
            _state.DistinctWindows.Add(windowName);
            _state.TotalWindowOpens++;
            distinct = _state.DistinctWindows.Count;
            total = _state.TotalWindowOpens;
        }

        _logger.LogDebug(
            "Window opened: {Window} (distinct={Distinct}, total={Total})",
            windowName, distinct, total);

        EvaluateAndFlush();
    }

    public void RecordBillingCompleted()
    {
        EnsureLoaded();

        int count;
        lock (_lock)
        {
            _state.BillingCompleted++;
            count = _state.BillingCompleted;
        }

        _logger.LogDebug(
            "Billing completed (total={Count})",
            count);

        EvaluateAndFlush();
    }

    public void RecordSessionStart()
    {
        EnsureLoaded();

        int count;
        lock (_lock)
        {
            _state.Sessions++;
            count = _state.Sessions;
        }

        _logger.LogDebug(
            "Session started (total={Count})",
            count);

        EvaluateAndFlush();
    }

    public void SetLevel(UserExperienceLevel level)
    {
        EnsureLoaded();

        UserExperienceLevel previous;
        lock (_lock)
        {
            previous = _state.Level;
            _state.Level = level;
            _profile = RebuildProfile();
        }

        _logger.LogInformation(
            "Experience level manually set: {Previous} → {New}",
            previous, level);

        FlushAsync();

        PublishSafe(new ExperienceLevelPromotedEvent(
            previous, level, "Manual override"));
    }

    public void Reset()
    {
        UserExperienceLevel previous;
        lock (_lock)
        {
            previous = _state.Level;
            _state = new JourneyState();
            _profile = UserExperienceProfile.Default;
            _loaded = true;
        }

        _logger.LogInformation("Onboarding journey reset to Beginner");

        FlushAsync();

        PublishSafe(new ExperienceLevelPromotedEvent(
            previous, UserExperienceLevel.Beginner, "Reset"));
    }

    // ── Event handlers ─────────────────────────────────────────────

    private void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Auto-track billing session completions.
        if (e.PropertyName == nameof(IAppStateService.CurrentBillingSession)
            && _appState.CurrentBillingSession == BillingSessionState.Completed)
        {
            RecordBillingCompleted();
        }
    }

    // ── Promotion engine ───────────────────────────────────────────

    private void EvaluateAndFlush()
    {
        var promotion = EvaluatePromotion();

        FlushAsync();

        if (promotion is not null)
            PublishSafe(promotion);
    }

    /// <summary>
    /// Evaluates all promotion rules against the current counters.
    /// Returns an <see cref="ExperienceLevelPromotedEvent"/> if a
    /// promotion fired, or <c>null</c> if no rule matched.
    /// </summary>
    private ExperienceLevelPromotedEvent? EvaluatePromotion()
    {
        lock (_lock)
        {
            var level = _state.Level;

            var (promoted, reason) = level switch
            {
                UserExperienceLevel.Beginner => EvaluateBeginnerRules(),
                UserExperienceLevel.Intermediate => EvaluateIntermediateRules(),
                _ => (false, (string?)null)
            };

            if (!promoted || reason is null)
                return null;

            var previous = level;
            _state.Level = _profile.NextLevel ?? level;
            _profile = RebuildProfile();

            _logger.LogInformation(
                "Auto-promotion: {Previous} → {New} ({Reason})",
                previous, _state.Level, reason);

            return new ExperienceLevelPromotedEvent(previous, _state.Level, reason);
        }
    }

    private (bool Promoted, string? Reason) EvaluateBeginnerRules()
    {
        // Rule 1: Opened 5+ distinct windows
        if (_state.DistinctWindows.Count >= BeginnerWindowThreshold)
            return (true, $"Opened {_state.DistinctWindows.Count} distinct windows");

        // Rule 2: Completed 3+ sessions
        if (_state.Sessions >= BeginnerSessionThreshold)
            return (true, $"Completed {_state.Sessions} sessions");

        return (false, null);
    }

    private (bool Promoted, string? Reason) EvaluateIntermediateRules()
    {
        // Rule 1: Completed 5+ billing sessions
        if (_state.BillingCompleted >= IntermediateBillingThreshold)
            return (true, $"Completed {_state.BillingCompleted} billing sessions");

        // Rule 2: Completed 10+ sessions
        if (_state.Sessions >= IntermediateSessionThreshold)
            return (true, $"Completed {_state.Sessions} sessions");

        return (false, null);
    }

    // ── Profile rebuild ────────────────────────────────────────────

    private UserExperienceProfile RebuildProfile() =>
        UserExperienceProfile.For(_state.Level, _state.Sessions);

    // ── Lazy load ──────────────────────────────────────────────────

    private void EnsureLoaded()
    {
        if (Volatile.Read(ref _loaded))
            return;

        lock (_lock)
        {
            if (_loaded)
                return;

            _state = LoadFromFile();
            _profile = RebuildProfile();
            _loaded = true;
        }
    }

    private JourneyState LoadFromFile()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new JourneyState();

            var json = File.ReadAllText(_filePath);
            var dto = JsonSerializer.Deserialize<JourneyStateDto>(json);

            if (dto is not null)
            {
                _logger.LogDebug(
                    "Loaded onboarding journey from {Path} — Level={Level}, Sessions={Sessions}",
                    _filePath, dto.Level, dto.Sessions);

                return new JourneyState
                {
                    Level = dto.Level,
                    Sessions = dto.Sessions,
                    TotalWindowOpens = dto.TotalWindowOpens,
                    BillingCompleted = dto.BillingCompleted,
                    DistinctWindows = dto.DistinctWindows is not null
                        ? new HashSet<string>(dto.DistinctWindows, StringComparer.OrdinalIgnoreCase)
                        : new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to load onboarding journey from {Path} — starting fresh",
                _filePath);
        }

        return new JourneyState();
    }

    // ── Async flush ────────────────────────────────────────────────

    private void FlushAsync()
    {
        JourneyStateDto snapshot;
        lock (_lock)
        {
            snapshot = new JourneyStateDto
            {
                Level = _state.Level,
                Sessions = _state.Sessions,
                TotalWindowOpens = _state.TotalWindowOpens,
                BillingCompleted = _state.BillingCompleted,
                DistinctWindows = [.. _state.DistinctWindows.Order()]
            };
        }

        _ = Task.Run(() =>
        {
            try
            {
                WriteFile(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to flush onboarding journey state to disk");
            }
        });
    }

    private void WriteFile(JourneyStateDto dto)
    {
        AtomicFileWriter.Write(_filePath, dto, WriteOptions, _logger, "onboarding journey");
    }

    // ── Event publishing ───────────────────────────────────────────

    private async void PublishSafe<TEvent>(TEvent @event) where TEvent : IEvent
    {
        try
        {
            await _eventBus.PublishAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish {EventType} — event swallowed",
                typeof(TEvent).Name);
        }
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        _appState.PropertyChanged -= OnAppStatePropertyChanged;
    }

    // ── Defaults ───────────────────────────────────────────────────

    private static string DefaultFilePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "StoreAssistantPro", "Settings", "onboarding-journey.json");

    // ═════════════════════════════════════════════════════════════════
    //  Internal state — in-memory working copy
    // ═════════════════════════════════════════════════════════════════

    private sealed class JourneyState
    {
        public UserExperienceLevel Level { get; set; }
        public int Sessions { get; set; }
        public int TotalWindowOpens { get; set; }
        public int BillingCompleted { get; set; }
        public HashSet<string> DistinctWindows { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    // ═════════════════════════════════════════════════════════════════
    //  Serializable DTO for JSON persistence
    // ═════════════════════════════════════════════════════════════════

    private sealed class JourneyStateDto
    {
        public UserExperienceLevel Level { get; set; }
        public int Sessions { get; set; }
        public int TotalWindowOpens { get; set; }
        public int BillingCompleted { get; set; }
        public string[]? DistinctWindows { get; set; }
    }
}
