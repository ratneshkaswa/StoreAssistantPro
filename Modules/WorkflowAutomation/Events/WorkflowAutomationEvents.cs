using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.Workflows;

namespace StoreAssistantPro.Modules.WorkflowAutomation.Events;

/// <summary>Published when an automation rule is triggered and executed.</summary>
public sealed class AutomationRuleExecutedEvent(AutomationResult result) : IEvent
{
    public AutomationResult Result { get; } = result;
}

/// <summary>Published when an automation rule is created or updated.</summary>
public sealed class AutomationRuleSavedEvent(AutomationRule rule) : IEvent
{
    public AutomationRule Rule { get; } = rule;
}
