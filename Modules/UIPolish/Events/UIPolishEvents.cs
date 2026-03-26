using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.UIPolish;

namespace StoreAssistantPro.Modules.UIPolish.Events;

/// <summary>Published when a long-running operation progresses.</summary>
public sealed class ProgressUpdatedEvent(ProgressState state) : IEvent
{
    public ProgressState State { get; } = state;
}

/// <summary>Published when DPI or scaling changes.</summary>
public sealed class DpiChangedEvent(double newDpi) : IEvent
{
    public double NewDpi { get; } = newDpi;
}
