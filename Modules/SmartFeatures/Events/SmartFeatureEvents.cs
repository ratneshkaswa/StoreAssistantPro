using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.AI;

namespace StoreAssistantPro.Modules.SmartFeatures.Events;

/// <summary>Published when a new anomaly is detected.</summary>
public sealed class AnomalyDetectedEvent(AnomalyAlert alert) : IEvent
{
    public AnomalyAlert Alert { get; } = alert;
}

/// <summary>Published when reorder suggestions are refreshed.</summary>
public sealed class ReorderSuggestionsReadyEvent(IReadOnlyList<ReorderSuggestion> suggestions) : IEvent
{
    public IReadOnlyList<ReorderSuggestion> Suggestions { get; } = suggestions;
}
