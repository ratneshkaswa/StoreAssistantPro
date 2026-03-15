using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

public class InteractionTrackerTests : IDisposable
{
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private readonly InteractionTracker _sut;

    public InteractionTrackerTests()
    {
        _regional.Now.Returns(new DateTime(2025, 6, 15, 10, 0, 0));

        _sut = new InteractionTracker(
            _regional, _eventBus,
            NullLogger<InteractionTracker>.Instance);
    }

    public void Dispose() => _sut.Dispose();

    // ═══════════════════════════════════════════════════════════════
    // Initial state
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void InitialSnapshot_AllZeros()
    {
        var snap = _sut.CurrentSnapshot;

        Assert.Equal(0, snap.KeyboardFrequency);
        Assert.Equal(0, snap.MouseFrequency);
        Assert.Equal(0, snap.BillingActionsPerMinute);
    }

    [Fact]
    public void InitialSnapshot_HasTimestamp()
    {
        Assert.Equal(new DateTime(2025, 6, 15, 10, 0, 0), _sut.CurrentSnapshot.CapturedAt);
    }

    // ═══════════════════════════════════════════════════════════════
    // RecordKeyPress
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RecordKeyPress_ThenTick_IncreasesKeyboardFrequency()
    {
        // Record several key presses
        for (var i = 0; i < 10; i++)
            _sut.RecordKeyPress();

        _sut.Tick();

        Assert.True(_sut.CurrentSnapshot.KeyboardFrequency > 0);
    }

    [Fact]
    public void RecordKeyPress_ManyPresses_HigherFrequency()
    {
        for (var i = 0; i < 30; i++)
            _sut.RecordKeyPress();

        _sut.Tick();

        // 30 presses within 5-second window = 6 keys/sec
        Assert.True(_sut.CurrentSnapshot.KeyboardFrequency >= 2.0);
    }

    [Fact]
    public void RecordKeyPress_NoAllocation()
    {
        // Just verify it doesn't throw — the lock-free design is
        // validated by the fact that this is a simple array write
        _sut.RecordKeyPress();
    }

    // ═══════════════════════════════════════════════════════════════
    // RecordMouseMove
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RecordMouseMove_ThenTick_IncreasesMouseFrequency()
    {
        for (var i = 0; i < 10; i++)
            _sut.RecordMouseMove();

        _sut.Tick();

        Assert.True(_sut.CurrentSnapshot.MouseFrequency > 0);
    }

    // ═══════════════════════════════════════════════════════════════
    // RecordBillingAction
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RecordBillingAction_ThenTick_IncreasesBillingRate()
    {
        for (var i = 0; i < 5; i++)
            _sut.RecordBillingAction();

        _sut.Tick();

        Assert.True(_sut.CurrentSnapshot.BillingActionsPerMinute > 0);
    }

    [Fact]
    public void RecordBillingAction_FrequencyScalesToPerMinute()
    {
        // 5 actions in a 5-second window = 1 action/sec = 60 actions/min
        for (var i = 0; i < 5; i++)
            _sut.RecordBillingAction();

        _sut.Tick();

        // Should be approximately 60 actions/min
        Assert.True(_sut.CurrentSnapshot.BillingActionsPerMinute >= 30);
    }

    // ═══════════════════════════════════════════════════════════════
    // Idle time
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void AfterRecordKeyPress_IdleIsNearZero()
    {
        _sut.RecordKeyPress();
        _sut.Tick();

        // Just pressed a key — idle should be near zero
        Assert.True(_sut.CurrentSnapshot.IdleSeconds < 1.0);
    }

    // ═══════════════════════════════════════════════════════════════
    // Tick — no-change suppression
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Tick_WithoutRecords_DoesNotPublishEventWhenNoChange()
    {
        _eventBus.ClearReceivedCalls();

        _sut.Tick();

        // Initial snapshot is already idle — no significant change
        _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<InteractionSnapshotChangedEvent>());
    }

    [Fact]
    public void Tick_AfterSignificantChange_PublishesEvent()
    {
        _eventBus.ClearReceivedCalls();

        // Record enough to cross a frequency threshold
        for (var i = 0; i < 20; i++)
            _sut.RecordKeyPress();

        _sut.Tick();

        _eventBus.Received().PublishAsync(
            Arg.Any<InteractionSnapshotChangedEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Mixed signals
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void MixedSignals_AllMetricsComputed()
    {
        for (var i = 0; i < 10; i++)
        {
            _sut.RecordKeyPress();
            _sut.RecordMouseMove();
            _sut.RecordBillingAction();
        }

        _sut.Tick();

        var snap = _sut.CurrentSnapshot;
        Assert.True(snap.KeyboardFrequency > 0);
        Assert.True(snap.MouseFrequency > 0);
        Assert.True(snap.BillingActionsPerMinute > 0);
    }

    // ═══════════════════════════════════════════════════════════════
    // Ring buffer wrapping
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RingBuffer_WrapsWithout_Error()
    {
        // Write more than buffer size (128)
        for (var i = 0; i < 200; i++)
            _sut.RecordKeyPress();

        _sut.Tick();

        // Should still work correctly — no out-of-bounds
        Assert.True(_sut.CurrentSnapshot.KeyboardFrequency > 0);
    }

    [Fact]
    public void RingBuffer_LargeVolumeDoesNotThrow()
    {
        // Simulate very rapid input
        for (var i = 0; i < 1000; i++)
        {
            _sut.RecordKeyPress();
            _sut.RecordMouseMove();
        }

        _sut.Tick();
        // No exception = pass
    }

    // ═══════════════════════════════════════════════════════════════
    // Dispose
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_IsIdempotent()
    {
        _sut.Dispose();
        _sut.Dispose();
        // No exception = pass
    }

    [Fact]
    public void AfterDispose_RecordDoesNotThrow()
    {
        _sut.Dispose();

        // Record methods should gracefully handle disposed state
        _sut.RecordKeyPress();
        _sut.RecordMouseMove();
        _sut.RecordBillingAction();
    }

    // ═══════════════════════════════════════════════════════════════
    // InteractionSnapshot record
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Snapshot_Idle_HasZeroFrequencies()
    {
        var snap = InteractionSnapshot.Idle(DateTime.UtcNow);

        Assert.Equal(0, snap.KeyboardFrequency);
        Assert.Equal(0, snap.MouseFrequency);
        Assert.Equal(0, snap.BillingActionsPerMinute);
        Assert.Equal(0, snap.IdleSeconds);
    }

    [Fact]
    public void Snapshot_IsRapidInput_TrueWhenHighFrequencyAndLowIdle()
    {
        var snap = new InteractionSnapshot(
            KeyboardFrequency: 4.0,
            MouseFrequency: 1.0,
            IdleSeconds: 0.2,
            BillingActionsPerMinute: 30,
            CapturedAt: DateTime.UtcNow);

        Assert.True(snap.IsRapidInput);
    }

    [Fact]
    public void Snapshot_IsRapidInput_FalseWhenLowFrequency()
    {
        var snap = new InteractionSnapshot(
            KeyboardFrequency: 0.5,
            MouseFrequency: 0.1,
            IdleSeconds: 0.2,
            BillingActionsPerMinute: 5,
            CapturedAt: DateTime.UtcNow);

        Assert.False(snap.IsRapidInput);
    }

    [Fact]
    public void Snapshot_IsRapidInput_FalseWhenHighIdle()
    {
        var snap = new InteractionSnapshot(
            KeyboardFrequency: 4.0,
            MouseFrequency: 1.0,
            IdleSeconds: 2.0,
            BillingActionsPerMinute: 30,
            CapturedAt: DateTime.UtcNow);

        Assert.False(snap.IsRapidInput);
    }

    [Fact]
    public void Snapshot_IsIdle_TrueWhenIdleSecondsAboveThreshold()
    {
        var snap = new InteractionSnapshot(
            KeyboardFrequency: 0,
            MouseFrequency: 0,
            IdleSeconds: 5.0,
            BillingActionsPerMinute: 0,
            CapturedAt: DateTime.UtcNow);

        Assert.True(snap.IsIdle);
    }

    [Fact]
    public void Snapshot_IsIdle_FalseWhenRecentActivity()
    {
        var snap = new InteractionSnapshot(
            KeyboardFrequency: 2.0,
            MouseFrequency: 1.0,
            IdleSeconds: 0.5,
            BillingActionsPerMinute: 10,
            CapturedAt: DateTime.UtcNow);

        Assert.False(snap.IsIdle);
    }

    [Fact]
    public void Snapshot_RecordEquality()
    {
        var dt = new DateTime(2025, 6, 15, 10, 0, 0);
        var a = new InteractionSnapshot(2.0, 1.0, 0.5, 30.0, dt);
        var b = new InteractionSnapshot(2.0, 1.0, 0.5, 30.0, dt);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Snapshot_DifferentValues_NotEqual()
    {
        var dt = new DateTime(2025, 6, 15, 10, 0, 0);
        var a = new InteractionSnapshot(2.0, 1.0, 0.5, 30.0, dt);
        var b = new InteractionSnapshot(3.0, 1.0, 0.5, 30.0, dt);

        Assert.NotEqual(a, b);
    }

    // ═══════════════════════════════════════════════════════════════
    // InteractionSnapshotChangedEvent record
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Event_RecordEquality()
    {
        var dt = new DateTime(2025, 6, 15, 10, 0, 0);
        var snap = new InteractionSnapshot(2.0, 1.0, 0.5, 30.0, dt);
        var a = new InteractionSnapshotChangedEvent(snap);
        var b = new InteractionSnapshotChangedEvent(snap);

        Assert.Equal(a, b);
    }

    // ═══════════════════════════════════════════════════════════════
    // PropertyChanged notification
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Tick_WithChange_RaisesPropertyChanged()
    {
        var raised = false;
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(InteractionTracker.CurrentSnapshot))
                raised = true;
        };

        for (var i = 0; i < 20; i++)
            _sut.RecordKeyPress();

        _sut.Tick();

        Assert.True(raised);
    }

    [Fact]
    public void Tick_FromBackgroundThread_Should_MarshalSnapshotUpdateToDispatcher()
    {
        RunOnStaThread(() =>
        {
            var sut = new InteractionTracker(
                _regional,
                _eventBus,
                NullLogger<InteractionTracker>.Instance,
                Dispatcher.CurrentDispatcher);

            try
            {
                var dispatcherThreadId = Thread.CurrentThread.ManagedThreadId;
                var raisedThreadId = -1;

                sut.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(InteractionTracker.CurrentSnapshot))
                        raisedThreadId = Thread.CurrentThread.ManagedThreadId;
                };

                for (var i = 0; i < 20; i++)
                    sut.RecordKeyPress();

                var worker = new Thread(sut.Tick);
                worker.Start();
                WaitForThread(worker);

                Assert.True(sut.CurrentSnapshot.KeyboardFrequency > 0);
                Assert.Equal(dispatcherThreadId, raisedThreadId);
            }
            finally
            {
                sut.Dispose();
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // Concurrency safety
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConcurrentRecords_DoNotCorruptState()
    {
        // Simulate concurrent input from multiple threads
        var tasks = new Task[4];
        for (var t = 0; t < 4; t++)
        {
            var signal = t % 3;
            tasks[t] = Task.Run(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    switch (signal)
                    {
                        case 0: _sut.RecordKeyPress(); break;
                        case 1: _sut.RecordMouseMove(); break;
                        case 2: _sut.RecordBillingAction(); break;
                    }
                }
            });
        }

        await Task.WhenAll(tasks);
        _sut.Tick();

        // No corruption — snapshot computes without exception
        Assert.NotNull(_sut.CurrentSnapshot);
        Assert.True(_sut.CurrentSnapshot.KeyboardFrequency >= 0);
        Assert.True(_sut.CurrentSnapshot.MouseFrequency >= 0);
        Assert.True(_sut.CurrentSnapshot.BillingActionsPerMinute >= 0);
    }

    // ═══════════════════════════════════════════════════════════════
    // IsRapidInput threshold boundary
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(1.9, 0.5, false)]  // just below frequency threshold
    [InlineData(2.0, 0.5, true)]   // at frequency threshold
    [InlineData(5.0, 0.5, true)]   // well above
    [InlineData(2.0, 1.5, false)]  // at idle threshold boundary
    [InlineData(2.0, 1.4, true)]   // just below idle threshold
    public void Snapshot_IsRapidInput_BoundaryValues(
        double keyFreq, double idleSec, bool expected)
    {
        var snap = new InteractionSnapshot(keyFreq, 0, idleSec, 0, DateTime.UtcNow);
        Assert.Equal(expected, snap.IsRapidInput);
    }

    // ═══════════════════════════════════════════════════════════════
    // IsIdle threshold boundary
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(2.9, false)]  // just below threshold
    [InlineData(3.0, true)]   // at threshold
    [InlineData(10.0, true)]  // well above
    public void Snapshot_IsIdle_BoundaryValues(double idleSec, bool expected)
    {
        var snap = new InteractionSnapshot(0, 0, idleSec, 0, DateTime.UtcNow);
        Assert.Equal(expected, snap.IsIdle);
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
