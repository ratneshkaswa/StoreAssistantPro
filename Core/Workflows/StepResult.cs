namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// The result of executing a single workflow step.
/// Tells the <see cref="IWorkflowManager"/> how to proceed.
/// </summary>
public enum StepResult
{
    /// <summary>Step completed — advance to the next step.</summary>
    Continue,

    /// <summary>The entire workflow completed successfully.</summary>
    Complete,

    /// <summary>The user or logic cancelled — abort the workflow.</summary>
    Cancel,

    /// <summary>Stay on the current step (e.g., validation failure).</summary>
    Retry
}
