using System.ComponentModel;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Lightweight singleton that tracks operator interaction frequency
/// for flow-state detection. Feeds computed metrics to
/// <see cref="IFlowStateEngine"/> via <see cref="INotifyPropertyChanged"/>
/// and <see cref="Events.InteractionSnapshotChangedEvent"/>.
///
/// <para><b>Signal sources (call from UI thread or XAML behaviors):</b></para>
/// <list type="table">
///   <listheader>
///     <term>Method</term>
///     <description>When to call</description>
///   </listheader>
///   <item>
///     <term><see cref="RecordKeyPress"/></term>
///     <description>Every <c>PreviewKeyDown</c> on the main window
///     (via <c>KeyboardNav</c> or a thin behavior).</description>
///   </item>
///   <item>
///     <term><see cref="RecordMouseMove"/></term>
///     <description>Coalesced <c>PreviewMouseMove</c> — the behavior
///     should throttle to ≤ 5 Hz to avoid flooding.</description>
///   </item>
///   <item>
///     <term><see cref="RecordBillingAction"/></term>
///     <description>Each discrete billing operation (cart add,
///     quantity change, discount applied, payment step).</description>
///   </item>
/// </list>
///
/// <para><b>Performance contract:</b></para>
/// <list type="bullet">
///   <item><b>Record path</b> — single <c>Interlocked.Increment</c>
///         + one <c>Environment.TickCount64</c> read. Zero heap
///         allocation, no lock.</item>
///   <item><b>Computation</b> — a <see cref="System.Threading.Timer"/>
///         ticks every <see cref="InteractionTracker.TickIntervalMs"/> ms.
///         The callback computes the snapshot and publishes only when
///         metrics cross a significance threshold.</item>
///   <item><b>Idle optimization</b> — the timer self-disables after
///         <see cref="InteractionTracker.MaxIdleSeconds"/> seconds of
///         inactivity and re-enables on the next <c>Record*</c> call.</item>
/// </list>
///
/// <para><b>Architecture:</b></para>
/// <list type="bullet">
///   <item>Registered as a <b>singleton</b>.</item>
///   <item>No WPF dependency — pure service logic.</item>
///   <item>Thread-safe via <c>Interlocked</c> on the hot path and
///         <c>Lock</c> only in the timer callback.</item>
/// </list>
/// </summary>
public interface IInteractionTracker : INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// The most recently computed interaction snapshot.
    /// Updated by the internal timer tick.
    /// </summary>
    InteractionSnapshot CurrentSnapshot { get; }

    /// <summary>
    /// Record a single keyboard event. Call from
    /// <c>PreviewKeyDown</c> handler. Lock-free, zero-allocation.
    /// </summary>
    void RecordKeyPress();

    /// <summary>
    /// Record a coalesced mouse-move event. The caller should
    /// throttle to ≤ 5 Hz. Lock-free, zero-allocation.
    /// </summary>
    void RecordMouseMove();

    /// <summary>
    /// Record a discrete billing-domain action (cart add, qty change,
    /// discount, payment step). Lock-free, zero-allocation.
    /// </summary>
    void RecordBillingAction();

    /// <summary>
    /// Forces an immediate snapshot recomputation. Normally called
    /// by the internal timer, but exposed for testing.
    /// </summary>
    void Tick();
}
