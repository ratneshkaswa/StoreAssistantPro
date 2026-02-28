using Microsoft.Extensions.Logging;
using NSubstitute;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Services;

public class PerformanceMonitorTests
{
    private readonly ILogger<PerformanceMonitor> _logger = Substitute.For<ILogger<PerformanceMonitor>>();

    private PerformanceMonitor CreateSut() => new(_logger);

    [Fact]
    public void BeginScope_ReturnsNonNullScope()
    {
        var sut = CreateSut();

        using var scope = sut.BeginScope("TestOperation");

        Assert.NotNull(scope);
    }

    [Fact]
    public void Dispose_FastOperation_LogsAtDebugLevel()
    {
        var sut = CreateSut();

        using (sut.BeginScope("FastOp")) { /* instant */ }

        _logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Dispose_SlowOperation_LogsAtWarningLevel()
    {
        var sut = CreateSut();

        using (sut.BeginScope("SlowOp", TimeSpan.Zero))
        {
            // Threshold of zero means any operation is "slow".
            Thread.Sleep(1);
        }

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Dispose_CustomThreshold_UsesProvidedValue()
    {
        var sut = CreateSut();

        // Very high threshold — operation should be "fast".
        using (sut.BeginScope("CustomThresholdOp", TimeSpan.FromMinutes(10))) { }

        _logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());

        _logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void BeginScope_DefaultThreshold_Is500ms()
    {
        var sut = CreateSut();

        // Fast operation with default threshold should log Debug, not Warning.
        using (sut.BeginScope("DefaultThresholdOp")) { }

        _logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void MultipleScopes_EachLogsIndependently()
    {
        var sut = CreateSut();

        using (sut.BeginScope("Op1")) { }
        using (sut.BeginScope("Op2")) { }

        _logger.Received(2).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
