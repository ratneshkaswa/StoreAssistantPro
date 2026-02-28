using System.Collections.Concurrent;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Lightweight service that records discrete user interactions
/// (window opens, billing completions, product creates, arbitrary
/// feature usage) as simple counters — no business logic, no
/// promotion rules, no event publishing.
///
/// <para><b>Design goals:</b></para>
/// <list type="bullet">
///   <item><b>Zero contention</b> — all counter increments use
///         <see cref="System.Threading.Interlocked"/> or
///         <see cref="ConcurrentDictionary{TKey,TValue}"/>. No
///         <c>lock</c> is ever held on the record path.</item>
///   <item><b>Coalesced persistence</b> — changes are flushed to
///         disk on a timer (default 30 s) rather than per-action,
///         keeping I/O overhead near-zero even under heavy usage.</item>
///   <item><b>Read-only queries</b> — consumers (onboarding service,
///         settings UI, diagnostics) read counters without allocation
///         or locking.</item>
/// </list>
///
/// <para><b>Relationship to <see cref="IOnboardingJourneyService"/>:</b></para>
/// <para>
/// The onboarding service owns promotion logic and experience level.
/// This tracker provides the raw telemetry counters that other
/// services (including onboarding) can query to make decisions.
/// Both services are singletons and can be injected independently.
/// </para>
///
/// <para><b>Lifetime:</b> Registered as a <b>singleton</b>.
/// Implements <see cref="IDisposable"/> to stop the flush timer
/// and unsubscribe from events at shutdown.</para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// // Record a named interaction:
/// tracker.RecordWindowOpen("SalesView");
/// tracker.RecordFeatureUsed("KeyboardShortcut.F5");
/// tracker.RecordProductCreated();
///
/// // Query counters:
/// long opens = tracker.GetWindowOpenCount("SalesView");
/// long total = tracker.TotalWindowOpens;
/// long f5    = tracker.GetFeatureUsageCount("KeyboardShortcut.F5");
/// </code>
/// </summary>
public interface IUserInteractionTracker : IDisposable
{
    // ── Recording ──────────────────────────────────────────────────

    /// <summary>
    /// Records that the operator opened or navigated to a window.
    /// Both the per-window counter and the global
    /// <see cref="TotalWindowOpens"/> counter are incremented.
    /// </summary>
    /// <param name="windowName">
    /// Unqualified view/window type name, e.g. <c>"SalesView"</c>.
    /// Case-insensitive.
    /// </param>
    void RecordWindowOpen(string windowName);

    /// <summary>
    /// Records that a billing session completed successfully.
    /// Also auto-detected from
    /// <see cref="IAppStateService.CurrentBillingSession"/>
    /// transitioning to
    /// <see cref="Models.BillingSessionState.Completed"/>.
    /// </summary>
    void RecordBillingCompleted();

    /// <summary>
    /// Records that a product was created. Also auto-detected from
    /// <see cref="Events.TransactionCommittedEvent"/> with scope
    /// <c>"CreateProduct"</c>.
    /// </summary>
    void RecordProductCreated();

    /// <summary>
    /// Records a single use of an arbitrary named feature.
    /// Use a dotted key convention, e.g.
    /// <c>"KeyboardShortcut.F5"</c>, <c>"BulkDiscount.Applied"</c>.
    /// </summary>
    /// <param name="featureKey">
    /// Case-insensitive feature identifier.
    /// </param>
    void RecordFeatureUsed(string featureKey);

    // ── Queries ────────────────────────────────────────────────────

    /// <summary>Total number of window-open events across all windows.</summary>
    long TotalWindowOpens { get; }

    /// <summary>Total number of completed billing sessions.</summary>
    long TotalBillingCompleted { get; }

    /// <summary>Total number of products created.</summary>
    long TotalProductsCreated { get; }

    /// <summary>Number of distinct window names ever opened.</summary>
    int DistinctWindowCount { get; }

    /// <summary>
    /// Returns the open count for a specific window, or <c>0</c>
    /// if the window has never been opened.
    /// </summary>
    long GetWindowOpenCount(string windowName);

    /// <summary>
    /// Returns the usage count for a specific feature, or <c>0</c>
    /// if the feature has never been used.
    /// </summary>
    long GetFeatureUsageCount(string featureKey);

    /// <summary>
    /// Returns a snapshot of all per-window open counts.
    /// Keys are window names; values are open counts.
    /// </summary>
    IReadOnlyDictionary<string, long> GetAllWindowCounts();

    /// <summary>
    /// Returns a snapshot of all per-feature usage counts.
    /// Keys are feature identifiers; values are usage counts.
    /// </summary>
    IReadOnlyDictionary<string, long> GetAllFeatureCounts();

    /// <summary>
    /// Resets all counters to zero and clears the persisted file.
    /// Used by "Reset telemetry" in system settings.
    /// </summary>
    void Reset();
}
