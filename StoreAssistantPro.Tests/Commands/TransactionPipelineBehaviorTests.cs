using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Commands.Transaction;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Commands;

public class TransactionPipelineBehaviorTests
{
    // ══════════════════════════════════════════════════════════════
    //  Test command types
    // ══════════════════════════════════════════════════════════════

    /// <summary>Transactional — should be wrapped.</summary>
    private sealed record TransactionalCommand(string Name)
        : ICommandRequest<int>, ITransactionalCommand;

    /// <summary>Non-transactional — should pass through.</summary>
    private sealed record ReadOnlyCommand(string Query)
        : ICommandRequest<string>;

    // ══════════════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════════════

    private static readonly ILogger<TransactionPipelineBehavior<TransactionalCommand, int>>
        TxLogger = NullLogger<TransactionPipelineBehavior<TransactionalCommand, int>>.Instance;

    private static readonly ILogger<TransactionPipelineBehavior<ReadOnlyCommand, string>>
        ReadLogger = NullLogger<TransactionPipelineBehavior<ReadOnlyCommand, string>>.Instance;

    // ══════════════════════════════════════════════════════════════
    //  Non-transactional commands — pass-through
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task NonTransactional_SkipsTransaction_CallsNextDirectly()
    {
        var txService = Substitute.For<ITransactionSafetyService>();
        var behavior = new TransactionPipelineBehavior<ReadOnlyCommand, string>(
            txService, ReadLogger);

        var result = await behavior.HandleAsync(
            new ReadOnlyCommand("SELECT *"),
            () => Task.FromResult(CommandResult<string>.Success("data")));

        Assert.True(result.Succeeded);
        Assert.Equal("data", result.Value);
        await txService.DidNotReceiveWithAnyArgs()
            .ExecuteSafeAsync(Arg.Any<Func<Task<CommandResult<string>>>>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Transactional commands — success path
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Transactional_Success_CommitsAndReturnsResult()
    {
        var txService = Substitute.For<ITransactionSafetyService>();
        var expectedResult = CommandResult<int>.Success(42);

        txService.ExecuteSafeAsync(Arg.Any<Func<Task<CommandResult<int>>>>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<Task<CommandResult<int>>>>();
                return operation().ContinueWith(t =>
                    TransactionResult<CommandResult<int>>.Success(t.Result));
            });

        var behavior = new TransactionPipelineBehavior<TransactionalCommand, int>(
            txService, TxLogger);

        var result = await behavior.HandleAsync(
            new TransactionalCommand("Widget"),
            () => Task.FromResult(expectedResult));

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Transactional_Success_InvokesTransactionService()
    {
        var txService = Substitute.For<ITransactionSafetyService>();
        txService.ExecuteSafeAsync(Arg.Any<Func<Task<CommandResult<int>>>>())
            .Returns(callInfo =>
            {
                var op = callInfo.Arg<Func<Task<CommandResult<int>>>>();
                return op().ContinueWith(t =>
                    TransactionResult<CommandResult<int>>.Success(t.Result));
            });

        var behavior = new TransactionPipelineBehavior<TransactionalCommand, int>(
            txService, TxLogger);

        await behavior.HandleAsync(
            new TransactionalCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        await txService.Received(1)
            .ExecuteSafeAsync(Arg.Any<Func<Task<CommandResult<int>>>>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Transactional commands — handler failure → rollback
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Transactional_HandlerFails_RollsBackAndReturnsFailure()
    {
        var txService = Substitute.For<ITransactionSafetyService>();

        // When the inner handler returns failure, the behavior throws
        // inside the transaction so TransactionSafetyService rolls back.
        // We simulate that by catching the throw and returning failure.
        txService.ExecuteSafeAsync(Arg.Any<Func<Task<CommandResult<int>>>>())
            .Returns(async callInfo =>
            {
                var op = callInfo.Arg<Func<Task<CommandResult<int>>>>();
                try
                {
                    await op();
                    return TransactionResult<CommandResult<int>>.Success(
                        CommandResult<int>.Success(0));
                }
                catch (Exception ex)
                {
                    return TransactionResult<CommandResult<int>>.Failure(ex.Message);
                }
            });

        var behavior = new TransactionPipelineBehavior<TransactionalCommand, int>(
            txService, TxLogger);

        var result = await behavior.HandleAsync(
            new TransactionalCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Failure("Validation error")));

        Assert.False(result.Succeeded);
        Assert.Equal("Validation error", result.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  Transaction service failure — mapped to CommandResult
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Transactional_TransactionFails_ReturnsCommandFailure()
    {
        var txService = Substitute.For<ITransactionSafetyService>();
        txService.ExecuteSafeAsync(Arg.Any<Func<Task<CommandResult<int>>>>())
            .Returns(TransactionResult<CommandResult<int>>.Failure(
                "Database connection lost"));

        var behavior = new TransactionPipelineBehavior<TransactionalCommand, int>(
            txService, TxLogger);

        var result = await behavior.HandleAsync(
            new TransactionalCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        Assert.False(result.Succeeded);
        Assert.Equal("Database connection lost", result.ErrorMessage);
    }

    [Fact]
    public async Task Transactional_ConcurrencyConflict_ReturnsFailure()
    {
        var txService = Substitute.For<ITransactionSafetyService>();
        txService.ExecuteSafeAsync(Arg.Any<Func<Task<CommandResult<int>>>>())
            .Returns(TransactionResult<CommandResult<int>>.Failure(
                "Row modified by another user",
                isConcurrencyConflict: true));

        var behavior = new TransactionPipelineBehavior<TransactionalCommand, int>(
            txService, TxLogger);

        var result = await behavior.HandleAsync(
            new TransactionalCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        Assert.False(result.Succeeded);
        Assert.Contains("Row modified", result.ErrorMessage);
    }

    [Fact]
    public async Task Transactional_ConstraintViolation_ReturnsFailure()
    {
        var txService = Substitute.For<ITransactionSafetyService>();
        txService.ExecuteSafeAsync(Arg.Any<Func<Task<CommandResult<int>>>>())
            .Returns(TransactionResult<CommandResult<int>>.Failure(
                "Unique constraint violated",
                isConstraintViolation: true));

        var behavior = new TransactionPipelineBehavior<TransactionalCommand, int>(
            txService, TxLogger);

        var result = await behavior.HandleAsync(
            new TransactionalCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        Assert.False(result.Succeeded);
        Assert.Contains("Unique constraint", result.ErrorMessage);
    }

    [Fact]
    public async Task Transactional_Cancelled_ReturnsFailure()
    {
        var txService = Substitute.For<ITransactionSafetyService>();
        txService.ExecuteSafeAsync(Arg.Any<Func<Task<CommandResult<int>>>>())
            .Returns(TransactionResult<CommandResult<int>>.Failure(
                "Operation cancelled",
                isCancelled: true));

        var behavior = new TransactionPipelineBehavior<TransactionalCommand, int>(
            txService, TxLogger);

        var result = await behavior.HandleAsync(
            new TransactionalCommand("Widget"),
            () => Task.FromResult(CommandResult<int>.Success(1)));

        Assert.False(result.Succeeded);
        Assert.Contains("cancelled", result.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  Handler is called inside the transaction
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Transactional_HandlerCalledInsideTransaction()
    {
        var handlerCalledInsideTx = false;
        var txService = Substitute.For<ITransactionSafetyService>();
        txService.ExecuteSafeAsync(Arg.Any<Func<Task<CommandResult<int>>>>())
            .Returns(async callInfo =>
            {
                var op = callInfo.Arg<Func<Task<CommandResult<int>>>>();
                var innerResult = await op();
                return TransactionResult<CommandResult<int>>.Success(innerResult);
            });

        var behavior = new TransactionPipelineBehavior<TransactionalCommand, int>(
            txService, TxLogger);

        await behavior.HandleAsync(
            new TransactionalCommand("Widget"),
            () =>
            {
                handlerCalledInsideTx = true;
                return Task.FromResult(CommandResult<int>.Success(1));
            });

        Assert.True(handlerCalledInsideTx);
    }

    // ══════════════════════════════════════════════════════════════
    //  Marker interface detection
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void TransactionalCommand_ImplementsMarkerInterface()
    {
        Assert.IsAssignableFrom<ITransactionalCommand>(
            new TransactionalCommand("test"));
    }

    [Fact]
    public void ReadOnlyCommand_DoesNotImplementMarkerInterface()
    {
        Assert.IsNotAssignableFrom<ITransactionalCommand>(
            new ReadOnlyCommand("test"));
    }

    [Fact]
    public void TransactionalCommand_AlsoImplementsICommandRequest()
    {
        Assert.IsAssignableFrom<ICommandRequest<int>>(
            new TransactionalCommand("test"));
    }

    [Fact]
    public void TransactionalCommand_AlsoImplementsICommand()
    {
        Assert.IsAssignableFrom<ICommand>(
            new TransactionalCommand("test"));
    }

    // ══════════════════════════════════════════════════════════════
    //  Result passthrough fidelity
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task NonTransactional_Failure_PassedThrough()
    {
        var txService = Substitute.For<ITransactionSafetyService>();
        var behavior = new TransactionPipelineBehavior<ReadOnlyCommand, string>(
            txService, ReadLogger);

        var result = await behavior.HandleAsync(
            new ReadOnlyCommand("bad query"),
            () => Task.FromResult(CommandResult<string>.Failure("syntax error")));

        Assert.False(result.Succeeded);
        Assert.Equal("syntax error", result.ErrorMessage);
    }
}
