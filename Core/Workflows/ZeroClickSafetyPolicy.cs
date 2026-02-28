using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Singleton implementation of <see cref="IZeroClickSafetyPolicy"/>.
/// <para>
/// Maintains a hardcoded set of action categories that are permanently
/// forbidden from zero-click execution, plus a runtime-modifiable set
/// for additional restrictions.
/// </para>
/// </summary>
public sealed class ZeroClickSafetyPolicy : IZeroClickSafetyPolicy
{
    private readonly ILogger<ZeroClickSafetyPolicy> _logger;
    private readonly Lock _lock = new();

    /// <summary>
    /// Permanently forbidden categories. These cannot be unblocked
    /// at runtime, by configuration, or by any code path.
    /// </summary>
    private static readonly HashSet<ZeroClickActionCategory> s_hardcoded =
    [
        ZeroClickActionCategory.Delete,
        ZeroClickActionCategory.SettingsChange,
        ZeroClickActionCategory.FinancialConfirmation,
        ZeroClickActionCategory.SecuritySensitive
    ];

    /// <summary>
    /// Additional runtime-blocked categories (e.g., from System Settings).
    /// </summary>
    private readonly HashSet<ZeroClickActionCategory> _runtimeBlocked = [];

    public ZeroClickSafetyPolicy(ILogger<ZeroClickSafetyPolicy> logger)
    {
        _logger = logger;
    }

    // ── IZeroClickSafetyPolicy ───────────────────────────────────────

    public IReadOnlySet<ZeroClickActionCategory> HardcodedBlockedCategories =>
        s_hardcoded;

    public IReadOnlySet<ZeroClickActionCategory> BlockedCategories
    {
        get
        {
            lock (_lock)
            {
                var all = new HashSet<ZeroClickActionCategory>(s_hardcoded);
                all.UnionWith(_runtimeBlocked);
                return all;
            }
        }
    }

    public ZeroClickSafetyVerdict Evaluate(
        string ruleId, ZeroClickActionCategory category)
    {
        // Hardcoded block — non-negotiable
        if (s_hardcoded.Contains(category))
        {
            var reason = category switch
            {
                ZeroClickActionCategory.Delete =>
                    "Delete actions require manual confirmation — destructive and irreversible.",
                ZeroClickActionCategory.SettingsChange =>
                    "Settings changes require manual confirmation — affects application configuration.",
                ZeroClickActionCategory.FinancialConfirmation =>
                    "Financial confirmations require manual confirmation — completes monetary transactions.",
                ZeroClickActionCategory.SecuritySensitive =>
                    "Security-sensitive actions require manual confirmation — affects access control.",
                _ =>
                    $"Category {category} is permanently blocked from zero-click execution."
            };

            _logger.LogDebug(
                "ZeroClick safety: BLOCKED rule '{RuleId}' — {Category}: {Reason}",
                ruleId, category, reason);

            return ZeroClickSafetyVerdict.Block(category, ruleId, reason);
        }

        // Runtime block
        bool runtimeBlocked;
        lock (_lock) runtimeBlocked = _runtimeBlocked.Contains(category);

        if (runtimeBlocked)
        {
            var reason = $"Category {category} is blocked by runtime policy.";

            _logger.LogDebug(
                "ZeroClick safety: BLOCKED rule '{RuleId}' — {Category}: {Reason}",
                ruleId, category, reason);

            return ZeroClickSafetyVerdict.Block(category, ruleId, reason);
        }

        // Allowed
        return ZeroClickSafetyVerdict.Allow(category, ruleId,
            $"Category {category} is permitted for zero-click execution.");
    }

    public bool IsBlocked(ZeroClickActionCategory category)
    {
        if (s_hardcoded.Contains(category))
            return true;

        lock (_lock) return _runtimeBlocked.Contains(category);
    }

    public void BlockCategory(ZeroClickActionCategory category)
    {
        lock (_lock) _runtimeBlocked.Add(category);

        _logger.LogInformation(
            "ZeroClick safety: runtime-blocked category {Category}.", category);
    }

    public void UnblockCategory(ZeroClickActionCategory category)
    {
        // Cannot unblock hardcoded categories
        if (s_hardcoded.Contains(category))
        {
            _logger.LogWarning(
                "ZeroClick safety: cannot unblock hardcoded category {Category}.", category);
            return;
        }

        lock (_lock) _runtimeBlocked.Remove(category);

        _logger.LogInformation(
            "ZeroClick safety: runtime-unblocked category {Category}.", category);
    }
}
