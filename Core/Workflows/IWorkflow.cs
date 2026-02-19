namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Defines a multi-step user flow. Each concrete workflow declares its
/// ordered steps and implements the logic for each one.
/// <para>
/// <b>Architecture rule:</b> Workflows own the step sequence and logic.
/// ViewModels never drive navigation directly — they call
/// <see cref="IWorkflowManager.MoveNextAsync"/> or
/// <see cref="IWorkflowManager.CancelWorkflow"/> instead.
/// </para>
/// </summary>
public interface IWorkflow
{
    /// <summary>Unique identifier for this workflow.</summary>
    string Name { get; }

    /// <summary>Ordered steps that make up the workflow.</summary>
    IReadOnlyList<WorkflowStep> Steps { get; }

    /// <summary>
    /// Execute the logic for a single step. Returns a <see cref="StepResult"/>
    /// that tells the manager how to proceed.
    /// </summary>
    Task<StepResult> ExecuteStepAsync(WorkflowStep step, WorkflowContext context);

    /// <summary>Called once when the workflow completes successfully.</summary>
    Task OnCompletedAsync(WorkflowContext context);

    /// <summary>Called once when the workflow is cancelled.</summary>
    Task OnCancelledAsync(WorkflowContext context);
}
