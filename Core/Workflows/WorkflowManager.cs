using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Singleton state machine. Resolves workflows by name, walks through
/// their steps, and drives <see cref="INavigationService"/> via the
/// step's <see cref="WorkflowStep.PageKey"/>.
/// </summary>
public partial class WorkflowManager : ObservableObject, IWorkflowManager
{
    private readonly Dictionary<string, IWorkflow> _workflows = [];
    private readonly INavigationService _navigationService;
    private readonly IAppStateService _appState;
    private int _stepIndex;

    public WorkflowManager(
        IEnumerable<IWorkflow> workflows,
        INavigationService navigationService,
        IAppStateService appState)
    {
        _navigationService = navigationService;
        _appState = appState;

        foreach (var wf in workflows)
            _workflows[wf.Name] = wf;
    }

    // ── Observable state ──

    [ObservableProperty]
    public partial IWorkflow? CurrentWorkflow { get; private set; }

    [ObservableProperty]
    public partial WorkflowStep? CurrentStep { get; private set; }

    public WorkflowContext Context { get; private set; } = new();

    public bool IsRunning => CurrentWorkflow is not null;

    // ── Public API ──

    public async Task StartWorkflowAsync(string workflowName)
    {
        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot start '{workflowName}' — workflow '{CurrentWorkflow!.Name}' is already active.");

        if (!_workflows.TryGetValue(workflowName, out var workflow))
            throw new InvalidOperationException($"No workflow registered with name '{workflowName}'.");

        Context = new WorkflowContext();
        CurrentWorkflow = workflow;
        _stepIndex = 0;

        OnPropertyChanged(nameof(IsRunning));

        await RunCurrentStepAsync();
    }

    public async Task MoveNextAsync()
    {
        if (!IsRunning) return;

        await RunCurrentStepAsync();
    }

    public void CancelWorkflow()
    {
        if (!IsRunning) return;

        var workflow = CurrentWorkflow!;
        var context = Context;

        Reset();

        _ = workflow.OnCancelledAsync(context);
    }

    public async Task CompleteWorkflowAsync()
    {
        if (!IsRunning) return;

        var workflow = CurrentWorkflow!;
        var context = Context;

        Reset();

        await workflow.OnCompletedAsync(context);
    }

    // ── Step execution engine ──

    private async Task RunCurrentStepAsync()
    {
        var workflow = CurrentWorkflow!;

        while (_stepIndex < workflow.Steps.Count)
        {
            var step = workflow.Steps[_stepIndex];
            CurrentStep = step;

            // Navigate if the step declares a page key
            if (step.PageKey is not null)
                _navigationService.NavigateTo(step.PageKey);

            var result = await workflow.ExecuteStepAsync(step, Context);

            switch (result)
            {
                case StepResult.Continue:
                    _stepIndex++;
                    continue;

                case StepResult.Complete:
                    await FinishAsync(workflow);
                    return;

                case StepResult.Cancel:
                    CancelWorkflow();
                    return;

                case StepResult.Retry:
                    return; // stay on current step — ViewModel will call MoveNextAsync again
            }
        }

        // All steps exhausted — complete
        await FinishAsync(workflow);
    }

    private async Task FinishAsync(IWorkflow workflow)
    {
        var context = Context;
        Reset();
        await workflow.OnCompletedAsync(context);
    }

    private void Reset()
    {
        CurrentWorkflow = null;
        CurrentStep = null;
        _stepIndex = 0;
        OnPropertyChanged(nameof(IsRunning));
    }
}
