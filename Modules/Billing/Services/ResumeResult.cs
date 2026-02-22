using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Encapsulates the full outcome of the billing resume flow.
/// <para>
/// Produced by <see cref="IBillingResumeService.TryResumeAsync"/> after
/// the operator chooses to resume or discard (or no session is found).
/// </para>
/// </summary>
public sealed class ResumeResult
{
    /// <summary>How the resume flow ended.</summary>
    public ResumeOutcome Outcome { get; init; }

    /// <summary>
    /// The persisted session entity. <c>null</c> when
    /// <see cref="Outcome"/> is <see cref="ResumeOutcome.NoSession"/>.
    /// </summary>
    public BillingSession? Session { get; init; }

    /// <summary>
    /// Validated and recalculated cart returned by
    /// <see cref="IBillingSessionRestoreService"/>. <c>null</c> when
    /// no restore was performed or the cart data was unrecoverable.
    /// </summary>
    public RestoredCart? RestoredCart { get; init; }

    /// <summary>
    /// <c>true</c> when mode = Billing and focus lock = acquired after
    /// the resume completed. <c>false</c> signals a broken event chain.
    /// </summary>
    public bool IsModeAndFocusLockActive { get; init; }

    /// <summary>Shorthand: the user resumed and the mode/lock are healthy.</summary>
    public bool IsFullyResumed =>
        Outcome == ResumeOutcome.Resumed && IsModeAndFocusLockActive;

    // ── Factory helpers ────────────────────────────────────────────

    public static ResumeResult NoSession() => new()
    {
        Outcome = ResumeOutcome.NoSession
    };

    public static ResumeResult Discarded(BillingSession session) => new()
    {
        Outcome = ResumeOutcome.Discarded,
        Session = session
    };

    public static ResumeResult Resumed(
        BillingSession session,
        RestoredCart? restoredCart,
        bool modeAndFocusLockActive) => new()
    {
        Outcome = ResumeOutcome.Resumed,
        Session = session,
        RestoredCart = restoredCart,
        IsModeAndFocusLockActive = modeAndFocusLockActive
    };
}

/// <summary>How the billing resume flow ended.</summary>
public enum ResumeOutcome
{
    /// <summary>No persisted active session was found.</summary>
    NoSession,

    /// <summary>Session found — operator chose to resume.</summary>
    Resumed,

    /// <summary>Session found — operator chose to discard.</summary>
    Discarded
}
