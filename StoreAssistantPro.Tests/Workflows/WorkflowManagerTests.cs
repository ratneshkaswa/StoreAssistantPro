using NSubstitute;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Workflows;

namespace StoreAssistantPro.Tests.Workflows;

public class WorkflowManagerTests
{
    private readonly INavigationService _navService = Substitute.For<INavigationService>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();

    private WorkflowManager CreateSut(params IWorkflow[] workflows) =>
        new(workflows, _navService, _appState);

    // ── Helpers ──

    private static IWorkflow CreateFakeWorkflow(
        string name,
        IReadOnlyList<WorkflowStep> steps,
        Func<WorkflowStep, WorkflowContext, Task<StepResult>> executor)
    {
        var wf = Substitute.For<IWorkflow>();
        wf.Name.Returns(name);
        wf.Steps.Returns(steps);
        wf.ExecuteStepAsync(Arg.Any<WorkflowStep>(), Arg.Any<WorkflowContext>())
            .Returns(ci => executor(ci.Arg<WorkflowStep>(), ci.Arg<WorkflowContext>()));
        wf.OnCompletedAsync(Arg.Any<WorkflowContext>()).Returns(Task.CompletedTask);
        wf.OnCancelledAsync(Arg.Any<WorkflowContext>()).Returns(Task.CompletedTask);
        return wf;
    }

    // ── Tests ──

    [Fact]
    public async Task StartWorkflow_RunsAllSteps_ThenCompletes()
    {
        var executed = new List<string>();
        var steps = new WorkflowStep[]
        {
            new("Step1"),
            new("Step2"),
            new("Step3")
        };

        var wf = CreateFakeWorkflow("Test", steps, (step, _) =>
        {
            executed.Add(step.Key);
            return Task.FromResult(StepResult.Continue);
        });

        var sut = CreateSut(wf);
        await sut.StartWorkflowAsync("Test");

        Assert.Equal(["Step1", "Step2", "Step3"], executed);
        Assert.False(sut.IsRunning);
        await wf.Received(1).OnCompletedAsync(Arg.Any<WorkflowContext>());
    }

    [Fact]
    public async Task StartWorkflow_StepReturnsComplete_StopsEarly()
    {
        var executed = new List<string>();
        var steps = new WorkflowStep[] { new("A"), new("B") };

        var wf = CreateFakeWorkflow("Test", steps, (step, _) =>
        {
            executed.Add(step.Key);
            return Task.FromResult(step.Key == "A" ? StepResult.Complete : StepResult.Continue);
        });

        var sut = CreateSut(wf);
        await sut.StartWorkflowAsync("Test");

        Assert.Equal(["A"], executed);
        Assert.False(sut.IsRunning);
        await wf.Received(1).OnCompletedAsync(Arg.Any<WorkflowContext>());
    }

    [Fact]
    public async Task StartWorkflow_StepReturnsCancel_AbortsCleansUp()
    {
        var steps = new WorkflowStep[] { new("A"), new("B") };

        var wf = CreateFakeWorkflow("Test", steps, (step, _) =>
            Task.FromResult(step.Key == "A" ? StepResult.Cancel : StepResult.Continue));

        var sut = CreateSut(wf);
        await sut.StartWorkflowAsync("Test");

        Assert.False(sut.IsRunning);
        await wf.Received(1).OnCancelledAsync(Arg.Any<WorkflowContext>());
        await wf.DidNotReceive().OnCompletedAsync(Arg.Any<WorkflowContext>());
    }

    [Fact]
    public async Task StartWorkflow_StepReturnsRetry_PausesAtCurrentStep()
    {
        var callCount = 0;
        var steps = new WorkflowStep[] { new("A"), new("B") };

        var wf = CreateFakeWorkflow("Test", steps, (step, _) =>
        {
            callCount++;
            // First call returns Retry, second call (via MoveNextAsync) returns Continue
            return Task.FromResult(step.Key == "A" && callCount == 1
                ? StepResult.Retry
                : StepResult.Continue);
        });

        var sut = CreateSut(wf);
        await sut.StartWorkflowAsync("Test");

        // Should be paused at step A
        Assert.True(sut.IsRunning);
        Assert.Equal("A", sut.CurrentStep?.Key);

        // Now advance
        await sut.MoveNextAsync();

        Assert.False(sut.IsRunning);
    }

    [Fact]
    public async Task StartWorkflow_NavigatesToPageKey_WhenStepHasOne()
    {
        var steps = new WorkflowStep[] { new("A", "PageA"), new("B") };

        var wf = CreateFakeWorkflow("Test", steps, (_, _) =>
            Task.FromResult(StepResult.Continue));

        var sut = CreateSut(wf);
        await sut.StartWorkflowAsync("Test");

        _navService.Received(1).NavigateTo("PageA");
    }

    [Fact]
    public async Task StartWorkflow_ContextSharedAcrossSteps()
    {
        var steps = new WorkflowStep[] { new("Write"), new("Read") };
        string? readValue = null;

        var wf = CreateFakeWorkflow("Test", steps, (step, ctx) =>
        {
            if (step.Key == "Write") ctx.Set("Key", "Hello");
            if (step.Key == "Read") readValue = ctx.Get<string>("Key");
            return Task.FromResult(StepResult.Continue);
        });

        var sut = CreateSut(wf);
        await sut.StartWorkflowAsync("Test");

        Assert.Equal("Hello", readValue);
    }

    [Fact]
    public async Task StartWorkflow_ThrowsIfAlreadyRunning()
    {
        var steps = new WorkflowStep[] { new("A") };

        var wf = CreateFakeWorkflow("Test", steps, (_, _) =>
            Task.FromResult(StepResult.Retry)); // stays paused

        var sut = CreateSut(wf);
        await sut.StartWorkflowAsync("Test");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.StartWorkflowAsync("Test"));
    }

    [Fact]
    public async Task StartWorkflow_ThrowsForUnknownName()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.StartWorkflowAsync("DoesNotExist"));
    }

    [Fact]
    public async Task CancelWorkflow_ResetsState()
    {
        var steps = new WorkflowStep[] { new("A") };

        var wf = CreateFakeWorkflow("Test", steps, (_, _) =>
            Task.FromResult(StepResult.Retry));

        var sut = CreateSut(wf);
        await sut.StartWorkflowAsync("Test");

        Assert.True(sut.IsRunning);

        sut.CancelWorkflow();

        Assert.False(sut.IsRunning);
        Assert.Null(sut.CurrentWorkflow);
        Assert.Null(sut.CurrentStep);
    }

    [Fact]
    public async Task CompleteWorkflowAsync_ForcesCompletion()
    {
        var steps = new WorkflowStep[] { new("A") };

        var wf = CreateFakeWorkflow("Test", steps, (_, _) =>
            Task.FromResult(StepResult.Retry));

        var sut = CreateSut(wf);
        await sut.StartWorkflowAsync("Test");

        await sut.CompleteWorkflowAsync();

        Assert.False(sut.IsRunning);
        await wf.Received(1).OnCompletedAsync(Arg.Any<WorkflowContext>());
    }
}
