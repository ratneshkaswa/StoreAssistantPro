using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Touch.Events;

/// <summary>Published when touch/kiosk mode is toggled.</summary>
public sealed class TouchModeChangedEvent(bool isEnabled) : IEvent
{
    public bool IsEnabled { get; } = isEnabled;
}
