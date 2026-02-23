using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Tests.Core;

public class BaseCommandHandlerTests
{
    private sealed record TestCommand(string Value) : ICommandRequest<Unit>;

    [Fact]
    public async Task HandleAsync_Success_ReturnsExecuteResult()
    {
        var handler = new SuccessHandler();

        var result = await handler.HandleAsync(new TestCommand("ok"));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task HandleAsync_ExpectedFailure_ReturnsFailureFromExecute()
    {
        var handler = new ValidationFailureHandler();

        var result = await handler.HandleAsync(new TestCommand("bad"));

        Assert.False(result.Succeeded);
        Assert.Equal("Validation failed.", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_UnhandledException_CatchesAndReturnsFailure()
    {
        var handler = new ThrowingHandler();

        var result = await handler.HandleAsync(new TestCommand("boom"));

        Assert.False(result.Succeeded);
        Assert.Equal("Something broke", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_PassesCommandToExecute()
    {
        var handler = new CapturingHandler();

        await handler.HandleAsync(new TestCommand("captured"));

        Assert.Equal("captured", handler.CapturedValue);
    }

    // ── Test handler implementations ──

    private class SuccessHandler : BaseCommandHandler<TestCommand>
    {
        protected override Task<CommandResult> ExecuteAsync(TestCommand command) =>
            Task.FromResult(CommandResult.Success());
    }

    private class ValidationFailureHandler : BaseCommandHandler<TestCommand>
    {
        protected override Task<CommandResult> ExecuteAsync(TestCommand command) =>
            Task.FromResult(CommandResult.Failure("Validation failed."));
    }

    private class ThrowingHandler : BaseCommandHandler<TestCommand>
    {
        protected override Task<CommandResult> ExecuteAsync(TestCommand command) =>
            throw new InvalidOperationException("Something broke");
    }

    private class CapturingHandler : BaseCommandHandler<TestCommand>
    {
        public string? CapturedValue { get; private set; }

        protected override Task<CommandResult> ExecuteAsync(TestCommand command)
        {
            CapturedValue = command.Value;
            return Task.FromResult(CommandResult.Success());
        }
    }
}
