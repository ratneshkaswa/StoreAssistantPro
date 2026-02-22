using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Commands.Logging;

namespace StoreAssistantPro.Tests.Commands;

public class LoggingPipelineBehaviorTests
{
    // ══════════════════════════════════════════════════════════════
    //  Test command types
    // ══════════════════════════════════════════════════════════════

    private sealed record TestCommand(string Name)
        : ICommandRequest<int>;

    // ══════════════════════════════════════════════════════════════
    //  Capturing logger for message-level assertions
    // ══════════════════════════════════════════════════════════════

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }

        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }

    // ══════════════════════════════════════════════════════════════
    //  Helper: build behavior with capturing logger
    // ══════════════════════════════════════════════════════════════

    private static (LoggingPipelineBehavior<TestCommand, int> Behavior,
                     CapturingLogger<LoggingPipelineBehavior<TestCommand, int>> Logger) CreateSut()
    {
        var logger = new CapturingLogger<LoggingPipelineBehavior<TestCommand, int>>();
        var behavior = new LoggingPipelineBehavior<TestCommand, int>(logger);
        return (behavior, logger);
    }

    // ══════════════════════════════════════════════════════════════
    //  Start logging
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task LogsCommandStart()
    {
        var (behavior, logger) = CreateSut();
        var command = new TestCommand("Widget");

        await behavior.HandleAsync(
            command,
            () => Task.FromResult(CommandResult<int>.Success(1)));

        Assert.Contains(logger.Entries,
            e => e.Level == LogLevel.Information &&
                 e.Message.Contains("Executing") &&
                 e.Message.Contains("TestCommand"));
    }

    // ══════════════════════════════════════════════════════════════
    //  Success logging
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task LogsSuccessWithDuration()
    {
        var (behavior, logger) = CreateSut();
        var command = new TestCommand("Widget");

        var result = await behavior.HandleAsync(
            command,
            () => Task.FromResult(CommandResult<int>.Success(42)));

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
        Assert.Contains(logger.Entries,
            e => e.Level == LogLevel.Information &&
                 e.Message.Contains("succeeded") &&
                 e.Message.Contains("ms"));
    }

    // ══════════════════════════════════════════════════════════════
    //  Failure logging
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task LogsFailureWithErrorAndDuration()
    {
        var (behavior, logger) = CreateSut();
        var command = new TestCommand("Widget");

        var result = await behavior.HandleAsync(
            command,
            () => Task.FromResult(CommandResult<int>.Failure("Out of stock")));

        Assert.False(result.Succeeded);
        Assert.Contains(logger.Entries,
            e => e.Level == LogLevel.Warning &&
                 e.Message.Contains("failed") &&
                 e.Message.Contains("Out of stock") &&
                 e.Message.Contains("ms"));
    }

    [Fact]
    public async Task Failure_DoesNotLogSuccessMessage()
    {
        var (behavior, logger) = CreateSut();
        var command = new TestCommand("Widget");

        await behavior.HandleAsync(
            command,
            () => Task.FromResult(CommandResult<int>.Failure("error")));

        Assert.DoesNotContain(logger.Entries,
            e => e.Level == LogLevel.Information &&
                 e.Message.Contains("succeeded"));
    }

    [Fact]
    public async Task Success_DoesNotLogFailureMessage()
    {
        var (behavior, logger) = CreateSut();
        var command = new TestCommand("Widget");

        await behavior.HandleAsync(
            command,
            () => Task.FromResult(CommandResult<int>.Success(1)));

        Assert.DoesNotContain(logger.Entries,
            e => e.Level == LogLevel.Warning &&
                 e.Message.Contains("failed"));
    }

    // ══════════════════════════════════════════════════════════════
    //  Slow command warning
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SlowCommand_LogsWarning()
    {
        var (behavior, logger) = CreateSut();
        var command = new TestCommand("SlowOp");

        await behavior.HandleAsync(
            command,
            async () =>
            {
                await Task.Delay(
                    LoggingPipelineBehavior<TestCommand, int>.SlowCommandThreshold
                    + TimeSpan.FromMilliseconds(50));
                return CommandResult<int>.Success(1);
            });

        Assert.Contains(logger.Entries,
            e => e.Level == LogLevel.Warning &&
                 e.Message.Contains("Slow command") &&
                 e.Message.Contains("TestCommand"));
    }

    [Fact]
    public async Task FastCommand_NoSlowWarning()
    {
        var (behavior, logger) = CreateSut();
        var command = new TestCommand("FastOp");

        await behavior.HandleAsync(
            command,
            () => Task.FromResult(CommandResult<int>.Success(1)));

        Assert.DoesNotContain(logger.Entries,
            e => e.Message.Contains("Slow command"));
    }

    // ══════════════════════════════════════════════════════════════
    //  Log sequence: start → outcome
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Success_LogSequence_StartThenSucceeded()
    {
        var (behavior, logger) = CreateSut();

        await behavior.HandleAsync(
            new TestCommand("W"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        var infoEntries = logger.Entries
            .Where(e => e.Level == LogLevel.Information)
            .ToList();

        Assert.True(infoEntries.Count >= 2);
        Assert.Contains("Executing", infoEntries[0].Message);
        Assert.Contains("succeeded", infoEntries[1].Message);
    }

    [Fact]
    public async Task Failure_LogSequence_StartThenFailed()
    {
        var (behavior, logger) = CreateSut();

        await behavior.HandleAsync(
            new TestCommand("W"),
            () => Task.FromResult(CommandResult<int>.Failure("err")));

        var start = logger.Entries.First(e => e.Message.Contains("Executing"));
        var fail = logger.Entries.First(e => e.Message.Contains("failed"));

        Assert.Equal(LogLevel.Information, start.Level);
        Assert.Equal(LogLevel.Warning, fail.Level);
        Assert.True(logger.Entries.IndexOf(start) < logger.Entries.IndexOf(fail));
    }

    // ══════════════════════════════════════════════════════════════
    //  Result passthrough
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Success_PassesResultThrough()
    {
        var (behavior, _) = CreateSut();

        var result = await behavior.HandleAsync(
            new TestCommand("W"),
            () => Task.FromResult(CommandResult<int>.Success(99)));

        Assert.True(result.Succeeded);
        Assert.Equal(99, result.Value);
    }

    [Fact]
    public async Task Failure_PassesResultThrough()
    {
        var (behavior, _) = CreateSut();

        var result = await behavior.HandleAsync(
            new TestCommand("W"),
            () => Task.FromResult(CommandResult<int>.Failure("boom")));

        Assert.False(result.Succeeded);
        Assert.Equal("boom", result.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  Command name in log messages
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllLogEntries_ContainCommandName()
    {
        var (behavior, logger) = CreateSut();

        await behavior.HandleAsync(
            new TestCommand("W"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        Assert.All(logger.Entries,
            e => Assert.Contains("TestCommand", e.Message));
    }

    // ══════════════════════════════════════════════════════════════
    //  Duration is measured
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task DurationIncluded_InSuccessLog()
    {
        var (behavior, logger) = CreateSut();

        await behavior.HandleAsync(
            new TestCommand("W"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        var successEntry = logger.Entries.First(e => e.Message.Contains("succeeded"));
        Assert.Matches(@"\d+\.\d+ ms", successEntry.Message);
    }

    [Fact]
    public async Task DurationIncluded_InFailureLog()
    {
        var (behavior, logger) = CreateSut();

        await behavior.HandleAsync(
            new TestCommand("W"),
            () => Task.FromResult(CommandResult<int>.Failure("err")));

        var failEntry = logger.Entries.First(e => e.Message.Contains("failed"));
        Assert.Matches(@"\d+\.\d+ ms", failEntry.Message);
    }

    // ══════════════════════════════════════════════════════════════
    //  CancellationToken — behavior always calls next()
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task AlwaysCallsNext()
    {
        var (behavior, _) = CreateSut();
        var nextCalled = false;

        await behavior.HandleAsync(
            new TestCommand("W"),
            () =>
            {
                nextCalled = true;
                return Task.FromResult(CommandResult<int>.Success(1));
            });

        Assert.True(nextCalled);
    }

    // ══════════════════════════════════════════════════════════════
    //  SlowCommandThreshold is 500ms
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void SlowCommandThreshold_Is500ms()
    {
        Assert.Equal(
            TimeSpan.FromMilliseconds(500),
            LoggingPipelineBehavior<TestCommand, int>.SlowCommandThreshold);
    }
}
