using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published by <see cref="Services.IPredictiveFocusService"/> whenever a
/// new <see cref="FocusHint"/> is produced. XAML-side behaviors subscribe
/// to execute the focus transition in the visual tree.
/// </summary>
public sealed record FocusHintChangedEvent(FocusHint Hint) : IEvent;
