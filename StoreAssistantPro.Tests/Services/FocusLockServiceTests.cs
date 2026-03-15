using NSubstitute;
using StoreAssistantPro.Core.Events;
using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

public class FocusLockServiceTests
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private Func<OperationalModeChangedEvent, Task>? _modeHandler;

    private FocusLockService CreateSut(Dispatcher? dispatcher = null)
    {
        _eventBus.When(e => e.Subscribe(Arg.Any<Func<OperationalModeChangedEvent, Task>>()))
            .Do(ci => _modeHandler = ci.Arg<Func<OperationalModeChangedEvent, Task>>());

        return new FocusLockService(_eventBus, dispatcher);
    }

    // ── Initial state ──────────────────────────────────────────────

    [Fact]
    public void InitialState_IsUnlocked()
    {
        var sut = CreateSut();

        Assert.False(sut.IsFocusLocked);
        Assert.Equal(string.Empty, sut.ActiveModule);
    }

    // ── Acquire ────────────────────────────────────────────────────

    [Fact]
    public void Acquire_LocksToModule()
    {
        var sut = CreateSut();

        sut.Acquire("Billing");

        Assert.True(sut.IsFocusLocked);
        Assert.Equal("Billing", sut.ActiveModule);
    }

    [Fact]
    public void Acquire_SameModuleTwice_IsIdempotent()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");

        sut.Acquire("Billing"); // no-op

        Assert.True(sut.IsFocusLocked);
        Assert.Equal("Billing", sut.ActiveModule);
    }

    [Fact]
    public void Acquire_DifferentModule_WhileLocked_Throws()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");

        var ex = Assert.Throws<InvalidOperationException>(
            () => sut.Acquire("Inventory"));

        Assert.Contains("Billing", ex.Message);
        Assert.Contains("Inventory", ex.Message);
    }

    // ── Release ────────────────────────────────────────────────────

    [Fact]
    public void Release_MatchingModule_Unlocks()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");

        sut.Release("Billing");

        Assert.False(sut.IsFocusLocked);
        Assert.Equal(string.Empty, sut.ActiveModule);
    }

    [Fact]
    public void Release_WhenNotLocked_IsNoOp()
    {
        var sut = CreateSut();

        sut.Release("Billing"); // no-op

        Assert.False(sut.IsFocusLocked);
    }

    [Fact]
    public void Release_WrongModule_Throws()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");

        var ex = Assert.Throws<InvalidOperationException>(
            () => sut.Release("Inventory"));

        Assert.Contains("Billing", ex.Message);
        Assert.Contains("Inventory", ex.Message);
    }

    // ── Auto mode switching ────────────────────────────────────────

    [Fact]
    public async Task ModeChangedToBilling_AcquiresLock()
    {
        var sut = CreateSut();

        await _modeHandler!(new OperationalModeChangedEvent(
            OperationalMode.Management, OperationalMode.Billing));

        Assert.True(sut.IsFocusLocked);
        Assert.Equal("Billing", sut.ActiveModule);
    }

    [Fact]
    public async Task ModeChangedToManagement_ReleasesLock()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");

        await _modeHandler!(new OperationalModeChangedEvent(
            OperationalMode.Billing, OperationalMode.Management));

        Assert.False(sut.IsFocusLocked);
        Assert.Equal(string.Empty, sut.ActiveModule);
    }

    [Fact]
    public async Task ModeChangedToBilling_WhenAlreadyLocked_IsIdempotent()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");

        await _modeHandler!(new OperationalModeChangedEvent(
            OperationalMode.Management, OperationalMode.Billing));

        Assert.True(sut.IsFocusLocked);
        Assert.Equal("Billing", sut.ActiveModule);
    }

    [Fact]
    public async Task ModeChangedToManagement_WhenNotLocked_IsNoOp()
    {
        var sut = CreateSut();

        await _modeHandler!(new OperationalModeChangedEvent(
            OperationalMode.Billing, OperationalMode.Management));

        Assert.False(sut.IsFocusLocked);
    }

    // ── PropertyChanged notifications ──────────────────────────────

    [Fact]
    public void Acquire_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        var changed = new List<string>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        sut.Acquire("Billing");

        Assert.Contains(nameof(IFocusLockService.ActiveModule), changed);
        Assert.Contains(nameof(IFocusLockService.IsFocusLocked), changed);
    }

    [Fact]
    public void Release_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");

        var changed = new List<string>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        sut.Release("Billing");

        Assert.Contains(nameof(IFocusLockService.ActiveModule), changed);
        Assert.Contains(nameof(IFocusLockService.IsFocusLocked), changed);
    }

    [Fact]
    public void ModeChangedFromBackgroundThread_Should_MarshalPropertyChangesToDispatcher()
    {
        RunOnStaThread(() =>
        {
            var sut = CreateSut(Dispatcher.CurrentDispatcher);
            var dispatcherThreadId = Thread.CurrentThread.ManagedThreadId;
            var raisedThreadIds = new List<int>();

            sut.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(IFocusLockService.ActiveModule)
                    or nameof(IFocusLockService.IsFocusLocked))
                {
                    raisedThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
                }
            };

            var worker = new Thread(() =>
                _modeHandler!(new OperationalModeChangedEvent(
                    OperationalMode.Management,
                    OperationalMode.Billing)).GetAwaiter().GetResult());

            worker.Start();
            WaitForThread(worker);

            Assert.True(sut.IsFocusLocked);
            Assert.Equal("Billing", sut.ActiveModule);
            Assert.NotEmpty(raisedThreadIds);
            Assert.All(raisedThreadIds, id => Assert.Equal(dispatcherThreadId, id));
        });
    }

    // ── Release hold ─────────────────────────────────────────────

    [Fact]
    public void HoldRelease_SetsIsReleaseHeld()
    {
        var sut = CreateSut();

        sut.HoldRelease();

        Assert.True(sut.IsReleaseHeld);
    }

    [Fact]
    public void HoldRelease_WhenAlreadyHeld_Throws()
    {
        var sut = CreateSut();
        sut.HoldRelease();

        Assert.Throws<InvalidOperationException>(
            () => sut.HoldRelease());
    }

    [Fact]
    public void LiftReleaseHold_ClearsIsReleaseHeld()
    {
        var sut = CreateSut();
        sut.HoldRelease();

        sut.LiftReleaseHold();

        Assert.False(sut.IsReleaseHeld);
    }

    [Fact]
    public void LiftReleaseHold_WhenNotHeld_Throws()
    {
        var sut = CreateSut();

        Assert.Throws<InvalidOperationException>(
            () => sut.LiftReleaseHold());
    }

    [Fact]
    public void Release_WhileHeld_DefersRelease()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");
        sut.HoldRelease();

        sut.Release("Billing");

        // Lock is still held — release was deferred
        Assert.True(sut.IsFocusLocked);
        Assert.Equal("Billing", sut.ActiveModule);
    }

    [Fact]
    public void LiftReleaseHold_FlushesDeferredRelease()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");
        sut.HoldRelease();
        sut.Release("Billing");

        sut.LiftReleaseHold();

        Assert.False(sut.IsFocusLocked);
        Assert.Equal(string.Empty, sut.ActiveModule);
    }

    [Fact]
    public void LiftReleaseHold_NoPendingRelease_DoesNotUnlock()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");
        sut.HoldRelease();

        sut.LiftReleaseHold();

        Assert.True(sut.IsFocusLocked);
        Assert.Equal("Billing", sut.ActiveModule);
    }

    [Fact]
    public void Acquire_WhileHeld_ClearsDeferredRelease()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");
        sut.HoldRelease();
        sut.Release("Billing");

        // Re-acquire clears the deferred release
        sut.Acquire("Billing");
        sut.LiftReleaseHold();

        // Lock remains because deferred release was cleared
        Assert.True(sut.IsFocusLocked);
        Assert.Equal("Billing", sut.ActiveModule);
    }

    [Fact]
    public async Task ModeChangedToManagement_WhileHeld_DefersRelease()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");
        sut.HoldRelease();

        await _modeHandler!(new OperationalModeChangedEvent(
            OperationalMode.Billing, OperationalMode.Management));

        Assert.True(sut.IsFocusLocked);
        Assert.Equal("Billing", sut.ActiveModule);
    }

    [Fact]
    public async Task ModeChangedToManagement_WhileHeld_FlushesOnLift()
    {
        var sut = CreateSut();
        sut.Acquire("Billing");
        sut.HoldRelease();

        await _modeHandler!(new OperationalModeChangedEvent(
            OperationalMode.Billing, OperationalMode.Management));

        sut.LiftReleaseHold();

        Assert.False(sut.IsFocusLocked);
    }

    // ── Dispose ────────────────────────────────────────────────────

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        var sut = CreateSut();

        sut.Dispose();

        _eventBus.Received(1).Unsubscribe(
            Arg.Any<Func<OperationalModeChangedEvent, Task>>());
    }

    // ── Subscription ───────────────────────────────────────────────

    [Fact]
    public void Constructor_SubscribesToModeChangedEvent()
    {
        _ = CreateSut();

        _eventBus.Received(1).Subscribe(
            Arg.Any<Func<OperationalModeChangedEvent, Task>>());
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        using var completed = new ManualResetEventSlim(false);

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
                completed.Set();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(completed.Wait(TimeSpan.FromSeconds(10)), "STA test thread timed out.");

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void WaitForThread(Thread thread)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (thread.IsAlive && DateTime.UtcNow < deadline)
            DrainDispatcher();

        Assert.False(thread.IsAlive, "Worker thread timed out.");
    }

    private static void DrainDispatcher()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new DispatcherOperationCallback(_ =>
            {
                frame.Continue = false;
                return null;
            }),
            null);
        Dispatcher.PushFrame(frame);
    }
}
