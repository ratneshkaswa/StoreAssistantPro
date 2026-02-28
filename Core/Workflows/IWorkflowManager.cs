using System.ComponentModel;

namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Singleton state machine that drives the application's user flows.
/// ViewModels call <see cref="MoveNextAsync"/>/<see cref="CancelWorkflow"/>
/// to advance; the manager updates <see cref="CurrentWorkflow"/>,
/// <see cref="CurrentStep"/>, and triggers navigation via page keys.
/// </summary>
public interface IWorkflowManager : INotifyPropertyChanged
{
    /// <summary>The currently active workflow, or <c>null</c> if idle.</summary>
    IWorkflow? CurrentWorkflow { get; }

    /// <summary>The step the active workflow is on, or <c>null</c>.</summary>
    WorkflowStep? CurrentStep { get; }

    /// <summary>Shared context for the active workflow execution.</summary>
    WorkflowContext Context { get; }

    /// <summary>Whether any workflow is currently running.</summary>
    bool IsRunning { get; }

    /// <summary>Start a named workflow. Throws if one is already running.</summary>
    Task StartWorkflowAsync(string workflowName);

    /// <summary>Execute the current step and advance.</summary>
    Task MoveNextAsync();

    /// <summary>Cancel the active workflow.</summary>
    void CancelWorkflow();

    /// <summary>Force-complete the active workflow from external code.</summary>
    Task CompleteWorkflowAsync();
}
