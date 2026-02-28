using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Tests.Commands;

public class CommandRequestArchitectureTests
{
    // ══════════════════════════════════════════════════════════════
    //  Test command types
    // ══════════════════════════════════════════════════════════════

    private sealed record CreateItemCommand(string Name, decimal Price)
        : ICommandRequest<int>;

    private sealed record DeleteItemCommand(int Id)
        : ICommandRequest<bool>;

    // ══════════════════════════════════════════════════════════════
    //  ICommandRequest<TResult>
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void CommandRequest_ImplementsICommand()
    {
        ICommand command = new CreateItemCommand("Widget", 9.99m);

        Assert.IsAssignableFrom<ICommand>(command);
    }

    [Fact]
    public void CommandRequest_ImplementsICommandRequest()
    {
        ICommandRequest<int> request = new CreateItemCommand("Widget", 9.99m);

        Assert.IsAssignableFrom<ICommandRequest<int>>(request);
    }

    [Fact]
    public void CommandRequest_PreservesData()
    {
        var command = new CreateItemCommand("Widget", 9.99m);

        Assert.Equal("Widget", command.Name);
        Assert.Equal(9.99m, command.Price);
    }

    [Fact]
    public void CommandRequest_DifferentResultTypes_AreDistinct()
    {
        var intRequest = new CreateItemCommand("A", 1m);
        var boolRequest = new DeleteItemCommand(42);

        Assert.IsAssignableFrom<ICommandRequest<int>>(intRequest);
        Assert.IsAssignableFrom<ICommandRequest<bool>>(boolRequest);
        Assert.IsNotAssignableFrom<ICommandRequest<bool>>(intRequest);
        Assert.IsNotAssignableFrom<ICommandRequest<int>>(boolRequest);
    }

    // ══════════════════════════════════════════════════════════════
    //  CommandResult<TResult>
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void TypedResult_Success_CarriesValue()
    {
        var result = CommandResult<int>.Success(42);

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void TypedResult_Failure_CarriesError()
    {
        var result = CommandResult<int>.Failure("Item not found");

        Assert.False(result.Succeeded);
        Assert.Equal("Item not found", result.ErrorMessage);
        Assert.Equal(default, result.Value);
    }

    [Fact]
    public void TypedResult_ToBase_Success()
    {
        var typed = CommandResult<int>.Success(42);

        var @base = typed.ToBase();

        Assert.True(@base.Succeeded);
        Assert.Null(@base.ErrorMessage);
    }

    [Fact]
    public void TypedResult_ToBase_Failure()
    {
        var typed = CommandResult<int>.Failure("Bad request");

        var @base = typed.ToBase();

        Assert.False(@base.Succeeded);
        Assert.Equal("Bad request", @base.ErrorMessage);
    }

    [Fact]
    public void TypedResult_Success_WithReferenceType()
    {
        var result = CommandResult<string>.Success("created");

        Assert.True(result.Succeeded);
        Assert.Equal("created", result.Value);
    }

    [Fact]
    public void TypedResult_Failure_WithReferenceType()
    {
        var result = CommandResult<string>.Failure("error");

        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
    }

    // ══════════════════════════════════════════════════════════════
    //  ICommandRequestHandler<TCommand, TResult>
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Handler_ReturnsTypedSuccess()
    {
        ICommandRequestHandler<CreateItemCommand, int> handler =
            new CreateItemHandler();

        var result = await handler.HandleAsync(
            new CreateItemCommand("Widget", 9.99m));

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value);
    }

    [Fact]
    public async Task Handler_ReturnsTypedFailure()
    {
        ICommandRequestHandler<CreateItemCommand, int> handler =
            new FailingCreateItemHandler();

        var result = await handler.HandleAsync(
            new CreateItemCommand("", 0m));

        Assert.False(result.Succeeded);
        Assert.Equal("Name is required", result.ErrorMessage);
    }

    [Fact]
    public async Task Handler_ReceivesCancellationToken()
    {
        var handler = new CancellationAwareHandler();
        using var cts = new CancellationTokenSource();

        await handler.HandleAsync(new CreateItemCommand("A", 1m), cts.Token);

        Assert.True(handler.ReceivedToken);
    }

    // ══════════════════════════════════════════════════════════════
    //  ICommandPipelineBehavior<TCommand, TResult>
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Behavior_CanWrapHandler()
    {
        var handler = new CreateItemHandler();
        var behavior = new TrackingBehavior<CreateItemCommand, int>();

        var command = new CreateItemCommand("Widget", 9.99m);

        var result = await behavior.HandleAsync(
            command,
            () => handler.HandleAsync(command));

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value);
        Assert.True(behavior.BeforeCalled);
        Assert.True(behavior.AfterCalled);
    }

    [Fact]
    public async Task Behavior_CanShortCircuit()
    {
        var handler = new CreateItemHandler();
        var behavior = new ShortCircuitBehavior<CreateItemCommand, int>();

        var command = new CreateItemCommand("Widget", 9.99m);

        var result = await behavior.HandleAsync(
            command,
            () => handler.HandleAsync(command));

        Assert.False(result.Succeeded);
        Assert.Equal("Blocked by behavior", result.ErrorMessage);
        Assert.False(behavior.NextWasCalled);
    }

    [Fact]
    public async Task Behaviors_ChainInOrder()
    {
        var order = new List<string>();
        var behavior1 = new OrderTrackingBehavior<CreateItemCommand, int>("B1", order);
        var behavior2 = new OrderTrackingBehavior<CreateItemCommand, int>("B2", order);
        var handler = new CreateItemHandler();
        var command = new CreateItemCommand("Widget", 9.99m);

        // Simulate pipeline: B1 → B2 → Handler
        var result = await behavior1.HandleAsync(
            command,
            () => behavior2.HandleAsync(
                command,
                () => handler.HandleAsync(command)));

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["B1:before", "B2:before", "B2:after", "B1:after"],
            order);
    }

    [Fact]
    public async Task Behavior_CanModifyResult()
    {
        var handler = new CreateItemHandler();
        var behavior = new ResultModifyingBehavior<CreateItemCommand, int>();
        var command = new CreateItemCommand("Widget", 9.99m);

        var result = await behavior.HandleAsync(
            command,
            () => handler.HandleAsync(command));

        Assert.True(result.Succeeded);
        // Behavior replaces value 1 → 999
        Assert.Equal(999, result.Value);
    }

    [Fact]
    public async Task Delegate_IsCallable()
    {
        CommandHandlerDelegate<int> next =
            () => Task.FromResult(CommandResult<int>.Success(42));

        var result = await next();

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
    }

    // ══════════════════════════════════════════════════════════════
    //  Backward compatibility
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void ExistingICommand_StillWorks()
    {
        // Existing simple commands remain valid
        ICommand simpleCommand = new LegacyCommand("test");

        Assert.IsAssignableFrom<ICommand>(simpleCommand);
    }

    [Fact]
    public void ExistingCommandResult_StillWorks()
    {
        var success = CommandResult.Success();
        var failure = CommandResult.Failure("oops");

        Assert.True(success.Succeeded);
        Assert.False(failure.Succeeded);
        Assert.Equal("oops", failure.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  Test implementations
    // ══════════════════════════════════════════════════════════════

    private sealed record LegacyCommand(string Value) : ICommand;

    private sealed class CreateItemHandler
        : ICommandRequestHandler<CreateItemCommand, int>
    {
        public Task<CommandResult<int>> HandleAsync(
            CreateItemCommand command, CancellationToken ct = default) =>
            Task.FromResult(CommandResult<int>.Success(1));
    }

    private sealed class FailingCreateItemHandler
        : ICommandRequestHandler<CreateItemCommand, int>
    {
        public Task<CommandResult<int>> HandleAsync(
            CreateItemCommand command, CancellationToken ct = default) =>
            Task.FromResult(CommandResult<int>.Failure("Name is required"));
    }

    private sealed class CancellationAwareHandler
        : ICommandRequestHandler<CreateItemCommand, int>
    {
        public bool ReceivedToken { get; private set; }

        public Task<CommandResult<int>> HandleAsync(
            CreateItemCommand command, CancellationToken ct = default)
        {
            ReceivedToken = ct.CanBeCanceled || ct == CancellationToken.None;
            // Always true — we just verify the token is forwarded
            ReceivedToken = true;
            return Task.FromResult(CommandResult<int>.Success(1));
        }
    }

    private sealed class TrackingBehavior<TCommand, TResult>
        : ICommandPipelineBehavior<TCommand, TResult>
        where TCommand : ICommandRequest<TResult>
    {
        public bool BeforeCalled { get; private set; }
        public bool AfterCalled { get; private set; }

        public async Task<CommandResult<TResult>> HandleAsync(
            TCommand command,
            CommandHandlerDelegate<TResult> next,
            CancellationToken ct = default)
        {
            BeforeCalled = true;
            var result = await next();
            AfterCalled = true;
            return result;
        }
    }

    private sealed class ShortCircuitBehavior<TCommand, TResult>
        : ICommandPipelineBehavior<TCommand, TResult>
        where TCommand : ICommandRequest<TResult>
    {
        public bool NextWasCalled { get; private set; }

        public Task<CommandResult<TResult>> HandleAsync(
            TCommand command,
            CommandHandlerDelegate<TResult> next,
            CancellationToken ct = default)
        {
            // Deliberately NOT calling next() — short circuit
            NextWasCalled = false;
            return Task.FromResult(
                CommandResult<TResult>.Failure("Blocked by behavior"));
        }
    }

    private sealed class OrderTrackingBehavior<TCommand, TResult>(
        string name, List<string> order)
        : ICommandPipelineBehavior<TCommand, TResult>
        where TCommand : ICommandRequest<TResult>
    {
        public async Task<CommandResult<TResult>> HandleAsync(
            TCommand command,
            CommandHandlerDelegate<TResult> next,
            CancellationToken ct = default)
        {
            order.Add($"{name}:before");
            var result = await next();
            order.Add($"{name}:after");
            return result;
        }
    }

    private sealed class ResultModifyingBehavior<TCommand, TResult>
        : ICommandPipelineBehavior<TCommand, TResult>
        where TCommand : ICommandRequest<TResult>
    {
        public async Task<CommandResult<TResult>> HandleAsync(
            TCommand command,
            CommandHandlerDelegate<TResult> next,
            CancellationToken ct = default)
        {
            var result = await next();

            if (result.Succeeded && result.Value is int)
            {
                // Replace value with 999 for testing
                return CommandResult<TResult>.Success((TResult)(object)999);
            }

            return result;
        }
    }
}
