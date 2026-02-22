using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Commands.Validation;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Commands;
using StoreAssistantPro.Modules.Sales.Events;
using StoreAssistantPro.Modules.Sales.Models;
using StoreAssistantPro.Modules.Sales.Services;

namespace StoreAssistantPro.Tests.Commands;

public class SaveBillCommandTests
{
    // ══════════════════════════════════════════════════════════════
    //  Shared fakes
    // ══════════════════════════════════════════════════════════════

    private readonly ISalesService _salesService = Substitute.For<ISalesService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IBillCalculationService _billCalculation = new BillCalculationService();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IOfflineModeService _offlineMode = Substitute.For<IOfflineModeService>();
    private readonly IOfflineBillingQueue _offlineQueue = Substitute.For<IOfflineBillingQueue>();

    public SaveBillCommandTests()
    {
        _regional.Now.Returns(new DateTime(2026, 6, 15, 10, 0, 0));
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Success(1));
        _offlineMode.IsOffline.Returns(false);
    }

    private SaveBillCommandHandler CreateHandler() =>
        new(_salesService, _eventBus, _billCalculation, _regional,
            _offlineMode, _offlineQueue);

    private static Guid NewKey() => Guid.NewGuid();

    private static SaveBillCommand ValidCommand(
        Guid? key = null,
        string paymentMethod = "Cash",
        IReadOnlyList<BillItemDto>? items = null,
        BillDiscount? discount = null) =>
        new(key ?? NewKey(),
            paymentMethod,
            items ?? [new BillItemDto(1, 2, 50m)],
            discount ?? BillDiscount.None);

    // ══════════════════════════════════════════════════════════════
    //  COMMAND — marker interfaces
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Command_ImplementsICommandRequest()
    {
        var cmd = ValidCommand();
        Assert.IsAssignableFrom<ICommandRequest<int>>(cmd);
    }

    [Fact]
    public void Command_ImplementsITransactionalCommand()
    {
        var cmd = ValidCommand();
        Assert.IsAssignableFrom<ITransactionalCommand>(cmd);
    }

    [Fact]
    public void Command_ImplementsIOfflineCapableCommand()
    {
        var cmd = ValidCommand();
        Assert.IsAssignableFrom<IOfflineCapableCommand>(cmd);
    }

    [Fact]
    public void Command_PreservesData()
    {
        var key = Guid.NewGuid();
        var items = new List<BillItemDto> { new(1, 3, 10m) };
        var discount = new BillDiscount { Type = DiscountType.Percentage, Value = 5m };
        var cmd = new SaveBillCommand(key, "Card", items, discount);

        Assert.Equal(key, cmd.IdempotencyKey);
        Assert.Equal("Card", cmd.PaymentMethod);
        Assert.Single(cmd.Items);
        Assert.Equal(DiscountType.Percentage, cmd.Discount.Type);
    }

    // ══════════════════════════════════════════════════════════════
    //  VALIDATOR — success
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Validator_ValidCommand_ReturnsSuccess()
    {
        var validator = new SaveBillCommandValidator();
        var result = await validator.ValidateAsync(ValidCommand());
        Assert.True(result.IsValid);
    }

    // ══════════════════════════════════════════════════════════════
    //  VALIDATOR — failures
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Validator_EmptyIdempotencyKey_ReturnsError()
    {
        var validator = new SaveBillCommandValidator();
        var cmd = ValidCommand(key: Guid.Empty);
        var result = await validator.ValidateAsync(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(SaveBillCommand.IdempotencyKey));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validator_BlankPaymentMethod_ReturnsError(string? payment)
    {
        var validator = new SaveBillCommandValidator();
        var cmd = ValidCommand(paymentMethod: payment!);
        var result = await validator.ValidateAsync(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(SaveBillCommand.PaymentMethod));
    }

    [Fact]
    public async Task Validator_EmptyItems_ReturnsError()
    {
        var validator = new SaveBillCommandValidator();
        var cmd = ValidCommand(items: []);
        var result = await validator.ValidateAsync(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(SaveBillCommand.Items));
    }

    [Fact]
    public async Task Validator_ItemWithZeroQuantity_ReturnsError()
    {
        var validator = new SaveBillCommandValidator();
        var cmd = ValidCommand(items: [new BillItemDto(1, 0, 10m)]);
        var result = await validator.ValidateAsync(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName.Contains("Quantity"));
    }

    [Fact]
    public async Task Validator_ItemWithNegativePrice_ReturnsError()
    {
        var validator = new SaveBillCommandValidator();
        var cmd = ValidCommand(items: [new BillItemDto(1, 1, -5m)]);
        var result = await validator.ValidateAsync(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName.Contains("UnitPrice"));
    }

    [Fact]
    public async Task Validator_MultipleErrors_AggregatesAll()
    {
        var validator = new SaveBillCommandValidator();
        var cmd = new SaveBillCommand(Guid.Empty, "", [], BillDiscount.None);
        var result = await validator.ValidateAsync(cmd);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3);
    }

    // ══════════════════════════════════════════════════════════════
    //  HANDLER — online success
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Handler_OnlineSuccess_ReturnsSaleId()
    {
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Success(42));

        var result = await CreateHandler().HandleAsync(ValidCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Handler_OnlineSuccess_PersistsSaleEntity()
    {
        await CreateHandler().HandleAsync(ValidCommand());

        await _salesService.Received(1).CreateSaleAsync(
            Arg.Is<Sale>(s =>
                s.TotalAmount == 100m &&
                s.PaymentMethod == "Cash" &&
                s.Items.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_OnlineSuccess_PublishesSaleCompletedEvent()
    {
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Success(7));

        await CreateHandler().HandleAsync(ValidCommand());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SaleCompletedEvent>(e => e.SaleId == 7 && e.TotalAmount == 100m));
    }

    [Fact]
    public async Task Handler_OnlineSuccess_SetsIdempotencyKey()
    {
        var key = Guid.NewGuid();
        await CreateHandler().HandleAsync(ValidCommand(key: key));

        await _salesService.Received(1).CreateSaleAsync(
            Arg.Is<Sale>(s => s.IdempotencyKey == key),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_OnlineSuccess_PassesDiscountFields()
    {
        var discount = new BillDiscount
        {
            Type = DiscountType.Percentage, Value = 10m, Reason = "VIP"
        };
        var items = new List<BillItemDto> { new(1, 2, 100m) };
        var cmd = ValidCommand(items: items, discount: discount);

        await CreateHandler().HandleAsync(cmd);

        await _salesService.Received(1).CreateSaleAsync(
            Arg.Is<Sale>(s =>
                s.DiscountType == DiscountType.Percentage &&
                s.DiscountValue == 10m &&
                s.DiscountAmount == 20m &&
                s.DiscountReason == "VIP"),
            Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════
    //  HANDLER — online failure
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Handler_TransactionFails_ReturnsFailure()
    {
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Failure("Stock insufficient"));

        var result = await CreateHandler().HandleAsync(ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal("Stock insufficient", result.ErrorMessage);
    }

    [Fact]
    public async Task Handler_ConcurrencyConflict_ReturnsConflictMessage()
    {
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Failure(
                "conflict", isConcurrencyConflict: true));

        var result = await CreateHandler().HandleAsync(ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Contains("another user", result.ErrorMessage);
    }

    [Fact]
    public async Task Handler_OnlineFailure_DoesNotPublishEvent()
    {
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Failure("error"));

        await CreateHandler().HandleAsync(ValidCommand());

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<SaleCompletedEvent>());
    }

    // ══════════════════════════════════════════════════════════════
    //  HANDLER — offline path
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Handler_Offline_EnqueuesBillAndReturnsSuccess()
    {
        _offlineMode.IsOffline.Returns(true);

        var result = await CreateHandler().HandleAsync(ValidCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.Value);
        await _offlineQueue.Received(1).EnqueueAsync(
            Arg.Is<OfflineBill>(b =>
                b.Status == OfflineBillStatus.PendingSync &&
                b.Sale.TotalAmount == 100m &&
                b.Sale.Items.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_Offline_PublishesSaleQueuedOfflineEvent()
    {
        _offlineMode.IsOffline.Returns(true);
        var key = Guid.NewGuid();

        await CreateHandler().HandleAsync(ValidCommand(key: key));

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SaleQueuedOfflineEvent>(e =>
                e.IdempotencyKey == key && e.TotalAmount == 100m));
    }

    [Fact]
    public async Task Handler_Offline_DoesNotCallSalesService()
    {
        _offlineMode.IsOffline.Returns(true);

        await CreateHandler().HandleAsync(ValidCommand());

        await _salesService.DidNotReceive()
            .CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════
    //  FULL PIPELINE — validation short-circuits
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Pipeline_InvalidCommand_ShortCircuitsBeforeHandler()
    {
        var handlerCalled = false;
        var handler = new CallbackHandler(() => handlerCalled = true);

        var pipeline = BuildPipeline(handler, withValidation: true);
        var cmd = new SaveBillCommand(Guid.Empty, "", [], BillDiscount.None);

        var result = await pipeline.ExecuteAsync<SaveBillCommand, int>(cmd);

        Assert.False(result.Succeeded);
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task Pipeline_ValidCommand_ReachesHandler()
    {
        var handlerCalled = false;
        var handler = new CallbackHandler(() => handlerCalled = true);

        var pipeline = BuildPipeline(handler, withValidation: true);

        var result = await pipeline
            .ExecuteAsync<SaveBillCommand, int>(ValidCommand());

        Assert.True(result.Succeeded);
        Assert.True(handlerCalled);
    }

    // ══════════════════════════════════════════════════════════════
    //  Pipeline builder
    // ══════════════════════════════════════════════════════════════

    private static ICommandExecutionPipeline BuildPipeline(
        ICommandRequestHandler<SaveBillCommand, int> handler,
        bool withValidation)
    {
        var services = new ServiceCollection();
        services.AddSingleton(handler);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        if (withValidation)
        {
            services.AddTransient<ICommandValidator<SaveBillCommand>,
                SaveBillCommandValidator>();
            services.AddTransient(typeof(ICommandPipelineBehavior<,>),
                typeof(ValidationPipelineBehavior<,>));
        }

        var provider = services.BuildServiceProvider();
        return new CommandExecutionPipeline(
            provider, NullLogger<CommandExecutionPipeline>.Instance);
    }

    /// <summary>Minimal handler for pipeline integration tests.</summary>
    private sealed class CallbackHandler(Action onCalled)
        : ICommandRequestHandler<SaveBillCommand, int>
    {
        public Task<CommandResult<int>> HandleAsync(
            SaveBillCommand command, CancellationToken ct = default)
        {
            onCalled();
            return Task.FromResult(CommandResult<int>.Success(1));
        }
    }
}
