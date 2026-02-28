namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Represents one discrete step within a workflow.
/// Each step has a unique key and an optional page key that the
/// <see cref="IWorkflowManager"/> uses to drive navigation.
/// </summary>
public sealed record WorkflowStep(string Key, string? PageKey = null);
