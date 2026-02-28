using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

public class TransactionSafetyServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);
    private readonly ILogger<TransactionSafetyService> _logger =
        NullLogger<TransactionSafetyService>.Instance;

    public TransactionSafetyServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        using var db = new AppDbContext(_dbOptions);
        db.UserCredentials.Add(new UserCredential
        {
            UserType = UserType.Admin,
            PinHash = "test-hash"
        });
        db.SaveChanges();
    }

    private IDbContextFactory<AppDbContext> CreateFactory()
    {
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(_dbOptions)));
        return factory;
    }

    private TransactionSafetyService CreateSut() =>
        new(CreateFactory(), _eventBus, _perf, _logger);

    // ══════════════════════════════════════════════════════════════
    //  Service-layer overload: ExecuteSafeAsync(Func<Task>)
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ServiceLayer_Success_ReturnsSucceeded()
    {
        var sut = CreateSut();
        var called = false;

        var result = await sut.ExecuteSafeAsync(() =>
        {
            called = true;
            return Task.CompletedTask;
        });

        Assert.True(result.Succeeded);
        Assert.True(called);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.Exception);
        Assert.False(result.IsConcurrencyConflict);
        Assert.False(result.IsConstraintViolation);
        Assert.False(result.IsCancelled);
    }

    [Fact]
    public async Task ServiceLayer_OperationThrows_ReturnsFailure()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(
            () => Task.FromException(new InvalidOperationException("Stock insufficient")));

        Assert.False(result.Succeeded);
        Assert.Equal("Stock insufficient", result.ErrorMessage);
        Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.False(result.IsConcurrencyConflict);
    }

    [Fact]
    public async Task ServiceLayer_ConcurrencyConflict_FlaggedCorrectly()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(
            () => Task.FromException(new DbUpdateConcurrencyException("conflict")));

        Assert.False(result.Succeeded);
        Assert.True(result.IsConcurrencyConflict);
        Assert.False(result.IsConstraintViolation);
        Assert.False(result.IsCancelled);
        Assert.Contains("another user", result.ErrorMessage);
    }

    [Fact]
    public async Task ServiceLayer_NeverThrowsToCallers()
    {
        var sut = CreateSut();

        var ex = await Record.ExceptionAsync(() =>
            sut.ExecuteSafeAsync(() => throw new OutOfMemoryException("boom")));

        Assert.Null(ex);
    }

    [Fact]
    public async Task ServiceLayer_DbUpdateException_SetsConstraintFlag()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(
            () => Task.FromException(new DbUpdateException("FK violation")));

        Assert.False(result.Succeeded);
        Assert.True(result.IsConstraintViolation);
        Assert.False(result.IsConcurrencyConflict);
        Assert.False(result.IsCancelled);
        Assert.Contains("data conflict", result.ErrorMessage);
    }

    [Fact]
    public async Task ServiceLayer_Cancellation_SetsCancelledFlag()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(
            () => Task.FromException(new OperationCanceledException("cancelled")));

        Assert.False(result.Succeeded);
        Assert.True(result.IsCancelled);
        Assert.False(result.IsConcurrencyConflict);
        Assert.False(result.IsConstraintViolation);
        Assert.Contains("cancelled", result.ErrorMessage);
    }

    [Fact]
    public async Task ServiceLayer_TaskCanceledException_SetsCancelledFlag()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(
            () => Task.FromException(new TaskCanceledException("timed out")));

        Assert.False(result.Succeeded);
        Assert.True(result.IsCancelled);
    }

    [Fact]
    public async Task ServiceLayer_UnexpectedException_ReturnsSafeMessage()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(
            () => Task.FromException(new NullReferenceException("Object reference not set")));

        Assert.False(result.Succeeded);
        Assert.DoesNotContain("Object reference", result.ErrorMessage);
        Assert.Contains("unexpected error", result.ErrorMessage);
        Assert.IsType<NullReferenceException>(result.Exception);
    }

    // ══════════════════════════════════════════════════════════════
    //  Service-layer overload: ExecuteSafeAsync<T>(Func<Task<T>>)
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ServiceLayerTyped_Success_ReturnsValue()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(() => Task.FromResult(42));

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task ServiceLayerTyped_Failure_ReturnsDefaultValue()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync<int>(
            () => Task.FromException<int>(new InvalidOperationException("fail")));

        Assert.False(result.Succeeded);
        Assert.Equal(default, result.Value);
        Assert.Equal("fail", result.ErrorMessage);
    }

    [Fact]
    public async Task ServiceLayerTyped_ConcurrencyConflict_FlaggedCorrectly()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync<string>(
            () => Task.FromException<string>(new DbUpdateConcurrencyException("conflict")));

        Assert.False(result.Succeeded);
        Assert.True(result.IsConcurrencyConflict);
    }

    [Fact]
    public async Task ServiceLayerTyped_DbUpdateException_SetsConstraintFlag()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync<int>(
            () => Task.FromException<int>(new DbUpdateException("unique violation")));

        Assert.False(result.Succeeded);
        Assert.True(result.IsConstraintViolation);
        Assert.False(result.IsConcurrencyConflict);
    }

    [Fact]
    public async Task ServiceLayerTyped_Cancellation_SetsCancelledFlag()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync<int>(
            () => Task.FromException<int>(new OperationCanceledException()));

        Assert.False(result.Succeeded);
        Assert.True(result.IsCancelled);
    }

    // ══════════════════════════════════════════════════════════════
    //  Transactional overload: ExecuteSafeAsync(Func<AppDbContext, Task>)
    //
    //  Note: InMemory provider does not support real transactions.
    //  The InMemory DatabaseFacade.BeginTransactionAsync throws.
    //  These tests verify the error-handling path and that the
    //  service-layer overloads work correctly in isolation.
    //  Full transactional tests require a real SQL Server database.
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Transactional_InMemoryProvider_ReturnsFailure()
    {
        // InMemory doesn't support transactions — the service should
        // catch the exception and return a failure result, never throw.
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(async (AppDbContext db) =>
        {
            db.Products.Add(new Product { Name = "Test", SalePrice = 10m });
            await Task.CompletedTask;
        });

        // The call should not throw — failure is captured in the result
        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task TransactionalTyped_InMemoryProvider_ReturnsFailure()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(async (AppDbContext db) =>
        {
            db.Products.Add(new Product { Name = "Test", SalePrice = 10m });
            await Task.CompletedTask;
            return 1;
        });

        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  TransactionResult model tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void TransactionResult_Success_Properties()
    {
        var result = TransactionResult.Success();

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.Exception);
        Assert.False(result.IsConcurrencyConflict);
        Assert.False(result.IsConstraintViolation);
        Assert.False(result.IsCancelled);
    }

    [Fact]
    public void TransactionResult_Failure_Properties()
    {
        var ex = new InvalidOperationException("bad");
        var result = TransactionResult.Failure("bad", ex);

        Assert.False(result.Succeeded);
        Assert.Equal("bad", result.ErrorMessage);
        Assert.Same(ex, result.Exception);
        Assert.False(result.IsConcurrencyConflict);
    }

    [Fact]
    public void TransactionResult_ConcurrencyFailure_Properties()
    {
        var result = TransactionResult.Failure("conflict", isConcurrencyConflict: true);

        Assert.True(result.IsConcurrencyConflict);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void TransactionResult_ConstraintViolation_Properties()
    {
        var result = TransactionResult.Failure("duplicate", isConstraintViolation: true);

        Assert.True(result.IsConstraintViolation);
        Assert.False(result.IsConcurrencyConflict);
        Assert.False(result.IsCancelled);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void TransactionResult_Cancelled_Properties()
    {
        var result = TransactionResult.Failure("cancelled", isCancelled: true);

        Assert.True(result.IsCancelled);
        Assert.False(result.IsConcurrencyConflict);
        Assert.False(result.IsConstraintViolation);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void TransactionResultTyped_Success_Properties()
    {
        var result = TransactionResult<int>.Success(99);

        Assert.True(result.Succeeded);
        Assert.Equal(99, result.Value);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void TransactionResultTyped_Failure_Properties()
    {
        var result = TransactionResult<int>.Failure("nope");

        Assert.False(result.Succeeded);
        Assert.Equal(default, result.Value);
        Assert.Equal("nope", result.ErrorMessage);
    }

    [Fact]
    public void TransactionResultTyped_ConstraintViolation_Properties()
    {
        var result = TransactionResult<int>.Failure("dup", isConstraintViolation: true);

        Assert.True(result.IsConstraintViolation);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void TransactionResultTyped_Cancelled_Properties()
    {
        var result = TransactionResult<int>.Failure("cancelled", isCancelled: true);

        Assert.True(result.IsCancelled);
        Assert.False(result.Succeeded);
    }

    // ══════════════════════════════════════════════════════════════
    //  Multiple sequential operations
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ServiceLayer_MultipleSequentialCalls_IndependentResults()
    {
        var sut = CreateSut();

        var r1 = await sut.ExecuteSafeAsync(() => Task.CompletedTask);
        var r2 = await sut.ExecuteSafeAsync(
            () => Task.FromException(new InvalidOperationException("fail")));
        var r3 = await sut.ExecuteSafeAsync(() => Task.CompletedTask);

        Assert.True(r1.Succeeded);
        Assert.False(r2.Succeeded);
        Assert.True(r3.Succeeded);
    }

    [Fact]
    public async Task ServiceLayer_OperationWithSideEffects_ExecutesSideEffect()
    {
        var sut = CreateSut();
        var counter = 0;

        await sut.ExecuteSafeAsync(() =>
        {
            counter++;
            return Task.CompletedTask;
        });

        await sut.ExecuteSafeAsync(() =>
        {
            counter++;
            return Task.CompletedTask;
        });

        Assert.Equal(2, counter);
    }

    // ══════════════════════════════════════════════════════════════
    //  Error message sanitisation
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ServiceLayer_GenericException_DoesNotLeakRawMessage()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(
            () => Task.FromException(new ArgumentException("internal detail XYZ")));

        Assert.False(result.Succeeded);
        Assert.DoesNotContain("internal detail XYZ", result.ErrorMessage);
        Assert.Contains("unexpected error", result.ErrorMessage);
        // Original exception is still accessible for logging
        Assert.Equal("internal detail XYZ", result.Exception!.Message);
    }

    [Fact]
    public async Task ServiceLayer_DbUpdateException_DoesNotLeakSqlDetails()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(
            () => Task.FromException(new DbUpdateException(
                "An error occurred while saving the entity changes. " +
                "See the inner exception for details.",
                new Exception("UNIQUE constraint failed: Sales.IdempotencyKey"))));

        Assert.False(result.Succeeded);
        Assert.DoesNotContain("UNIQUE constraint", result.ErrorMessage);
        Assert.DoesNotContain("IdempotencyKey", result.ErrorMessage);
        Assert.Contains("data conflict", result.ErrorMessage);
    }

    [Fact]
    public async Task ServiceLayer_InvalidOperation_PreservesBusinessMessage()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(
            () => Task.FromException(new InvalidOperationException("Insufficient stock for 'Widget'. Available: 3.")));

        Assert.False(result.Succeeded);
        Assert.Equal("Insufficient stock for 'Widget'. Available: 3.", result.ErrorMessage);
    }

    // ══════════════════════════════════════════════════════════════
    //  Transaction lifecycle events
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ServiceLayer_Success_PublishesStartedAndCommitted()
    {
        var sut = CreateSut();

        await sut.ExecuteSafeAsync(() => Task.CompletedTask);

        await _eventBus.Received(1).PublishAsync(
            Arg.Any<TransactionStartedEvent>());
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<TransactionCommittedEvent>(e =>
                e.OperationScope == "Service-layer operation" &&
                e.Elapsed >= TimeSpan.Zero));
        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<TransactionFailedEvent>());
    }

    [Fact]
    public async Task ServiceLayer_Failure_PublishesStartedAndFailed()
    {
        var sut = CreateSut();

        await sut.ExecuteSafeAsync(
            () => Task.FromException(new InvalidOperationException("boom")));

        await _eventBus.Received(1).PublishAsync(
            Arg.Any<TransactionStartedEvent>());
        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<TransactionCommittedEvent>());
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<TransactionFailedEvent>(e =>
                e.OperationScope == "Service-layer operation" &&
                e.ErrorMessage == "boom"));
    }

    [Fact]
    public async Task ServiceLayerTyped_Success_PublishesStartedAndCommitted()
    {
        var sut = CreateSut();

        await sut.ExecuteSafeAsync(() => Task.FromResult(42));

        await _eventBus.Received(1).PublishAsync(
            Arg.Any<TransactionStartedEvent>());
        await _eventBus.Received(1).PublishAsync(
            Arg.Any<TransactionCommittedEvent>());
    }

    [Fact]
    public async Task ServiceLayerTyped_Failure_PublishesStartedAndFailed()
    {
        var sut = CreateSut();

        await sut.ExecuteSafeAsync<int>(
            () => Task.FromException<int>(new InvalidOperationException("nope")));

        await _eventBus.Received(1).PublishAsync(
            Arg.Any<TransactionStartedEvent>());
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<TransactionFailedEvent>(e => e.ErrorMessage == "nope"));
    }

    [Fact]
    public async Task Transactional_InMemory_PublishesStartedAndFailed()
    {
        // InMemory doesn't support transactions — triggers failure path
        var sut = CreateSut();

        await sut.ExecuteSafeAsync(async (AppDbContext db) =>
        {
            db.Products.Add(new Product { Name = "Test", SalePrice = 10m });
            await Task.CompletedTask;
        });

        await _eventBus.Received(1).PublishAsync(
            Arg.Any<TransactionStartedEvent>());
        await _eventBus.Received(1).PublishAsync(
            Arg.Any<TransactionFailedEvent>());
        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<TransactionCommittedEvent>());
    }

    [Fact]
    public async Task LifecycleEvents_ShareCorrelatedOperationId()
    {
        var sut = CreateSut();
        Guid? startedId = null;
        Guid? committedId = null;

        _eventBus.PublishAsync(Arg.Any<TransactionStartedEvent>())
            .Returns(ci =>
            {
                startedId = ci.Arg<TransactionStartedEvent>().OperationId;
                return Task.CompletedTask;
            });
        _eventBus.PublishAsync(Arg.Any<TransactionCommittedEvent>())
            .Returns(ci =>
            {
                committedId = ci.Arg<TransactionCommittedEvent>().OperationId;
                return Task.CompletedTask;
            });

        await sut.ExecuteSafeAsync(() => Task.CompletedTask);

        Assert.NotNull(startedId);
        Assert.NotNull(committedId);
        Assert.Equal(startedId, committedId);
        Assert.NotEqual(Guid.Empty, startedId!.Value);
    }

    [Fact]
    public async Task LifecycleEvents_FailedCarriesClassificationFlags()
    {
        var sut = CreateSut();

        await sut.ExecuteSafeAsync(
            () => Task.FromException(new DbUpdateConcurrencyException("conflict")));

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<TransactionFailedEvent>(e =>
                e.IsConcurrencyConflict &&
                !e.IsConstraintViolation &&
                !e.IsCancelled));
    }

    [Fact]
    public async Task LifecycleEvents_CancellationCarriesCancelledFlag()
    {
        var sut = CreateSut();

        await sut.ExecuteSafeAsync(
            () => Task.FromException(new OperationCanceledException()));

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<TransactionFailedEvent>(e =>
                e.IsCancelled &&
                !e.IsConcurrencyConflict &&
                !e.IsConstraintViolation));
    }

    [Fact]
    public async Task LifecycleEvents_ConstraintViolationCarriesFlag()
    {
        var sut = CreateSut();

        await sut.ExecuteSafeAsync(
            () => Task.FromException(new DbUpdateException("constraint")));

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<TransactionFailedEvent>(e =>
                e.IsConstraintViolation &&
                !e.IsConcurrencyConflict &&
                !e.IsCancelled));
    }

    [Fact]
    public async Task EventBusFailure_DoesNotBreakTransaction()
    {
        _eventBus.PublishAsync(Arg.Any<TransactionStartedEvent>())
            .Returns(Task.FromException(new Exception("event bus down")));

        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(() => Task.CompletedTask);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task EventBusFailure_OnCommitted_DoesNotBreakResult()
    {
        _eventBus.PublishAsync(Arg.Any<TransactionCommittedEvent>())
            .Returns(Task.FromException(new Exception("event bus down")));

        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(() => Task.CompletedTask);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task EventBusFailure_OnFailed_DoesNotBreakResult()
    {
        _eventBus.PublishAsync(Arg.Any<TransactionFailedEvent>())
            .Returns(Task.FromException(new Exception("event bus down")));

        var sut = CreateSut();

        var result = await sut.ExecuteSafeAsync(
            () => Task.FromException(new InvalidOperationException("op fail")));

        Assert.False(result.Succeeded);
        Assert.Equal("op fail", result.ErrorMessage);
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
