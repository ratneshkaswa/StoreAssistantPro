using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton implementation of <see cref="ITipRotationService"/>.
///
/// <para><b>Design goals:</b></para>
/// <list type="bullet">
///   <item><b>Variety</b> — operators don't see the same tip every
///         time they visit a page. A bounded recency ring buffer
///         tracks the last <see cref="RecencyDepth"/> shown tips
///         per window and suppresses them from selection.</item>
///   <item><b>Priority respect</b> — among non-recent candidates the
///         highest-priority tip wins. When all eligible tips have
///         been shown recently, recency is relaxed and the
///         highest-priority candidate is returned anyway (graceful
///         degradation over showing nothing).</item>
///   <item><b>Experience-driven filtering</b> — the effective
///         <see cref="TipLevel"/> ceiling is read from
///         <see cref="IOnboardingJourneyService.CurrentProfile"/>.
///         <see cref="UserExperienceProfile.MaxVisibleTipLevel"/>
///         Beginner operators see only <see cref="TipLevel.Beginner"/>
///         tips; Intermediate operators see up to
///         <see cref="TipLevel.Normal"/>; Advanced operators see
///         all levels.</item>
///   <item><b>Progressive frequency reduction</b> — a per-window
///         cooldown controlled by
///         <see cref="UserExperienceProfile.TipCooldown"/> throttles
///         how often tips appear. Beginners see tips on every visit;
///         Intermediate operators every 5 min; Advanced every 30 min.
///         One-time tips that have never been shown bypass the
///         cooldown.</item>
///   <item><b>Zero external I/O</b> — recency state is in-memory
///         only. Dismiss persistence is delegated to
///         <see cref="ITipStateService"/>. Experience level is
///         owned by <see cref="IOnboardingJourneyService"/>.</item>
/// </list>
///
/// <para><b>Thread safety:</b> A single <see cref="Lock"/> guards
/// all mutable state. The critical section is kept short — no
/// predicate evaluation or I/O happens under the lock.</para>
/// </summary>
public sealed class TipRotationService : ITipRotationService, IDisposable
{
    private readonly ITipRegistryService _registry;
    private readonly ITipStateService _tipState;
    private readonly IOnboardingJourneyService _onboarding;
    private readonly IEventBus _eventBus;
    private readonly ILogger<TipRotationService> _logger;
    private readonly Lock _lock = new();

    // ── Configuration ──────────────────────────────────────────────

    /// <summary>
    /// Number of recently-shown tip IDs to remember per window.
    /// When all eligible tips have been seen, the buffer wraps and
    /// tips become eligible again — ensuring the operator eventually
    /// sees every tip.
    /// </summary>
    internal const int RecencyDepth = 5;

    /// <inheritdoc />
    public int OnboardingSessionThreshold { get; set; } = 3;

    /// <inheritdoc />
    public int SessionCount { get; private set; }

    // ── Per-window rotation state ──────────────────────────────────

    /// <summary>
    /// Tracks the last <see cref="RecencyDepth"/> tip IDs shown for
    /// each window. Implemented as a ring buffer — when it fills, the
    /// oldest entry is overwritten.
    /// </summary>
    private readonly Dictionary<string, RecencyRing> _recency = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Caches the most recently selected tip per window so repeated
    /// calls within the same context don't re-run the pipeline.
    /// Cleared by <see cref="Invalidate"/> / <see cref="InvalidateAll"/>.
    /// </summary>
    private readonly Dictionary<string, TipDefinition?> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tracks the last time a tip was actually shown (returned non-null)
    /// for each window. Used by the cooldown gate to enforce
    /// <see cref="UserExperienceProfile.TipCooldown"/>.
    /// </summary>
    private readonly Dictionary<string, DateTime> _lastShownUtc = new(StringComparer.OrdinalIgnoreCase);

    // ── Constructor ────────────────────────────────────────────────

    public TipRotationService(
        ITipRegistryService registry,
        ITipStateService tipState,
        IOnboardingJourneyService onboarding,
        IEventBus eventBus,
        ILogger<TipRotationService> logger)
    {
        _registry = registry;
        _tipState = tipState;
        _onboarding = onboarding;
        _eventBus = eventBus;
        _logger = logger;

        // When the operator's experience level changes (auto-promotion,
        // manual override, or reset), invalidate all cached tip
        // selections so the next GetNextTip call re-evaluates with
        // the updated MaxVisibleTipLevel.
        _eventBus.Subscribe<ExperienceLevelPromotedEvent>(OnExperienceLevelPromotedAsync);
    }

    // ── Public API ─────────────────────────────────────────────────

    public TipDefinition? GetNextTip(string windowName, HelpContext context)
    {
        ArgumentNullException.ThrowIfNull(windowName);
        ArgumentNullException.ThrowIfNull(context);

        // Fast path: return cached result if still valid.
        lock (_lock)
        {
            if (_cache.TryGetValue(windowName, out var cached))
                return cached;
        }

        // Determine effective max level from the operator's
        // experience profile — Beginner sees only Beginner tips,
        // Intermediate sees Beginner + Normal, Advanced sees all.
        var profile = _onboarding.CurrentProfile;
        var maxLevel = profile.MaxVisibleTipLevel;
        var cooldown = profile.TipCooldown;

        // ── Cooldown gate ──────────────────────────────────────────
        // Progressive frequency reduction: if a tip was shown for
        // this window within the cooldown window, suppress the
        // request. We still need to check whether any unseen one-time
        // tip exists (those bypass cooldown — too important to delay).
        var cooldownActive = false;
        if (cooldown > TimeSpan.Zero)
        {
            lock (_lock)
            {
                if (_lastShownUtc.TryGetValue(windowName, out var lastShown)
                    && (DateTime.UtcNow - lastShown) < cooldown)
                {
                    cooldownActive = true;
                }
            }
        }

        // Delegate heavy filtering to the registry.
        // Registry.Resolve returns the single highest-priority tip
        // that passes window + level + context + dismiss filters.
        // We need *all* eligible candidates to apply recency, so we
        // go through the registry's full list and filter ourselves.
        var allForWindow = _registry.GetTipsForWindow(windowName);
        if (allForWindow.Count == 0)
            return CacheAndReturn(windowName, null);

        // Snapshot the recency ring once — used for both the
        // unseen-one-time check and the fresh/stale split below.
        HashSet<string> recentIds;
        lock (_lock)
        {
            recentIds = _recency.TryGetValue(windowName, out var ring)
                ? ring.ToHashSet()
                : [];
        }

        // Build the eligible candidate list (same pipeline as
        // TipRegistryService.Resolve but without the first-match
        // short-circuit so we can rank by recency).
        var candidates = new List<TipDefinition>();
        var hasUnseenOneTime = false;
        foreach (var tip in allForWindow)
        {
            if (tip.Level > maxLevel)
                continue;

            if (!EvaluateConditionSafe(tip, context))
                continue;

            if (_tipState.IsTipDismissed(tip.TipId))
                continue;

            candidates.Add(tip);

            // Track whether any unseen one-time tip exists —
            // these bypass cooldown because they must be shown
            // at least once (e.g. onboarding tips).
            if (tip.IsOneTime && !recentIds.Contains(tip.TipId))
                hasUnseenOneTime = true;
        }

        if (candidates.Count == 0)
            return CacheAndReturn(windowName, null);

        // If the cooldown is active and no unseen one-time tips
        // exist, suppress this request — return null so the banner
        // stays hidden until the cooldown elapses. Do NOT cache this
        // result — the next call after cooldown expiry must re-evaluate.
        if (cooldownActive && !hasUnseenOneTime)
        {
            _logger.LogDebug(
                "Tip cooldown active for {Window} (level={Level}, cooldown={Cooldown}) — suppressed",
                windowName, profile.Level, cooldown);
            return null;
        }

        var fresh = new List<TipDefinition>();
        var stale = new List<TipDefinition>();

        foreach (var tip in candidates)
        {
            if (recentIds.Contains(tip.TipId))
                stale.Add(tip);
            else
                fresh.Add(tip);
        }

        // Prefer fresh (non-recent) tips, ordered by priority desc.
        // Fall back to stale (recently-shown) tips if all have been
        // seen — this avoids showing nothing when the pool is small.
        // Both lists are already in priority-descending order because
        // GetTipsForWindow returns them sorted by the registry.
        var winner = fresh.Count > 0 ? fresh[0] : stale[0];

        // Record in recency ring + update last-shown timestamp.
        lock (_lock)
        {
            if (!_recency.TryGetValue(windowName, out var ring))
            {
                ring = new RecencyRing(RecencyDepth);
                _recency[windowName] = ring;
            }

            ring.Push(winner.TipId);
            _lastShownUtc[windowName] = DateTime.UtcNow;
        }

        _logger.LogDebug(
            "Tip rotation: {Window} → {TipId} (P{Priority}, fresh={Fresh}, level={Level}, cooldown={Cooldown})",
            windowName, winner.TipId, winner.Priority, fresh.Count > 0, profile.Level, cooldown);

        return CacheAndReturn(windowName, winner);
    }

    public void Invalidate(string windowName)
    {
        lock (_lock)
        {
            _cache.Remove(windowName);
            _lastShownUtc.Remove(windowName);
        }
    }

    public void InvalidateAll()
    {
        lock (_lock)
        {
            _cache.Clear();
            _lastShownUtc.Clear();
        }
    }

    public void RecordDismissal(TipDefinition tip)
    {
        ArgumentNullException.ThrowIfNull(tip);

        _tipState.DismissTip(tip.TipId);

        _logger.LogDebug("Tip dismissed via rotation: {TipId}", tip.TipId);

        Invalidate(tip.WindowName);
    }

    public void NotifySessionStart()
    {
        lock (_lock)
        {
            SessionCount++;
            _cache.Clear();
        }

        _logger.LogDebug(
            "Session start #{Count} — level={Level}, onboarding={IsOnboarding}",
            SessionCount, _onboarding.CurrentProfile.Level,
            _onboarding.CurrentProfile.IsOnboarding);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private TipDefinition? CacheAndReturn(string windowName, TipDefinition? tip)
    {
        lock (_lock)
        {
            _cache[windowName] = tip;
        }

        return tip;
    }

    /// <summary>
    /// Evaluates the tip's <see cref="TipDefinition.ContextCondition"/>
    /// inside a try-catch so a faulty predicate cannot crash the
    /// entire rotation pipeline.
    /// </summary>
    private bool EvaluateConditionSafe(TipDefinition tip, HelpContext context)
    {
        try
        {
            return tip.ContextCondition(context);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ContextCondition for tip {TipId} threw — treating as non-match",
                tip.TipId);
            return false;
        }
    }

    // ── Event subscription ─────────────────────────────────────────

    private Task OnExperienceLevelPromotedAsync(ExperienceLevelPromotedEvent evt)
    {
        _logger.LogDebug(
            "Experience level changed: {Previous} → {New} ({Reason}) — invalidating all tip caches",
            evt.PreviousLevel, evt.NewLevel, evt.PromotionReason);

        InvalidateAll();
        return Task.CompletedTask;
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        _eventBus.Unsubscribe<ExperienceLevelPromotedEvent>(OnExperienceLevelPromotedAsync);
    }

    // ═════════════════════════════════════════════════════════════════
    //  RecencyRing — bounded FIFO ring buffer of tip IDs
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fixed-size ring buffer that tracks the most recently shown
    /// tip IDs for a single window. When the buffer is full, the
    /// oldest entry is silently overwritten.
    /// <para>
    /// This ensures that after <see cref="RecencyDepth"/> rotations,
    /// the oldest tip becomes eligible again — the operator cycles
    /// through the full tip pool rather than getting stuck.
    /// </para>
    /// </summary>
    private sealed class RecencyRing
    {
        private readonly string?[] _buffer;
        private int _head;
        private int _count;

        public RecencyRing(int capacity)
        {
            _buffer = new string?[capacity];
        }

        /// <summary>
        /// Adds a tip ID to the ring, overwriting the oldest entry
        /// if the buffer is full.
        /// </summary>
        public void Push(string tipId)
        {
            _buffer[_head] = tipId;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length)
                _count++;
        }

        /// <summary>
        /// Returns a snapshot of all tip IDs currently in the ring
        /// as a case-insensitive <see cref="HashSet{T}"/>.
        /// </summary>
        public HashSet<string> ToHashSet()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < _count; i++)
            {
                var id = _buffer[i];
                if (id is not null)
                    set.Add(id);
            }

            return set;
        }

        /// <summary>
        /// Returns <c>true</c> if the ring contains
        /// <paramref name="tipId"/>.
        /// </summary>
        public bool Contains(string tipId)
        {
            for (var i = 0; i < _count; i++)
            {
                if (string.Equals(_buffer[i], tipId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
