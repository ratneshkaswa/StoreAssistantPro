using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Commands.Performance;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Commands;

public class PerformancePipelineBehaviorTests
{
    // ══════════════════════════════════════════════════════════════
    //  Test command types
    // ══════════════════════════════════════════════════════════════

    private sealed record TestCommand(string Name)
        : ICommandRequest<int>;

    // ══════════════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════════════

    private static PerformancePipelineBehavior<TestCommand, int> CreateBehavior(
        IPerformanceMonitor perf)
    {
        var logger = NullLogger<PerformancePipelineBehavior<TestCommand, int>>.Instance;
        return new PerformancePipelineBehavior<TestCommand, int>(perf, logger);
    }

    private static IPerformanceMonitor CreateMonitor(TimedScope? scope = null)
    {
        var monitor = Substitute.For<IPerformanceMonitor>();
        scope ??= CreateNoOpScope();
        monitor.BeginScope(Arg.Any<string>(), Arg.Any<TimeSpan?>())
            .Returns(scope);
        return monitor;
    }

    private static TimedScope CreateNoOpScope()
    {
        var logger = NullLogger<PerformanceMonitor>.Instance;
        return new PerformanceMonitor(logger)
            .BeginScope("test", TimeSpan.FromMinutes(10));
    }

    // ══════════════════════════════════════════════════════════════
    //  BeginScope is called with command-derived name
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task BeginsPerformanceScope()
    {
        var monitor = CreateMonitor();
        var behavior = CreateBehavior(monitor);

        await behavior.HandleAsync(
            new TestCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        monitor.Received(1).BeginScope(
            Arg.Is<string>(s => s.Contains("TestCommand")),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task ScopeNameContainsCommandPrefix()
    {
        string? capturedName = null;
        var monitor = Substitute.For<IPerformanceMonitor>();
        monitor.BeginScope(Arg.Any<string>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                capturedName = callInfo.Arg<string>();
                return CreateNoOpScope();
            });

        var behavior = CreateBehavior(monitor);

        await behavior.HandleAsync(
            new TestCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        Assert.NotNull(capturedName);
        Assert.StartsWith("Command.", capturedName);
        Assert.Contains("TestCommand", capturedName);
    }

    // ══════════════════════════════════════════════════════════════
    //  Scope lifecycle — disposed after execution (timing ends)
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task DisposesScope_AfterExecution()
    {
        // Real monitor — if scope doesn't dispose, the timing log
        // would never fire. We verify the monitor logs (which only
        // happens on Dispose).
        var perfLogger = Substitute.For<ILogger<PerformanceMonitor>>();
        var realMonitor = new PerformanceMonitor(perfLogger);
        var behavior = new PerformancePipelineBehavior<TestCommand, int>(
            realMonitor,
            NullLogger<PerformancePipelineBehavior<TestCommand, int>>.Instance);

        await behavior.HandleAsync(
            new TestCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        // TimedScope.Dispose logs at Debug for fast operations.
        // If it wasn't disposed, this would not fire.
        perfLogger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Result passthrough — success
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Success_ReturnsResultUnchanged()
    {
        var monitor = CreateMonitor();
        var behavior = CreateBehavior(monitor);

        var result = await behavior.HandleAsync(
            new TestCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Success(42)));

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Success_PreservesValue()
    {
        var monitor = CreateMonitor();
        var behavior = CreateBehavior(monitor);

        var result = await behavior.HandleAsync(
            new TestCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Success(99999)));

        Assert.Equal(99999, result.Value);
    }

    // ══════════════════════════════════════════════════════════════
    //  Result passthrough — failure
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Failure_ReturnsResultUnchanged()
    {
        var monitor = CreateMonitor();
        var behavior = CreateBehavior(monitor);

        var result = await behavior.HandleAsync(
            new TestCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Failure("boom")));

        Assert.False(result.Succeeded);
        Assert.Equal("boom", result.ErrorMessage);
    }

    [Fact]
    public async Task Failure_StillMeasuresPerformance()
    {
        var monitor = CreateMonitor();
        var behavior = CreateBehavior(monitor);

        await behavior.HandleAsync(
            new TestCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Failure("error")));

        monitor.Received(1).BeginScope(
            Arg.Any<string>(), Arg.Any<TimeSpan?>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Handler is always called
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task AlwaysCallsNext()
    {
        var monitor = CreateMonitor();
        var behavior = CreateBehavior(monitor);
        var nextCalled = false;

        await behavior.HandleAsync(
            new TestCommand("Widget"),
            () =>
            {
                nextCalled = true;
                return Task.FromResult(CommandResult<int>.Success(1));
            });

        Assert.True(nextCalled);
    }

    // ══════════════════════════════════════════════════════════════
    //  Scope wraps the next() call — handler runs before dispose
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ScopeCoversEntireExecution()
    {
        // Verify handler executes *before* the scope logs on dispose.
        // Use a real monitor and capture the ordering via the perf logger.
        var events = new List<string>();
        var perfLogger = Substitute.For<ILogger<PerformanceMonitor>>();
        perfLogger.When(l => l.Log(
                Arg.Any<LogLevel>(),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>()))
            .Do(_ => events.Add("scope:logged"));

        var realMonitor = new PerformanceMonitor(perfLogger);
        var behavior = new PerformancePipelineBehavior<TestCommand, int>(
            realMonitor,
            NullLogger<PerformancePipelineBehavior<TestCommand, int>>.Instance);

        await behavior.HandleAsync(
            new TestCommand("Widget"),
            () =>
            {
                events.Add("handler:executed");
                return Task.FromResult(CommandResult<int>.Success(1));
            });

        // Handler must execute before scope logs (scope logs on Dispose)
        Assert.Equal("handler:executed", events[0]);
        Assert.Equal("scope:logged", events[1]);
    }

    // ══════════════════════════════════════════════════════════════
    //  Scope disposed even on handler exception
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ScopeDisposed_EvenOnException()
    {
        // Real monitor — if scope survives the exception, the timing
        // log would not fire. Verify it does.
        var perfLogger = Substitute.For<ILogger<PerformanceMonitor>>();
        var realMonitor = new PerformanceMonitor(perfLogger);
        var behavior = new PerformancePipelineBehavior<TestCommand, int>(
            realMonitor,
            NullLogger<PerformancePipelineBehavior<TestCommand, int>>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.HandleAsync(
                new TestCommand("Widget"),
                () => throw new InvalidOperationException("unexpected")));

        // TimedScope.Dispose fires even on exception thanks to `using`
        perfLogger.Received().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Integration: real PerformanceMonitor detects slow commands
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SlowCommand_PerformanceMonitorLogsWarning()
    {
        var perfLogger = Substitute.For<ILogger<PerformanceMonitor>>();
        var realMonitor = new PerformanceMonitor(perfLogger);

        var behavior = new PerformancePipelineBehavior<TestCommand, int>(
            realMonitor,
            NullLogger<PerformancePipelineBehavior<TestCommand, int>>.Instance);

        await behavior.HandleAsync(
            new TestCommand("SlowWidget"),
            async () =>
            {
                await Task.Delay(550);
                return CommandResult<int>.Success(1);
            });

        perfLogger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task FastCommand_PerformanceMonitorLogsDebug()
    {
        var perfLogger = Substitute.For<ILogger<PerformanceMonitor>>();
        var realMonitor = new PerformanceMonitor(perfLogger);

        var behavior = new PerformancePipelineBehavior<TestCommand, int>(
            realMonitor,
            NullLogger<PerformancePipelineBehavior<TestCommand, int>>.Instance);

        await behavior.HandleAsync(
            new TestCommand("FastWidget"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        perfLogger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());

        perfLogger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // ══════════════════════════════════════════════════════════════
    //  BeginScope called exactly once per execution
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task BeginScopeCalledExactlyOnce()
    {
        var monitor = CreateMonitor();
        var behavior = CreateBehavior(monitor);

        await behavior.HandleAsync(
            new TestCommand("A"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        await behavior.HandleAsync(
            new TestCommand("B"),
            () => Task.FromResult(CommandResult<int>.Success(2)));

        monitor.Received(2).BeginScope(
            Arg.Any<string>(), Arg.Any<TimeSpan?>());
    }
}
