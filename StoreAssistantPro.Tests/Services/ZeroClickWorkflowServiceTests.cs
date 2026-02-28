using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace StoreAssistantPro.Tests.Services;

public class ZeroClickWorkflowServiceTests : IDisposable
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IFocusLockService _focusLock = Substitute.For<IFocusLockService>();
    private readonly IWorkflowManager _workflowManager = Substitute.For<IWorkflowManager>();
    private readonly IZeroClickSafetyPolicy _safetyPolicy;
    private readonly IFlowStateEngine _flowStateEngine = Substitute.For<IFlowStateEngine>();
    private readonly IPerformanceMonitor _perf;
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly ILogger<ZeroClickWorkflowService> _logger =
        NullLogger<ZeroClickWorkflowService>.Instance;

    private readonly ZeroClickWorkflowService _sut;

    public ZeroClickWorkflowServiceTests()
    {
        _perf = new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);
        _safetyPolicy = new ZeroClickSafetyPolicy(
            NullLogger<ZeroClickSafetyPolicy>.Instance);
        _regional.Now.Returns(new DateTime(2025, 6, 15, 10, 0, 0));
        _appState.IsOfflineMode.Returns(false);
        _focusLock.IsFocusLocked.Returns(false);
        _workflowManager.IsRunning.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Calm);

        _sut = new ZeroClickWorkflowService(
            _eventBus, _appState, _focusLock,
            _workflowManager, _safetyPolicy, _flowStateEngine,
            _perf, _regional, _logger);
    }

    public void Dispose() => _sut.Dispose();

    // ── Registration ─────────────────────────────────────────────────

    [Fact]
    public void RegisterRule_AddsToRegisteredRuleIds()
    {
        var rule = CreateRule("TestRule");

        _sut.RegisterRule(rule);

        Assert.Contains("TestRule", _sut.RegisteredRuleIds);
    }

    [Fact]
    public void RegisterRule_DuplicateId_Throws()
    {
        _sut.RegisterRule(CreateRule("TestRule"));

        Assert.Throws<InvalidOperationException>(
            () => _sut.RegisterRule(CreateRule("TestRule")));
    }

    [Fact]
    public void RegisterRule_NullRule_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.RegisterRule(null!));
    }

    [Fact]
    public void RegisteredRuleIds_Empty_ByDefault()
    {
        Assert.Empty(_sut.RegisteredRuleIds);
    }

    // ── Enable / Disable ─────────────────────────────────────────────

    [Fact]
    public void DisableRule_AddsToDisabledSet()
    {
        _sut.DisableRule("SomeRule");

        Assert.True(_sut.IsRuleDisabled("SomeRule"));
        Assert.Contains("SomeRule", _sut.DisabledRuleIds);
    }

    [Fact]
    public void EnableRule_RemovesFromDisabledSet()
    {
        _sut.DisableRule("SomeRule");
        _sut.EnableRule("SomeRule");

        Assert.False(_sut.IsRuleDisabled("SomeRule"));
    }

    [Fact]
    public void IsRuleDisabled_UnknownRule_ReturnsFalse()
    {
        Assert.False(_sut.IsRuleDisabled("NonExistentRule"));
    }

    // ── EvaluateAll — High Confidence ────────────────────────────────

    [Fact]
    public async Task EvaluateAll_HighConfidence_ExecutesRule()
    {
        var rule = CreateRule("Auto", ZeroClickConfidence.High);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.Received(1).ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_HighConfidence_PublishesEvent()
    {
        var rule = CreateRule("Auto", ZeroClickConfidence.High);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ZeroClickActionExecutedEvent>(e => e.RuleId == "Auto"));
    }

    // ── EvaluateAll — Below High Confidence ──────────────────────────

    [Fact]
    public async Task EvaluateAll_NoneConfidence_DoesNotExecute()
    {
        var rule = CreateRule("Skip", ZeroClickConfidence.None);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_MediumConfidence_DoesNotExecute()
    {
        var rule = CreateRule("Blocked", ZeroClickConfidence.Medium);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_LowConfidence_DoesNotExecute()
    {
        var rule = CreateRule("Low", ZeroClickConfidence.Low);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
    }

    // ── Safety gates ─────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAll_OfflineMode_SkipsExecution()
    {
        _appState.IsOfflineMode.Returns(true);
        var rule = CreateRule("Offline", ZeroClickConfidence.High);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_WorkflowRunning_SkipsExecution()
    {
        _workflowManager.IsRunning.Returns(true);
        var rule = CreateRule("Busy", ZeroClickConfidence.High);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_FocusLocked_SkipsExecution()
    {
        _focusLock.IsFocusLocked.Returns(true);
        var rule = CreateRule("Locked", ZeroClickConfidence.High);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_DisabledRule_SkipsExecution()
    {
        var rule = CreateRule("Disabled", ZeroClickConfidence.High);
        _sut.RegisterRule(rule);
        _sut.DisableRule("Disabled");

        await _sut.EvaluateAllAsync();

        await rule.DidNotReceive().EvaluateAsync(Arg.Any<CancellationToken>());
        await rule.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
    }

    // ── De-duplication ───────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAll_SameRuleTwiceInOneCycle_ExecutesOnlyOnce()
    {
        var rule = CreateRule("Dedup", ZeroClickConfidence.High);
        _sut.RegisterRule(rule);

        // Simulate calling EvaluateAll twice without a new trigger clearing the guard
        await _sut.EvaluateAllAsync();
        await _sut.EvaluateAllAsync();

        await rule.Received(1).ExecuteAsync(Arg.Any<CancellationToken>());
    }

    // ── Multiple rules ───────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAll_MultipleRules_EvaluatesAll()
    {
        var rule1 = CreateRule("Rule1", ZeroClickConfidence.High);
        var rule2 = CreateRule("Rule2", ZeroClickConfidence.None);
        var rule3 = CreateRule("Rule3", ZeroClickConfidence.High);
        _sut.RegisterRule(rule1);
        _sut.RegisterRule(rule2);
        _sut.RegisterRule(rule3);

        await _sut.EvaluateAllAsync();

        await rule1.Received(1).ExecuteAsync(Arg.Any<CancellationToken>());
        await rule2.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
        await rule3.Received(1).ExecuteAsync(Arg.Any<CancellationToken>());
    }

    // ── Error handling ───────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAll_RuleThrows_ContinuesOtherRules()
    {
        var badRule = CreateRule("Bad", ZeroClickConfidence.High);
        badRule.ExecuteAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("boom")));

        var goodRule = CreateRule("Good", ZeroClickConfidence.High);
        _sut.RegisterRule(badRule);
        _sut.RegisterRule(goodRule);

        await _sut.EvaluateAllAsync();

        await goodRule.Received(1).ExecuteAsync(Arg.Any<CancellationToken>());
    }

    // ── Event subscriptions ──────────────────────────────────────────

    [Fact]
    public void Constructor_SubscribesToDomainEvents()
    {
        _eventBus.Received().Subscribe(Arg.Any<Func<Modules.Authentication.Events.UserLoggedInEvent, Task>>());
        _eventBus.Received().Subscribe(Arg.Any<Func<OperationalModeChangedEvent, Task>>());
        _eventBus.Received().Subscribe(Arg.Any<Func<Modules.Billing.Events.BillingSessionCompletedEvent, Task>>());
        _eventBus.Received().Subscribe(Arg.Any<Func<Modules.Sales.Events.SaleCompletedEvent, Task>>());
        _eventBus.Received().Subscribe(Arg.Any<Func<ConnectionRestoredEvent, Task>>());
    }

    [Fact]
    public void Dispose_UnsubscribesFromAllEvents()
    {
        _sut.Dispose();

        _eventBus.Received().Unsubscribe(Arg.Any<Func<Modules.Authentication.Events.UserLoggedInEvent, Task>>());
        _eventBus.Received().Unsubscribe(Arg.Any<Func<OperationalModeChangedEvent, Task>>());
        _eventBus.Received().Unsubscribe(Arg.Any<Func<Modules.Billing.Events.BillingSessionCompletedEvent, Task>>());
        _eventBus.Received().Unsubscribe(Arg.Any<Func<Modules.Sales.Events.SaleCompletedEvent, Task>>());
        _eventBus.Received().Unsubscribe(Arg.Any<Func<ConnectionRestoredEvent, Task>>());
    }

    // ── ZeroClickEvaluation helpers ──────────────────────────────────

    [Fact]
    public void Evaluation_Skip_ReturnsNoneConfidence()
    {
        var eval = ZeroClickEvaluation.Skip("not applicable");

        Assert.Equal(ZeroClickConfidence.None, eval.Confidence);
        Assert.Equal("not applicable", eval.Description);
    }

    [Fact]
    public void Evaluation_AutoExecute_ReturnsHighConfidence()
    {
        var eval = ZeroClickEvaluation.AutoExecute("auto-navigate");

        Assert.Equal(ZeroClickConfidence.High, eval.Confidence);
    }

    [Fact]
    public void Evaluation_Blocked_ReturnsMediumConfidence()
    {
        var eval = ZeroClickEvaluation.Blocked("focus locked");

        Assert.Equal(ZeroClickConfidence.Medium, eval.Confidence);
    }

    // ── ZeroClickActionExecutedEvent ─────────────────────────────────

    [Fact]
    public void Event_RecordEquality()
    {
        var dt = new DateTime(2025, 6, 15, 10, 0, 0);
        var a = new ZeroClickActionExecutedEvent("R1", "test", dt);
        var b = new ZeroClickActionExecutedEvent("R1", "test", dt);

        Assert.Equal(a, b);
    }

    // ── Safety policy gate ──────────────────────────────────────────

    [Fact]
    public async Task EvaluateAll_DeleteCategory_BlockedBySafetyPolicy()
    {
        var rule = CreateRuleWithCategory("DeleteProduct",
            ZeroClickConfidence.High, ZeroClickActionCategory.Delete);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.DidNotReceive().EvaluateAsync(Arg.Any<CancellationToken>());
        await rule.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_SettingsChangeCategory_BlockedBySafetyPolicy()
    {
        var rule = CreateRuleWithCategory("ChangeTaxRate",
            ZeroClickConfidence.High, ZeroClickActionCategory.SettingsChange);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_FinancialCategory_BlockedBySafetyPolicy()
    {
        var rule = CreateRuleWithCategory("CompleteSale",
            ZeroClickConfidence.High, ZeroClickActionCategory.FinancialConfirmation);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_SecurityCategory_BlockedBySafetyPolicy()
    {
        var rule = CreateRuleWithCategory("ChangePin",
            ZeroClickConfidence.High, ZeroClickActionCategory.SecuritySensitive);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_NavigationCategory_AllowedBySafetyPolicy()
    {
        var rule = CreateRuleWithCategory("AutoNavigate",
            ZeroClickConfidence.High, ZeroClickActionCategory.Navigation);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.Received(1).ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_DataEntryCategory_AllowedBySafetyPolicy()
    {
        // DataEntry requires Focused or Flow state — Calm blocks it
        _flowStateEngine.CurrentState.Returns(FlowState.Focused);
        var rule = CreateRuleWithCategory("AutoAddToCart",
            ZeroClickConfidence.High, ZeroClickActionCategory.DataEntry);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.Received(1).ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_DefaultCategory_ReadOnly_AllowedBySafetyPolicy()
    {
        // Rules that don't override ActionCategory default to ReadOnly
        var rule = CreateRule("LegacyRule", ZeroClickConfidence.High);
        _sut.RegisterRule(rule);

        await _sut.EvaluateAllAsync();

        await rule.Received(1).ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAll_MixedCategories_OnlyAllowedExecute()
    {
        var safeRule = CreateRuleWithCategory("Safe",
            ZeroClickConfidence.High, ZeroClickActionCategory.Navigation);
        var dangerousRule = CreateRuleWithCategory("Dangerous",
            ZeroClickConfidence.High, ZeroClickActionCategory.Delete);

        _sut.RegisterRule(safeRule);
        _sut.RegisterRule(dangerousRule);

        await _sut.EvaluateAllAsync();

        await safeRule.Received(1).ExecuteAsync(Arg.Any<CancellationToken>());
        await dangerousRule.DidNotReceive().ExecuteAsync(Arg.Any<CancellationToken>());
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static IZeroClickRule CreateRule(
        string ruleId,
        ZeroClickConfidence confidence = ZeroClickConfidence.None)
    {
        var rule = Substitute.For<IZeroClickRule>();
        rule.RuleId.Returns(ruleId);
        rule.ActionCategory.Returns(ZeroClickActionCategory.ReadOnly);
        rule.EvaluateAsync(Arg.Any<CancellationToken>())
            .Returns(new ZeroClickEvaluation(confidence, $"{ruleId} description"));
        rule.ExecuteAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return rule;
    }

    private static IZeroClickRule CreateRuleWithCategory(
        string ruleId,
        ZeroClickConfidence confidence,
        ZeroClickActionCategory category)
    {
        var rule = Substitute.For<IZeroClickRule>();
        rule.RuleId.Returns(ruleId);
        rule.ActionCategory.Returns(category);
        rule.EvaluateAsync(Arg.Any<CancellationToken>())
            .Returns(new ZeroClickEvaluation(confidence, $"{ruleId} description"));
        rule.ExecuteAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return rule;
    }
}
