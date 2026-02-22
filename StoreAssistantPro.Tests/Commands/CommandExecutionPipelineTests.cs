using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Tests.Commands;

public class CommandExecutionPipelineTests
{
    // ══════════════════════════════════════════════════════════════
    //  Test command types
    // ══════════════════════════════════════════════════════════════

    private sealed record CreateItemCommand(string Name, decimal Price)
        : ICommandRequest<int>;

    private sealed record VoidCommand() : ICommandRequest<bool>;

    // ══════════════════════════════════════════════════════════════
    //  Builder helper — sets up DI container per-test
    // ══════════════════════════════════════════════════════════════

    private sealed class PipelineBuilder
    {
        private readonly ServiceCollection _services = new();

        public PipelineBuilder WithHandler<TCommand, TResult>(
            ICommandRequestHandler<TCommand, TResult> handler)
            where TCommand : ICommandRequest<TResult>
        {
            _services.AddSingleton(handler);
            return this;
        }

        public PipelineBuilder WithBehavior<TCommand, TResult>(
            ICommandPipelineBehavior<TCommand, TResult> behavior)
            where TCommand : ICommandRequest<TResult>
        {
            _services.AddSingleton<ICommandPipelineBehavior<TCommand, TResult>>(behavior);
            return this;
        }

        public ICommandExecutionPipeline Build()
        {
            var provider = _services.BuildServiceProvider();
            return new CommandExecutionPipeline(
                provider,
                NullLogger<CommandExecutionPipeline>.Instance);
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  No behaviors — handler invoked directly
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Execute_NoBehaviors_InvokesHandler()
    {
        var handler = new SuccessHandler(42);
        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("Widget", 9.99m));

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Execute_NoBehaviors_HandlerFailure_ReturnsFailure()
    {
        var handler = new FailingHandler("Stock error");
        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("Widget", 9.99m));

        Assert.False(result.Succeeded);
        Assert.Equal("Stock error", result.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  No handler — throws
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Execute_NoHandler_ThrowsInvalidOperation()
    {
        var pipeline = new PipelineBuilder().Build();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => pipeline.ExecuteAsync<CreateItemCommand, int>(
                new CreateItemCommand("X", 1m)));
    }

    // ══════════════════════════════════════════════════════════════
    //  Single behavior
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Execute_SingleBehavior_WrapsHandler()
    {
        var tracker = new OrderTracker();
        var behavior = new OrderTrackingBehavior<CreateItemCommand, int>("B1", tracker);
        var handler = new OrderTrackingHandler<CreateItemCommand, int>(99, tracker);

        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithBehavior<CreateItemCommand, int>(behavior)
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("Widget", 9.99m));

        Assert.True(result.Succeeded);
        Assert.Equal(99, result.Value);
        Assert.Equal(
            ["B1:before", "Handler", "B1:after"],
            tracker.Steps);
    }

    // ══════════════════════════════════════════════════════════════
    //  Multiple behaviors — execution order
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Execute_MultipleBehaviors_ExecuteInRegistrationOrder()
    {
        var tracker = new OrderTracker();
        var b1 = new OrderTrackingBehavior<CreateItemCommand, int>("B1", tracker);
        var b2 = new OrderTrackingBehavior<CreateItemCommand, int>("B2", tracker);
        var b3 = new OrderTrackingBehavior<CreateItemCommand, int>("B3", tracker);
        var handler = new OrderTrackingHandler<CreateItemCommand, int>(1, tracker);

        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithBehavior<CreateItemCommand, int>(b1)
            .WithBehavior<CreateItemCommand, int>(b2)
            .WithBehavior<CreateItemCommand, int>(b3)
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("W", 1m));

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["B1:before", "B2:before", "B3:before", "Handler", "B3:after", "B2:after", "B1:after"],
            tracker.Steps);
    }

    // ══════════════════════════════════════════════════════════════
    //  Short-circuit behavior
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Execute_BehaviorShortCircuits_HandlerNotCalled()
    {
        var handlerCalled = false;
        var handler = new CallbackHandler<CreateItemCommand, int>(
            _ => { handlerCalled = true; return 1; });
        var blocker = new ShortCircuitBehavior<CreateItemCommand, int>(
            "Validation failed");

        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithBehavior<CreateItemCommand, int>(blocker)
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("", 0m));

        Assert.False(result.Succeeded);
        Assert.Equal("Validation failed", result.ErrorMessage);
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task Execute_SecondBehaviorShortCircuits_FirstBehaviorStillGetsAfter()
    {
        var tracker = new OrderTracker();
        var b1 = new OrderTrackingBehavior<CreateItemCommand, int>("B1", tracker);
        var b2 = new ShortCircuitWithTrackingBehavior<CreateItemCommand, int>(
            "blocked", tracker, "B2");
        var handler = new OrderTrackingHandler<CreateItemCommand, int>(1, tracker);

        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithBehavior<CreateItemCommand, int>(b1)
            .WithBehavior<CreateItemCommand, int>(b2)
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("W", 1m));

        Assert.False(result.Succeeded);
        // B2 short-circuits, handler never called, B1 still gets after
        Assert.Equal(
            ["B1:before", "B2:short-circuit", "B1:after"],
            tracker.Steps);
    }

    // ══════════════════════════════════════════════════════════════
    //  CancellationToken forwarding
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Execute_ForwardsCancellationTokenToHandler()
    {
        CancellationToken? capturedCt = null;
        var handler = new CancellationCapturingHandler<CreateItemCommand, int>(
            ct => { capturedCt = ct; return 1; });

        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .Build();

        using var cts = new CancellationTokenSource();
        await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("W", 1m), cts.Token);

        Assert.Equal(cts.Token, capturedCt);
    }

    [Fact]
    public async Task Execute_ForwardsCancellationTokenToBehavior()
    {
        CancellationToken? capturedCt = null;
        var behavior = new CancellationCapturingBehavior<CreateItemCommand, int>(
            ct => capturedCt = ct);
        var handler = new SuccessHandler(1);

        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithBehavior<CreateItemCommand, int>(behavior)
            .Build();

        using var cts = new CancellationTokenSource();
        await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("W", 1m), cts.Token);

        Assert.Equal(cts.Token, capturedCt);
    }

    // ══════════════════════════════════════════════════════════════
    //  Result modification by behavior
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Execute_BehaviorCanModifyResult()
    {
        var handler = new SuccessHandler(10);
        var doubler = new ResultDoublingBehavior();

        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithBehavior<CreateItemCommand, int>(doubler)
            .Build();

        var result = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("W", 1m));

        Assert.True(result.Succeeded);
        Assert.Equal(20, result.Value); // 10 * 2
    }

    // ══════════════════════════════════════════════════════════════
    //  Different command types are isolated
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Execute_DifferentCommandTypes_UseOwnBehaviors()
    {
        var tracker = new OrderTracker();
        var intHandler = new OrderTrackingHandler<CreateItemCommand, int>(42, tracker);
        var boolHandler = new OrderTrackingHandler<VoidCommand, bool>(true, tracker);
        var intBehavior = new OrderTrackingBehavior<CreateItemCommand, int>("IntB", tracker);
        // No behavior for VoidCommand

        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(intHandler)
            .WithHandler<VoidCommand, bool>(boolHandler)
            .WithBehavior<CreateItemCommand, int>(intBehavior)
            .Build();

        tracker.Steps.Clear();
        var intResult = await pipeline.ExecuteAsync<CreateItemCommand, int>(
            new CreateItemCommand("W", 1m));
        var intSteps = tracker.Steps.ToList();

        tracker.Steps.Clear();
        var boolResult = await pipeline.ExecuteAsync<VoidCommand, bool>(
            new VoidCommand());
        var boolSteps = tracker.Steps.ToList();

        Assert.Equal(42, intResult.Value);
        Assert.Equal(["IntB:before", "Handler", "IntB:after"], intSteps);

        Assert.True(boolResult.Value);
        Assert.Equal(["Handler"], boolSteps); // no behavior
    }

    // ══════════════════════════════════════════════════════════════
    //  Command data flows through entire pipeline
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Execute_CommandDataReachesHandler()
    {
        CreateItemCommand? captured = null;
        var handler = new CallbackHandler<CreateItemCommand, int>(
            cmd => { captured = cmd; return 1; });

        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .Build();

        var command = new CreateItemCommand("TestItem", 99.99m);
        await pipeline.ExecuteAsync<CreateItemCommand, int>(command);

        Assert.NotNull(captured);
        Assert.Equal("TestItem", captured.Name);
        Assert.Equal(99.99m, captured.Price);
    }

    [Fact]
    public async Task Execute_CommandDataReachesBehavior()
    {
        CreateItemCommand? captured = null;
        var behavior = new CommandCapturingBehavior<CreateItemCommand, int>(
            cmd => captured = cmd);
        var handler = new SuccessHandler(1);

        var pipeline = new PipelineBuilder()
            .WithHandler<CreateItemCommand, int>(handler)
            .WithBehavior<CreateItemCommand, int>(behavior)
            .Build();

        var command = new CreateItemCommand("TestItem", 99.99m);
        await pipeline.ExecuteAsync<CreateItemCommand, int>(command);

        Assert.NotNull(captured);
        Assert.Equal("TestItem", captured.Name);
    }

    // ══════════════════════════════════════════════════════════════
    //  Test implementations
    // ══════════════════════════════════════════════════════════════

    private sealed class OrderTracker
    {
        public List<string> Steps { get; } = [];
    }

    private sealed class SuccessHandler(int value)
        : ICommandRequestHandler<CreateItemCommand, int>
    {
        public Task<CommandResult<int>> HandleAsync(
            CreateItemCommand command, CancellationToken ct = default) =>
            Task.FromResult(CommandResult<int>.Success(value));
    }

    private sealed class FailingHandler(string error)
        : ICommandRequestHandler<CreateItemCommand, int>
    {
        public Task<CommandResult<int>> HandleAsync(
            CreateItemCommand command, CancellationToken ct = default) =>
            Task.FromResult(CommandResult<int>.Failure(error));
    }

    private sealed class CallbackHandler<TCommand, TResult>(
        Func<TCommand, TResult> callback)
        : ICommandRequestHandler<TCommand, TResult>
        where TCommand : ICommandRequest<TResult>
    {
        public Task<CommandResult<TResult>> HandleAsync(
            TCommand command, CancellationToken ct = default) =>
            Task.FromResult(CommandResult<TResult>.Success(callback(command)));
    }

    private sealed class OrderTrackingHandler<TCommand, TResult>(
        TResult value, OrderTracker tracker)
        : ICommandRequestHandler<TCommand, TResult>
        where TCommand : ICommandRequest<TResult>
    {
        public Task<CommandResult<TResult>> HandleAsync(
            TCommand command, CancellationToken ct = default)
        {
            tracker.Steps.Add("Handler");
            return Task.FromResult(CommandResult<TResult>.Success(value));
        }
    }

    private sealed class OrderTrackingBehavior<TCommand, TResult>(
        string name, OrderTracker tracker)
        : ICommandPipelineBehavior<TCommand, TResult>
        where TCommand : ICommandRequest<TResult>
    {
        public async Task<CommandResult<TResult>> HandleAsync(
            TCommand command,
            CommandHandlerDelegate<TResult> next,
            CancellationToken ct = default)
        {
            tracker.Steps.Add($"{name}:before");
            var result = await next();
            tracker.Steps.Add($"{name}:after");
            return result;
        }
    }

    private sealed class ShortCircuitBehavior<TCommand, TResult>(string error)
        : ICommandPipelineBehavior<TCommand, TResult>
        where TCommand : ICommandRequest<TResult>
    {
        public Task<CommandResult<TResult>> HandleAsync(
            TCommand command,
            CommandHandlerDelegate<TResult> next,
            CancellationToken ct = default) =>
            Task.FromResult(CommandResult<TResult>.Failure(error));
    }

    private sealed class ShortCircuitWithTrackingBehavior<TCommand, TResult>(
        string error, OrderTracker tracker, string name)
        : ICommandPipelineBehavior<TCommand, TResult>
        where TCommand : ICommandRequest<TResult>
    {
        public Task<CommandResult<TResult>> HandleAsync(
            TCommand command,
            CommandHandlerDelegate<TResult> next,
            CancellationToken ct = default)
        {
            tracker.Steps.Add($"{name}:short-circuit");
            return Task.FromResult(CommandResult<TResult>.Failure(error));
        }
    }

    private sealed class CancellationCapturingHandler<TCommand, TResult>(
        Func<CancellationToken, TResult> callback)
        : ICommandRequestHandler<TCommand, TResult>
        where TCommand : ICommandRequest<TResult>
    {
        public Task<CommandResult<TResult>> HandleAsync(
            TCommand command, CancellationToken ct = default) =>
            Task.FromResult(CommandResult<TResult>.Success(callback(ct)));
    }

    private sealed class CancellationCapturingBehavior<TCommand, TResult>(
        Action<CancellationToken> capture)
        : ICommandPipelineBehavior<TCommand, TResult>
        where TCommand : ICommandRequest<TResult>
    {
        public async Task<CommandResult<TResult>> HandleAsync(
            TCommand command,
            CommandHandlerDelegate<TResult> next,
            CancellationToken ct = default)
        {
            capture(ct);
            return await next();
        }
    }

    private sealed class ResultDoublingBehavior
        : ICommandPipelineBehavior<CreateItemCommand, int>
    {
        public async Task<CommandResult<int>> HandleAsync(
            CreateItemCommand command,
            CommandHandlerDelegate<int> next,
            CancellationToken ct = default)
        {
            var result = await next();
            if (result is { Succeeded: true, Value: int v })
                return CommandResult<int>.Success(v * 2);
            return result;
        }
    }

    private sealed class CommandCapturingBehavior<TCommand, TResult>(
        Action<TCommand> capture)
        : ICommandPipelineBehavior<TCommand, TResult>
        where TCommand : ICommandRequest<TResult>
    {
        public async Task<CommandResult<TResult>> HandleAsync(
            TCommand command,
            CommandHandlerDelegate<TResult> next,
            CancellationToken ct = default)
        {
            capture(command);
            return await next();
        }
    }
}
