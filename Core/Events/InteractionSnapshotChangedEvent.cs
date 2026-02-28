using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published by <see cref="Services.IInteractionTracker"/> when the
/// computed <see cref="InteractionSnapshot"/> changes materially
/// (e.g., operator transitions from idle to rapid input, or vice versa).
/// <para>
/// Published on every tracker tick where any metric crosses a
/// significance threshold — not on every keystroke. Typical cadence
/// is 200 ms when input is active; no events during idle.
/// </para>
/// </summary>
/// <param name="Snapshot">Current interaction metrics.</param>
public sealed record InteractionSnapshotChangedEvent(
    InteractionSnapshot Snapshot) : IEvent;
