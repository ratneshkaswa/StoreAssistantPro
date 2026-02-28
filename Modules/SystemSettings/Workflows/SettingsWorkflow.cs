using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Workflows;

namespace StoreAssistantPro.Modules.SystemSettings.Workflows;

/// <summary>
/// Controls the settings flow:
/// 1. Open settings dialog
/// 2. (Edit + validate handled inside the dialog)
/// 3. Close → complete
/// Placeholder steps for master PIN validation can be added here.
/// </summary>
public class SettingsWorkflow(IDialogService dialogService) : IWorkflow
{
    public const string WorkflowName = "Settings";

    public const string SettingsDialog = "SystemSettings";

    private static readonly WorkflowStep OpenSettings = new("OpenSettings");

    public string Name => WorkflowName;

    public IReadOnlyList<WorkflowStep> Steps { get; } = [OpenSettings];

    public Task<StepResult> ExecuteStepAsync(WorkflowStep step, WorkflowContext context)
    {
        return step.Key switch
        {
            "OpenSettings" => Task.FromResult(ShowSettingsDialog()),
            _ => Task.FromResult(StepResult.Continue)
        };
    }

    private StepResult ShowSettingsDialog()
    {
        dialogService.ShowDialog(SettingsDialog);
        return StepResult.Complete;
    }

    public Task OnCompletedAsync(WorkflowContext context) => Task.CompletedTask;

    public Task OnCancelledAsync(WorkflowContext context) => Task.CompletedTask;
}
