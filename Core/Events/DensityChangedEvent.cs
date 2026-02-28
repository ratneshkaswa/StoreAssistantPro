using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published after the UI density mode changes. The shell subscribes
/// to this event and re-navigates to the current page so that views
/// pick up the swapped <c>StaticResource</c> token values.
/// </summary>
public sealed record DensityChangedEvent(DensityMode NewMode) : IEvent;
