using Microsoft.Extensions.Logging;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Thread-safe, singleton implementation of <see cref="ITipRegistryService"/>.
///
/// <para><b>Storage:</b> Tips are grouped by
/// <see cref="TipDefinition.WindowName"/> in a dictionary of sorted
/// lists. Each per-window list is pre-sorted by
/// <see cref="TipDefinition.Priority"/> descending at registration
/// time so <see cref="Resolve"/> can short-circuit on the first
/// eligible hit — no per-call sorting needed.</para>
///
/// <para><b>Thread safety:</b> A <see cref="Lock"/> guards all
/// mutations (<see cref="Register(TipDefinition)"/>). Read paths
/// (<see cref="Resolve"/>, <see cref="GetTipsForWindow"/>,
/// <see cref="GetAll"/>) snapshot the reference under the lock and
/// iterate outside it. After startup completes, the registry is
/// effectively read-only — the lock is uncontended.</para>
///
/// <para><b>Dismiss filtering:</b> <see cref="Resolve"/> delegates
/// to <see cref="ITipStateService.IsTipDismissed"/> at query time
/// so dismissals are always reflected immediately without cache
/// invalidation.</para>
/// </summary>
public sealed class TipRegistryService : ITipRegistryService
{
    private readonly ITipStateService _tipState;
    private readonly ILogger<TipRegistryService> _logger;
    private readonly Lock _lock = new();

    /// <summary>
    /// Tips grouped by <see cref="TipDefinition.WindowName"/>.
    /// Each list is kept sorted by <see cref="TipDefinition.Priority"/>
    /// descending so <see cref="Resolve"/> returns the first match.
    /// </summary>
    private readonly Dictionary<string, List<TipDefinition>> _byWindow = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Guards against duplicate <see cref="TipDefinition.TipId"/>
    /// registrations (first-registration-wins).
    /// </summary>
    private readonly HashSet<string> _knownIds = new(StringComparer.OrdinalIgnoreCase);

    // ── Constructor ────────────────────────────────────────────────

    public TipRegistryService(
        ITipStateService tipState,
        ILogger<TipRegistryService> logger)
    {
        _tipState = tipState;
        _logger = logger;
    }

    // ── Registration ───────────────────────────────────────────────

    public void Register(TipDefinition tip)
    {
        ArgumentNullException.ThrowIfNull(tip);

        lock (_lock)
        {
            if (!_knownIds.Add(tip.TipId))
            {
                _logger.LogDebug(
                    "Tip {TipId} already registered — skipped",
                    tip.TipId);
                return;
            }

            if (!_byWindow.TryGetValue(tip.WindowName, out var list))
            {
                list = [];
                _byWindow[tip.WindowName] = list;
            }

            // Insert in priority-descending order so Resolve can
            // iterate linearly and return the first match.
            var index = list.FindIndex(t => t.Priority < tip.Priority);
            if (index < 0)
                list.Add(tip);
            else
                list.Insert(index, tip);
        }

        _logger.LogDebug(
            "Tip registered: {TipId} → {Window} (P{Priority}, {Level})",
            tip.TipId, tip.WindowName, tip.Priority, tip.Level);
    }

    public void Register(IEnumerable<TipDefinition> tips)
    {
        ArgumentNullException.ThrowIfNull(tips);

        foreach (var tip in tips)
            Register(tip);
    }

    // ── Queries ────────────────────────────────────────────────────

    public IReadOnlyList<TipDefinition> GetTipsForWindow(string windowName)
    {
        lock (_lock)
        {
            if (_byWindow.TryGetValue(windowName, out var list))
                return list.ToArray();
        }

        return [];
    }

    public IReadOnlyList<TipDefinition> GetAll()
    {
        lock (_lock)
        {
            var count = 0;
            foreach (var list in _byWindow.Values)
                count += list.Count;

            var result = new TipDefinition[count];
            var offset = 0;
            foreach (var list in _byWindow.Values)
            {
                list.CopyTo(result, offset);
                offset += list.Count;
            }

            return result;
        }
    }

    // ── Resolution ─────────────────────────────────────────────────

    public TipDefinition? Resolve(string windowName, TipLevel maxLevel, HelpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Snapshot the per-window list under the lock.
        List<TipDefinition>? candidates;
        lock (_lock)
        {
            if (!_byWindow.TryGetValue(windowName, out candidates))
                return null;

            // Snapshot to avoid holding the lock during predicate evaluation.
            candidates = [.. candidates];
        }

        // List is pre-sorted by Priority descending —
        // first match through the filter pipeline wins.
        foreach (var tip in candidates)
        {
            // 1. Level filter
            if (tip.Level > maxLevel)
                continue;

            // 2. Context filter
            if (!EvaluateConditionSafe(tip, context))
                continue;

            // 3. Dismiss filter
            if (_tipState.IsTipDismissed(tip.TipId))
                continue;

            return tip;
        }

        return null;
    }

    // ── Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates the tip's <see cref="TipDefinition.ContextCondition"/>
    /// inside a try-catch so a faulty predicate cannot crash the
    /// entire resolution pipeline.
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
}
