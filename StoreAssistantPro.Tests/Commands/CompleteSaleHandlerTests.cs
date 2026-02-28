using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Sales.Commands;
using StoreAssistantPro.Modules.Sales.Events;
using StoreAssistantPro.Modules.Sales.Models;
using StoreAssistantPro.Modules.Sales.Services;

namespace StoreAssistantPro.Tests.Commands;

public class CompleteSaleHandlerTests
{
    private readonly ISalesService _salesService = Substitute.For<ISalesService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IBillCalculationService _billCalculation = new BillCalculationService();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IOfflineModeService _offlineMode = Substitute.For<IOfflineModeService>();
    private readonly IOfflineBillingQueue _offlineQueue = Substitute.For<IOfflineBillingQueue>();
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();

    public CompleteSaleHandlerTests()
    {
        _regional.Now.Returns(new DateTime(2026, 2, 19, 14, 30, 0));
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Success(1));
        _offlineMode.IsOffline.Returns(false);
        _sessionService.CurrentUserType.Returns(UserType.Admin);
    }

    private CompleteSaleHandler CreateSut() =>
        new(_salesService, _eventBus, _billCalculation, _regional, _offlineMode, _offlineQueue, _sessionService);

    private static Guid NewKey() => Guid.NewGuid();

    [Fact]
    public async Task HandleAsync_Success_CreatesSaleAndPublishesEvent()
    {
        var items = new List<SaleItemDto> { new(1, 3, 10m) };
        var command = new CompleteSaleCommand(NewKey(), 30m, "Cash", items, BillDiscount.None);

        var result = await CreateSut().HandleAsync(command);

        Assert.True(result.Succeeded);
        await _salesService.Received(1).CreateSaleAsync(Arg.Is<Sale>(s =>
            s.TotalAmount == 30m &&
            s.PaymentMethod == "Cash" &&
            s.Items.Count == 1));
        await _eventBus.Received(1).PublishAsync(Arg.Is<SaleCompletedEvent>(e =>
            e.TotalAmount == 30m));
    }

    [Fact]
    public async Task HandleAsync_TransactionFails_ReturnsFailure()
    {
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Failure("Stock insufficient"));

        var command = new CompleteSaleCommand(NewKey(), 10m, "Card", [new(1, 1, 10m)], BillDiscount.None);

        var result = await CreateSut().HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal("Stock insufficient", result.ErrorMessage);
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<SaleCompletedEvent>());
    }

    [Fact]
    public async Task HandleAsync_ConcurrencyConflict_ReturnsConflictMessage()
    {
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Failure(
                "conflict", isConcurrencyConflict: true));

        var command = new CompleteSaleCommand(NewKey(), 10m, "Cash", [new(1, 1, 10m)], BillDiscount.None);

        var result = await CreateSut().HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Contains("another user", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_Success_PassesDiscountFieldsToService()
    {
        var discount = new BillDiscount
        {
            Type = DiscountType.Percentage,
            Value = 10m,
            Reason = "VIP"
        };
        var items = new List<SaleItemDto> { new(1, 2, 100m) };
        var command = new CompleteSaleCommand(NewKey(), 180m, "Card", items, discount);

        await CreateSut().HandleAsync(command);

        await _salesService.Received(1).CreateSaleAsync(Arg.Is<Sale>(s =>
            s.DiscountType == DiscountType.Percentage &&
            s.DiscountValue == 10m &&
            s.DiscountAmount == 20m &&
            s.DiscountReason == "VIP"));
    }

    [Fact]
    public async Task HandleAsync_Success_PublishesSaleIdFromResult()
    {
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Success(42));

        var items = new List<SaleItemDto> { new(1, 1, 10m) };
        var command = new CompleteSaleCommand(NewKey(), 10m, "Cash", items, BillDiscount.None);

        await CreateSut().HandleAsync(command);

        await _eventBus.Received(1).PublishAsync(Arg.Is<SaleCompletedEvent>(e =>
            e.SaleId == 42));
    }

    [Fact]
    public async Task HandleAsync_PassesIdempotencyKeyToSale()
    {
        var key = Guid.NewGuid();
        var items = new List<SaleItemDto> { new(1, 1, 10m) };
        var command = new CompleteSaleCommand(key, 10m, "Cash", items, BillDiscount.None);

        await CreateSut().HandleAsync(command);

        await _salesService.Received(1).CreateSaleAsync(Arg.Is<Sale>(s =>
            s.IdempotencyKey == key));
    }

    // ── Offline path ───────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Offline_EnqueuesBillAndReturnsSuccess()
    {
        _offlineMode.IsOffline.Returns(true);
        var items = new List<SaleItemDto> { new(1, 3, 10m) };
        var command = new CompleteSaleCommand(NewKey(), 30m, "Cash", items, BillDiscount.None);

        var result = await CreateSut().HandleAsync(command);

        Assert.True(result.Succeeded);
        await _offlineQueue.Received(1).EnqueueAsync(
            Arg.Is<OfflineBill>(b =>
                b.IdempotencyKey == command.IdempotencyKey &&
                b.Status == OfflineBillStatus.PendingSync &&
                b.Sale.TotalAmount == 30m &&
                b.Sale.PaymentMethod == "Cash" &&
                b.Sale.Items.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Offline_PublishesSaleQueuedOfflineEvent()
    {
        _offlineMode.IsOffline.Returns(true);
        var key = Guid.NewGuid();
        var items = new List<SaleItemDto> { new(1, 2, 50m) };
        var command = new CompleteSaleCommand(key, 100m, "Card", items, BillDiscount.None);

        await CreateSut().HandleAsync(command);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SaleQueuedOfflineEvent>(e =>
                e.IdempotencyKey == key &&
                e.TotalAmount == 100m));
    }

    [Fact]
    public async Task HandleAsync_Offline_DoesNotCallSalesService()
    {
        _offlineMode.IsOffline.Returns(true);
        var command = new CompleteSaleCommand(NewKey(), 10m, "Cash", [new(1, 1, 10m)], BillDiscount.None);

        await CreateSut().HandleAsync(command);

        await _salesService.DidNotReceive().CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Offline_DoesNotPublishSaleCompletedEvent()
    {
        _offlineMode.IsOffline.Returns(true);
        var command = new CompleteSaleCommand(NewKey(), 10m, "Cash", [new(1, 1, 10m)], BillDiscount.None);

        await CreateSut().HandleAsync(command);

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<SaleCompletedEvent>());
    }

    [Fact]
    public async Task HandleAsync_Offline_PreservesDiscountInSnapshot()
    {
        _offlineMode.IsOffline.Returns(true);
        var discount = new BillDiscount
        {
            Type = DiscountType.Percentage,
            Value = 10m,
            Reason = "VIP"
        };
        var items = new List<SaleItemDto> { new(1, 2, 100m) };
        var command = new CompleteSaleCommand(NewKey(), 180m, "Card", items, discount);

        await CreateSut().HandleAsync(command);

        await _offlineQueue.Received(1).EnqueueAsync(
            Arg.Is<OfflineBill>(b =>
                b.Sale.DiscountType == DiscountType.Percentage &&
                b.Sale.DiscountValue == 10m &&
                b.Sale.DiscountAmount == 20m &&
                b.Sale.DiscountReason == "VIP"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Offline_UsesRegionalNow()
    {
        _offlineMode.IsOffline.Returns(true);
        var expectedTime = new DateTime(2026, 2, 19, 14, 30, 0);
        _regional.Now.Returns(expectedTime);
        var command = new CompleteSaleCommand(NewKey(), 10m, "Cash", [new(1, 1, 10m)], BillDiscount.None);

        await CreateSut().HandleAsync(command);

        await _offlineQueue.Received(1).EnqueueAsync(
            Arg.Is<OfflineBill>(b =>
                b.CreatedTime == expectedTime &&
                b.Sale.SaleDate == expectedTime),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Online_DoesNotTouchOfflineQueue()
    {
        _offlineMode.IsOffline.Returns(false);
        var command = new CompleteSaleCommand(NewKey(), 10m, "Cash", [new(1, 1, 10m)], BillDiscount.None);

        await CreateSut().HandleAsync(command);

        await _offlineQueue.DidNotReceive().EnqueueAsync(
            Arg.Any<OfflineBill>(), Arg.Any<CancellationToken>());
    }
}
