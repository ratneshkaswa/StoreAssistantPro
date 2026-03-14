using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Reflection;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Tests.Services;

public class ConnectivityMonitorServiceTests : IDisposable
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory =
        Substitute.For<IDbContextFactory<AppDbContext>>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private ConnectivityMonitorService CreateSut() =>
        new(_contextFactory, _eventBus,
            NullLogger<ConnectivityMonitorService>.Instance,
            Timeout.InfiniteTimeSpan);

    private void SetupCanConnect(bool canConnect)
    {
        var dbFacade = Substitute.For<DatabaseFacade>(CreateFakeContext());
        dbFacade.CanConnectAsync(Arg.Any<CancellationToken>())
            .Returns(canConnect);

        var context = Substitute.For<AppDbContext>(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        context.Database.Returns(dbFacade);

        _contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(context);
    }

    private void SetupCanConnectThrows()
    {
        _contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Connection refused"));
    }

    private static AppDbContext CreateFakeContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // ══════════════════════════════════════════════════════════════
    //  Initial state
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void IsConnected_BeforeStart_ReturnsTrue()
    {
        var sut = CreateSut();

        Assert.True(sut.IsConnected);
    }

    [Fact]
    public void PollInterval_ReturnsConfiguredValue()
    {
        var sut = CreateSut();

        Assert.Equal(Timeout.InfiniteTimeSpan, sut.PollInterval);
    }

    // ══════════════════════════════════════════════════════════════
    //  StartAsync
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task StartAsync_WhenDbReachable_StaysConnected()
    {
        SetupCanConnect(true);
        var sut = CreateSut();

        await sut.StartAsync();

        Assert.True(sut.IsConnected);
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ConnectionLostEvent>());
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ConnectionRestoredEvent>());
    }

    [Fact]
    public async Task StartAsync_WhenDbUnreachable_PublishesLostEvent()
    {
        SetupCanConnect(false);
        var sut = CreateSut();

        await sut.StartAsync();

        Assert.False(sut.IsConnected);
        await _eventBus.Received(1).PublishAsync(Arg.Any<ConnectionLostEvent>());
    }

    [Fact]
    public async Task StartAsync_CalledTwice_OnlyChecksOnce()
    {
        SetupCanConnect(true);
        var sut = CreateSut();

        await sut.StartAsync();
        await sut.StartAsync();

        await _contextFactory.Received(1)
            .CreateDbContextAsync(Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════
    //  CheckNowAsync — state transitions
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CheckNow_ConnectedToDisconnected_PublishesLostEvent()
    {
        SetupCanConnect(false);
        var sut = CreateSut();

        await sut.CheckNowAsync();

        Assert.False(sut.IsConnected);
        await _eventBus.Received(1).PublishAsync(Arg.Any<ConnectionLostEvent>());
    }

    [Fact]
    public async Task CheckNow_DisconnectedToConnected_PublishesRestoredEvent()
    {
        // Go down first
        SetupCanConnect(false);
        var sut = CreateSut();
        await sut.CheckNowAsync();

        // Come back up
        SetupCanConnect(true);
        await sut.CheckNowAsync();

        Assert.True(sut.IsConnected);
        await _eventBus.Received(1).PublishAsync(
            Arg.Any<ConnectionRestoredEvent>());
    }

    [Fact]
    public async Task CheckNow_RestoredEvent_CarriesDowntimeDuration()
    {
        TimeSpan? capturedDowntime = null;
        _eventBus.PublishAsync(Arg.Any<ConnectionRestoredEvent>())
            .Returns(ci =>
            {
                capturedDowntime = ci.Arg<ConnectionRestoredEvent>().DowntimeDuration;
                return Task.CompletedTask;
            });

        SetupCanConnect(false);
        var sut = CreateSut();
        await sut.CheckNowAsync();

        // Small delay to accumulate measurable downtime
        await Task.Delay(50);

        SetupCanConnect(true);
        await sut.CheckNowAsync();

        Assert.NotNull(capturedDowntime);
        Assert.True(capturedDowntime.Value >= TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckNow_StaysConnected_NoEventPublished()
    {
        SetupCanConnect(true);
        var sut = CreateSut();

        await sut.CheckNowAsync();
        await sut.CheckNowAsync();
        await sut.CheckNowAsync();

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ConnectionLostEvent>());
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ConnectionRestoredEvent>());
    }

    [Fact]
    public async Task CheckNow_StaysDisconnected_NoExtraLostEvent()
    {
        SetupCanConnect(false);
        var sut = CreateSut();

        await sut.CheckNowAsync();
        await sut.CheckNowAsync();
        await sut.CheckNowAsync();

        await _eventBus.Received(1).PublishAsync(Arg.Any<ConnectionLostEvent>());
    }

    [Fact]
    public async Task CheckNow_ExceptionFromFactory_TreatedAsDisconnected()
    {
        SetupCanConnectThrows();
        var sut = CreateSut();

        await sut.CheckNowAsync();

        Assert.False(sut.IsConnected);
        await _eventBus.Received(1).PublishAsync(Arg.Any<ConnectionLostEvent>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Full cycle: connected → lost → restored
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task FullCycle_ConnectedLostRestored()
    {
        // Start connected
        SetupCanConnect(true);
        var sut = CreateSut();
        await sut.CheckNowAsync();
        Assert.True(sut.IsConnected);

        // Go down
        SetupCanConnect(false);
        await sut.CheckNowAsync();
        Assert.False(sut.IsConnected);

        // Come back
        SetupCanConnect(true);
        await sut.CheckNowAsync();
        Assert.True(sut.IsConnected);

        await _eventBus.Received(1).PublishAsync(Arg.Any<ConnectionLostEvent>());
        await _eventBus.Received(1).PublishAsync(Arg.Any<ConnectionRestoredEvent>());
    }

    [Fact]
    public async Task MultipleCycles_TracksEachTransition()
    {
        var sut = CreateSut();

        // Cycle 1: down then up
        SetupCanConnect(false);
        await sut.CheckNowAsync();
        SetupCanConnect(true);
        await sut.CheckNowAsync();

        // Cycle 2: down then up
        SetupCanConnect(false);
        await sut.CheckNowAsync();
        SetupCanConnect(true);
        await sut.CheckNowAsync();

        await _eventBus.Received(2).PublishAsync(Arg.Any<ConnectionLostEvent>());
        await _eventBus.Received(2).PublishAsync(Arg.Any<ConnectionRestoredEvent>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Event bus failure resilience
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task EventBusFailure_DoesNotThrow()
    {
        _eventBus.PublishAsync(Arg.Any<ConnectionLostEvent>())
            .Returns(Task.FromException(new Exception("bus down")));
        SetupCanConnect(false);

        var sut = CreateSut();

        // Should not throw
        await sut.CheckNowAsync();

        Assert.False(sut.IsConnected);
    }

    [Fact]
    public async Task TimerCallback_WhenPreviousCheckStillRunning_DoesNotOverlap()
    {
        var context = Substitute.For<AppDbContext>(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var dbFacade = Substitute.For<DatabaseFacade>(CreateFakeContext());
        dbFacade.CanConnectAsync(Arg.Any<CancellationToken>())
            .Returns(true);
        context.Database.Returns(dbFacade);

        var firstCheckGate = new TaskCompletionSource<AppDbContext>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var createCalls = 0;

        _contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var call = Interlocked.Increment(ref createCalls);
                return call == 1
                    ? firstCheckGate.Task
                    : Task.FromResult(context);
            });

        var sut = new ConnectivityMonitorService(
            _contextFactory,
            _eventBus,
            NullLogger<ConnectivityMonitorService>.Instance,
            TimeSpan.FromMilliseconds(10));

        var callback = typeof(ConnectivityMonitorService).GetMethod(
            "OnTimerElapsed",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate timer callback.");

        callback.Invoke(sut, [null]);
        await Task.Delay(30);
        callback.Invoke(sut, [null]);
        await Task.Delay(30);

        Assert.Equal(1, Volatile.Read(ref createCalls));

        firstCheckGate.SetResult(context);
        await Task.Delay(30);

        callback.Invoke(sut, [null]);
        await Task.Delay(30);

        Assert.Equal(2, Volatile.Read(ref createCalls));

        sut.Dispose();
    }

    // ══════════════════════════════════════════════════════════════
    //  Dispose
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var sut = CreateSut();
        sut.Dispose();
        sut.Dispose(); // Double dispose safe
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose() { }
}
