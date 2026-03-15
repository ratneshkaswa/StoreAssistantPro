using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

public class CalmUIServiceTests
{
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IFocusLockService _focusLock = Substitute.For<IFocusLockService>();
    private readonly IFlowStateEngine _flowStateEngine = Substitute.For<IFlowStateEngine>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private CalmUIService CreateSut()
    {
        return new CalmUIService(_appState, _focusLock, _flowStateEngine, _eventBus);
    }

    // ── Initial state ──────────────────────────────────────────────

    [Fact]
    public void InitialState_ManagementMode_CalmDisabled()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();

        Assert.False(sut.CalmModeEnabled);
        Assert.Equal(WorkspaceZone.Content, sut.ActiveZone);
    }

    // ── Emphasis matrix ────────────────────────────────────────────

    [Fact]
    public void Management_AllZonesGetFullEmphasis()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();

        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.MenuBar));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.Toolbar));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.Content));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.StatusBar));
    }

    [Fact]
    public void BillingUnlocked_ChromeGetsMuted_ContentGetsFull()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();

        Assert.True(sut.CalmModeEnabled);
        Assert.Equal(EmphasisLevel.Muted, sut.GetEmphasis(WorkspaceZone.MenuBar));
        Assert.Equal(EmphasisLevel.Muted, sut.GetEmphasis(WorkspaceZone.Toolbar));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.Content));
        Assert.Equal(EmphasisLevel.Muted, sut.GetEmphasis(WorkspaceZone.StatusBar));
    }

    [Fact]
    public void BillingLocked_ChromeGetsReceded_ContentGetsFull()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(true);

        var sut = CreateSut();

        Assert.True(sut.CalmModeEnabled);
        Assert.Equal(EmphasisLevel.Receded, sut.GetEmphasis(WorkspaceZone.MenuBar));
        Assert.Equal(EmphasisLevel.Receded, sut.GetEmphasis(WorkspaceZone.Toolbar));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.Content));
        Assert.Equal(EmphasisLevel.Receded, sut.GetEmphasis(WorkspaceZone.StatusBar));
    }

    // ── Reactive updates ───────────────────────────────────────────

    [Fact]
    public void WhenModeChanges_RecomputesAndPublishesEvent()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        Assert.False(sut.CalmModeEnabled);

        // Simulate mode change to Billing
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));

        Assert.True(sut.CalmModeEnabled);
        _eventBus.Received().PublishAsync(Arg.Any<CalmStateChangedEvent>());
    }

    [Fact]
    public void WhenFocusLockChanges_RecomputesEmphasis()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        Assert.Equal(EmphasisLevel.Muted, sut.GetEmphasis(WorkspaceZone.MenuBar));

        // Lock focus
        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        Assert.Equal(EmphasisLevel.Receded, sut.GetEmphasis(WorkspaceZone.MenuBar));
    }

    // ── Manual override ────────────────────────────────────────────

    [Fact]
    public void ManualOverride_ForcesCalm()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        Assert.False(sut.CalmModeEnabled);

        sut.SetCalmModeEnabled(true);

        Assert.True(sut.CalmModeEnabled);
        Assert.Equal(EmphasisLevel.Muted, sut.GetEmphasis(WorkspaceZone.Toolbar));
    }

    [Fact]
    public void ManualOverride_Null_ReturnsToAutomatic()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        sut.SetCalmModeEnabled(false);
        Assert.False(sut.CalmModeEnabled);

        sut.SetCalmModeEnabled(null);
        Assert.True(sut.CalmModeEnabled); // auto: billing → calm
    }

    // ── Content zone always Full ───────────────────────────────────

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ContentZone_AlwaysFull(bool isBilling, bool isLocked)
    {
        _appState.CurrentMode.Returns(isBilling ? OperationalMode.Billing : OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(isLocked);

        var sut = CreateSut();

        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.Content));
    }

    // ── No spurious events ─────────────────────────────────────────

    [Fact]
    public void IrrelevantPropertyChange_DoesNotPublish()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        _eventBus.ClearReceivedCalls();

        // Fire an irrelevant property change
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.FirmName)));

        _eventBus.DidNotReceive().PublishAsync(Arg.Any<CalmStateChangedEvent>());
    }

    // ── ChromeEmphasis integration (mirrors MainViewModel.ChromeEmphasis) ──

    /// <summary>
    /// Validates the ChromeEmphasis integer output that CalmTransition
    /// consumes, using the same logic as MainViewModel.ChromeEmphasis.
    /// 0 = Full, 1 = Muted, 2 = Receded.
    /// Now delegates to <see cref="ICalmUIService.GetEmphasis"/> which
    /// accounts for mode, focus lock, and flow state.
    /// </summary>
    private static int ComputeChromeEmphasis(ICalmUIService calmUI, IFocusLockService _)
    {
        return calmUI.GetEmphasis(WorkspaceZone.MenuBar) switch
        {
            EmphasisLevel.Receded => 2,
            EmphasisLevel.Muted => 1,
            _ => 0
        };
    }

    [Fact]
    public void ChromeEmphasis_Management_Returns0()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        Assert.Equal(0, ComputeChromeEmphasis(sut, _focusLock));
    }

    [Fact]
    public void ChromeEmphasis_BillingUnlocked_Returns1()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        Assert.Equal(1, ComputeChromeEmphasis(sut, _focusLock));
    }

    [Fact]
    public void ChromeEmphasis_BillingLocked_Returns2()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(true);

        var sut = CreateSut();
        Assert.Equal(2, ComputeChromeEmphasis(sut, _focusLock));
    }

    [Fact]
    public void ChromeEmphasis_TransitionsCorrectly_OnModeChange()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        Assert.Equal(0, ComputeChromeEmphasis(sut, _focusLock));

        // Switch to billing
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));

        Assert.Equal(1, ComputeChromeEmphasis(sut, _focusLock));

        // Lock focus
        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        Assert.Equal(2, ComputeChromeEmphasis(sut, _focusLock));

        // Unlock focus (back to muted)
        _focusLock.IsFocusLocked.Returns(false);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        Assert.Equal(1, ComputeChromeEmphasis(sut, _focusLock));

        // Back to management
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));

        Assert.Equal(0, ComputeChromeEmphasis(sut, _focusLock));
    }

    // ── Calm opacity mapping (mirrors BillingDimBehavior.CalmEmphasis) ──

    /// <summary>
    /// Maps ChromeEmphasis to the calm opacity values used by
    /// BillingDimBehavior.CalmEmphasis. Validates no harsh dimming.
    /// Full=1.0, Muted=0.82, Receded=0.65.
    /// </summary>
    private static double ComputeCalmOpacity(int chromeEmphasis) => chromeEmphasis switch
    {
        2 => 0.65,
        1 => 0.82,
        _ => 1.0
    };

    [Theory]
    [InlineData(OperationalMode.Management, false, 1.0)]
    [InlineData(OperationalMode.Billing, false, 0.82)]
    [InlineData(OperationalMode.Billing, true, 0.65)]
    public void CalmOpacity_MatchesEmphasis(
        OperationalMode mode, bool locked, double expectedOpacity)
    {
        _appState.CurrentMode.Returns(mode);
        _focusLock.IsFocusLocked.Returns(locked);

        var sut = CreateSut();
        var emphasis = ComputeChromeEmphasis(sut, _focusLock);
        var opacity = ComputeCalmOpacity(emphasis);

        Assert.Equal(expectedOpacity, opacity, precision: 2);
    }

    [Fact]
    public void CalmOpacity_NeverBelow_065()
    {
        // Verify the minimum opacity is 0.65 (Receded), never the
        // harsh 0.45 that the old binary dim used.
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(true);

        var sut = CreateSut();
        var emphasis = ComputeChromeEmphasis(sut, _focusLock);
        var opacity = ComputeCalmOpacity(emphasis);

        Assert.True(opacity >= 0.65,
            $"Calm opacity {opacity} is below 0.65 — too harsh for calm mode");
    }

    [Fact]
    public void CalmOpacity_FullLifecycle_NoHarshDimming()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        var opacities = new List<double>();

        // Management → Full
        opacities.Add(ComputeCalmOpacity(ComputeChromeEmphasis(sut, _focusLock)));

        // → Billing unlocked → Muted
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));
        opacities.Add(ComputeCalmOpacity(ComputeChromeEmphasis(sut, _focusLock)));

        // → Billing locked → Receded
        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));
        opacities.Add(ComputeCalmOpacity(ComputeChromeEmphasis(sut, _focusLock)));

        // → Unlock → Muted
        _focusLock.IsFocusLocked.Returns(false);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));
        opacities.Add(ComputeCalmOpacity(ComputeChromeEmphasis(sut, _focusLock)));

        // → Management → Full
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));
        opacities.Add(ComputeCalmOpacity(ComputeChromeEmphasis(sut, _focusLock)));

        Assert.Equal([1.0, 0.82, 0.65, 0.82, 1.0], opacities);
    }

    // ── IsCalmAutomatic (settings toggle support) ──────────────────

    [Fact]
    public void IsCalmAutomatic_InitiallyTrue()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();

        Assert.True(sut.IsCalmAutomatic);
    }

    [Fact]
    public void IsCalmAutomatic_FalseAfterManualOverride()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        sut.SetCalmModeEnabled(false);

        Assert.False(sut.IsCalmAutomatic);
    }

    [Fact]
    public void IsCalmAutomatic_TrueAfterClearingOverride()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        sut.SetCalmModeEnabled(false);
        Assert.False(sut.IsCalmAutomatic);

        sut.SetCalmModeEnabled(null);
        Assert.True(sut.IsCalmAutomatic);
    }

    // ── Settings toggle simulation ─────────────────────────────────

    [Fact]
    public void SettingsToggle_DisableThenEnable_RestoresAutomatic()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        Assert.True(sut.CalmModeEnabled);
        Assert.True(sut.IsCalmAutomatic);

        // User disables Calm UI in settings
        sut.SetCalmModeEnabled(false);
        Assert.False(sut.CalmModeEnabled);
        Assert.False(sut.IsCalmAutomatic);

        // All zones should be Full when disabled
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.MenuBar));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.Toolbar));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.StatusBar));

        // User re-enables Calm UI in settings
        sut.SetCalmModeEnabled(null);
        Assert.True(sut.CalmModeEnabled); // auto: billing → calm
        Assert.True(sut.IsCalmAutomatic);

        // Chrome should be Muted again
        Assert.Equal(EmphasisLevel.Muted, sut.GetEmphasis(WorkspaceZone.MenuBar));
    }

    [Fact]
    public void SettingsToggle_DisablePublishesEvent()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);

        var sut = CreateSut();
        _eventBus.ClearReceivedCalls();

        sut.SetCalmModeEnabled(false);

        _eventBus.Received(1).PublishAsync(
            Arg.Is<CalmStateChangedEvent>(e => !e.CalmModeEnabled));
    }

    // ═══════════════════════════════════════════════════════════════
    // FlowState Integration
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void FlowState_InitiallyCalm()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Calm);

        var sut = CreateSut();

        Assert.Equal(FlowState.Calm, sut.CurrentFlowState);
    }

    [Fact]
    public void FlowState_Calm_ManagementMode_AllFull()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Calm);

        var sut = CreateSut();

        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.MenuBar));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.Toolbar));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.Content));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.StatusBar));
    }

    [Fact]
    public void FlowState_Focused_BillingUnlocked_ChromeStaysMuted()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Focused);

        var sut = CreateSut();

        Assert.Equal(EmphasisLevel.Muted, sut.GetEmphasis(WorkspaceZone.MenuBar));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.Content));
    }

    [Fact]
    public void FlowState_Flow_BillingUnlocked_ChromeIntensifiesToReceded()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);

        var sut = CreateSut();

        // Flow → Muted intensifies to Receded for max noise reduction
        Assert.Equal(EmphasisLevel.Receded, sut.GetEmphasis(WorkspaceZone.MenuBar));
        Assert.Equal(EmphasisLevel.Receded, sut.GetEmphasis(WorkspaceZone.Toolbar));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.Content));
        Assert.Equal(EmphasisLevel.Receded, sut.GetEmphasis(WorkspaceZone.StatusBar));
    }

    [Fact]
    public void FlowState_Flow_BillingLocked_ChromeStaysReceded()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(true);
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);

        var sut = CreateSut();

        // Already Receded from lock — Flow doesn't change it
        Assert.Equal(EmphasisLevel.Receded, sut.GetEmphasis(WorkspaceZone.MenuBar));
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.Content));
    }

    [Fact]
    public void FlowState_ContentZone_AlwaysFull_RegardlessOfFlowState()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);

        foreach (var state in new[] { FlowState.Calm, FlowState.Focused, FlowState.Flow })
        {
            _flowStateEngine.CurrentState.Returns(state);
            var sut = CreateSut();

            Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.Content));
        }
    }

    [Fact]
    public void FlowState_Flow_ManagementMode_NoCalmSoAllFull()
    {
        // Management mode → calm disabled → Flow cannot intensify
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);

        var sut = CreateSut();

        Assert.False(sut.CalmModeEnabled);
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.MenuBar));
    }

    // ── Reactive flow state updates ──────────────────────────────────

    [Fact]
    public void WhenFlowStateChanges_RecomputesEmphasis()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Calm);

        var sut = CreateSut();
        Assert.Equal(EmphasisLevel.Muted, sut.GetEmphasis(WorkspaceZone.MenuBar));

        // Simulate flow state transition to Flow
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);
        _flowStateEngine.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _flowStateEngine,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFlowStateEngine.CurrentState)));

        Assert.Equal(FlowState.Flow, sut.CurrentFlowState);
        Assert.Equal(EmphasisLevel.Receded, sut.GetEmphasis(WorkspaceZone.MenuBar));
    }

    [Fact]
    public void WhenFlowStateChanges_PublishesCalmEvent()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Calm);

        var sut = CreateSut();
        _eventBus.ClearReceivedCalls();

        // Transition to Flow
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);
        _flowStateEngine.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _flowStateEngine,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFlowStateEngine.CurrentState)));

        _eventBus.Received().PublishAsync(
            Arg.Is<CalmStateChangedEvent>(e => e.FlowState == FlowState.Flow));
    }

    [Fact]
    public void WhenFlowStateChanges_CalmEventIncludesFlowState()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(true);
        _flowStateEngine.CurrentState.Returns(FlowState.Focused);

        var sut = CreateSut();
        _eventBus.ClearReceivedCalls();

        // Transition to Flow
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);
        _flowStateEngine.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _flowStateEngine,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFlowStateEngine.CurrentState)));

        _eventBus.Received().PublishAsync(
            Arg.Is<CalmStateChangedEvent>(e =>
                e.CalmModeEnabled && e.FlowState == FlowState.Flow));
    }

    [Fact]
    public void FlowState_IrrelevantPropertyChange_DoesNotRecompute()
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Calm);

        var sut = CreateSut();
        _eventBus.ClearReceivedCalls();

        // Fire irrelevant property from flow engine
        _flowStateEngine.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _flowStateEngine,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFlowStateEngine.TransitionReason)));

        _eventBus.DidNotReceive().PublishAsync(Arg.Any<CalmStateChangedEvent>());
    }

    // ── Full flow lifecycle ──────────────────────────────────────────

    [Fact]
    public void FlowLifecycle_CalmToFocusedToFlowAndBack()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Calm);

        var sut = CreateSut();

        // Stage 1: Calm — chrome is Muted
        Assert.Equal(EmphasisLevel.Muted, sut.GetEmphasis(WorkspaceZone.MenuBar));

        // Stage 2: → Focused — chrome stays Muted
        _flowStateEngine.CurrentState.Returns(FlowState.Focused);
        _flowStateEngine.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _flowStateEngine,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFlowStateEngine.CurrentState)));

        Assert.Equal(EmphasisLevel.Muted, sut.GetEmphasis(WorkspaceZone.MenuBar));

        // Stage 3: → Flow — chrome intensifies to Receded
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);
        _flowStateEngine.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _flowStateEngine,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFlowStateEngine.CurrentState)));

        Assert.Equal(EmphasisLevel.Receded, sut.GetEmphasis(WorkspaceZone.MenuBar));

        // Stage 4: → Focused again — back to Muted
        _flowStateEngine.CurrentState.Returns(FlowState.Focused);
        _flowStateEngine.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _flowStateEngine,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFlowStateEngine.CurrentState)));

        Assert.Equal(EmphasisLevel.Muted, sut.GetEmphasis(WorkspaceZone.MenuBar));

        // Stage 5: → Calm — back to Muted (still billing mode)
        _flowStateEngine.CurrentState.Returns(FlowState.Calm);
        _flowStateEngine.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _flowStateEngine,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFlowStateEngine.CurrentState)));

        Assert.Equal(EmphasisLevel.Muted, sut.GetEmphasis(WorkspaceZone.MenuBar));
    }

    [Fact]
    public void Dispose_UnsubscribesFromStateSources()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Calm);

        var sut = CreateSut();
        _eventBus.ClearReceivedCalls();

        sut.Dispose();

        _appState.CurrentMode.Returns(OperationalMode.Management);
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));

        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        _flowStateEngine.CurrentState.Returns(FlowState.Flow);
        _flowStateEngine.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _flowStateEngine,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFlowStateEngine.CurrentState)));

        _eventBus.DidNotReceive().PublishAsync(Arg.Any<CalmStateChangedEvent>());
    }

    [Fact]
    public void Dispose_CanBeCalledTwice()
    {
        var sut = CreateSut();

        sut.Dispose();
        sut.Dispose();
    }

    // ── Manual override vs Flow state ────────────────────────────────

    [Fact]
    public void ManualOverride_DisablesCalm_FlowStateCannotIntensify()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);

        var sut = CreateSut();
        Assert.Equal(EmphasisLevel.Receded, sut.GetEmphasis(WorkspaceZone.MenuBar));

        // User force-disables Calm UI
        sut.SetCalmModeEnabled(false);

        // Calm disabled → all Full, Flow cannot override
        Assert.Equal(EmphasisLevel.Full, sut.GetEmphasis(WorkspaceZone.MenuBar));
    }

    // ── ChromeEmphasis with Flow integration ─────────────────────────

    [Fact]
    public void ChromeEmphasis_FlowBillingUnlocked_Returns2()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);

        var sut = CreateSut();
        // Flow intensifies Muted→Receded, so chrome emphasis = 2
        Assert.Equal(2, ComputeChromeEmphasis(sut, _focusLock));
    }

    [Fact]
    public void CalmOpacity_FlowBillingUnlocked_065()
    {
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);

        var sut = CreateSut();
        var emphasis = ComputeChromeEmphasis(sut, _focusLock);
        var opacity = ComputeCalmOpacity(emphasis);

        Assert.Equal(0.65, opacity, precision: 2);
    }
}
