using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Sales.Events;
using StoreAssistantPro.Modules.Sales.Models;
using StoreAssistantPro.Modules.Sales.Services;

namespace StoreAssistantPro.Tests.Services;

public class OfflineSyncServiceTests : IDisposable
{
    private readonly IOfflineBillingQueue _queue = Substitute.For<IOfflineBillingQueue>();
    private readonly ISalesService _salesService = Substitute.For<ISalesService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IStatusBarService _statusBar = Substitute.For<IStatusBarService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IOfflineModeService _offlineMode = Substitute.For<IOfflineModeService>();

    private Func<OfflineModeChangedEvent, Task>? _modeHandler;

    public OfflineSyncServiceTests()
    {
        _regional.Now.Returns(new DateTime(2026, 2, 22, 14, 0, 0));
        _offlineMode.IsOffline.Returns(false);
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Success(42));
    }

    private OfflineSyncService CreateSut()
    {
        _eventBus.When(e => e.Subscribe(Arg.Any<Func<OfflineModeChangedEvent, Task>>()))
            .Do(ci => _modeHandler = ci.Arg<Func<OfflineModeChangedEvent, Task>>());

        return new OfflineSyncService(
            _queue, _salesService, _eventBus, _statusBar,
            _regional, _offlineMode,
            NullLogger<OfflineSyncService>.Instance);
    }

    private static OfflineBill CreatePendingBill(
        Guid? key = null,
        DateTime? createdTime = null) => new()
    {
        IdempotencyKey = key ?? Guid.NewGuid(),
        Status = OfflineBillStatus.PendingSync,
        CreatedTime = createdTime ?? new DateTime(2026, 2, 22, 13, 0, 0),
        Sale = new CompleteSaleSnapshot
        {
            TotalAmount = 100m,
            PaymentMethod = "Cash",
            SaleDate = new DateTime(2026, 2, 22, 13, 0, 0),
            DiscountType = DiscountType.None,
            DiscountValue = 0m,
            DiscountAmount = 0m,
            Items = [new SaleItemSnapshot { ProductId = 1, Quantity = 2, UnitPrice = 50m }]
        }
    };

    // ══════════════════════════════════════════════════════════════
    //  Subscription
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_SubscribesToOfflineModeChangedEvent()
    {
        _ = CreateSut();

        _eventBus.Received(1).Subscribe(Arg.Any<Func<OfflineModeChangedEvent, Task>>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Event trigger — only on restoration
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ModeChanged_GoingOffline_DoesNotSync()
    {
        _ = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([CreatePendingBill()]);

        await _modeHandler!(new OfflineModeChangedEvent(IsOffline: true, TimeSpan.Zero));

        await _salesService.DidNotReceive()
            .CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ModeChanged_ConnectionRestored_TriggersSyncPending()
    {
        var bill = CreatePendingBill();
        _ = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill]);

        await _modeHandler!(
            new OfflineModeChangedEvent(IsOffline: false, TimeSpan.FromMinutes(5)));

        await _salesService.Received(1)
            .CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════
    //  SyncPendingAsync — empty queue
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncPending_EmptyQueue_ReturnsZero()
    {
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(new List<OfflineBill>().AsReadOnly());

        var synced = await sut.SyncPendingAsync();

        Assert.Equal(0, synced);
        await _salesService.DidNotReceive()
            .CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════
    //  SyncPendingAsync — success path
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncPending_Success_RemovesBillFromQueue()
    {
        var bill = CreatePendingBill();
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill]);

        var synced = await sut.SyncPendingAsync();

        Assert.Equal(1, synced);
        await _queue.Received(1).RemoveAsync(bill.IdempotencyKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncPending_Success_PublishesSaleCompletedEvent()
    {
        var bill = CreatePendingBill();
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill]);
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Success(99));

        await sut.SyncPendingAsync();

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SaleCompletedEvent>(e => e.SaleId == 99 && e.TotalAmount == 100m));
    }

    [Fact]
    public async Task SyncPending_Success_MarksBillSyncingBeforePush()
    {
        var bill = CreatePendingBill();
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill]);

        // Capture the bill status when UpdateAsync is called
        OfflineBillStatus? statusOnUpdate = null;
        _queue.When(q => q.UpdateAsync(Arg.Any<OfflineBill>(), Arg.Any<CancellationToken>()))
            .Do(ci =>
            {
                // Capture first update only (Syncing)
                statusOnUpdate ??= ci.Arg<OfflineBill>().Status;
            });

        await sut.SyncPendingAsync();

        Assert.Equal(OfflineBillStatus.Syncing, statusOnUpdate);
    }

    [Fact]
    public async Task SyncPending_Success_BuildsSaleFromSnapshot()
    {
        var key = Guid.NewGuid();
        var bill = CreatePendingBill(key: key);
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill]);

        await sut.SyncPendingAsync();

        await _salesService.Received(1).CreateSaleAsync(
            Arg.Is<Sale>(s =>
                s.IdempotencyKey == key &&
                s.TotalAmount == 100m &&
                s.PaymentMethod == "Cash" &&
                s.Items.Count == 1 &&
                s.Items.First().ProductId == 1 &&
                s.Items.First().Quantity == 2 &&
                s.Items.First().UnitPrice == 50m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncPending_Success_IncrementsAttemptCount()
    {
        var bill = CreatePendingBill();
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill]);

        await sut.SyncPendingAsync();

        Assert.Equal(1, bill.SyncAttemptCount);
        Assert.NotNull(bill.LastSyncAttempt);
    }

    // ══════════════════════════════════════════════════════════════
    //  SyncPendingAsync — failure path
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncPending_Failure_MarksBillAsFailed()
    {
        var bill = CreatePendingBill();
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill]);
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Failure("Insufficient stock"));

        var synced = await sut.SyncPendingAsync();

        Assert.Equal(0, synced);
        Assert.Equal(OfflineBillStatus.Failed, bill.Status);
        Assert.Equal("Insufficient stock", bill.LastError);
        // UpdateAsync called twice: once for Syncing, once for Failed
        await _queue.Received(2).UpdateAsync(bill, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncPending_Failure_DoesNotRemoveBill()
    {
        var bill = CreatePendingBill();
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill]);
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Failure("Error"));

        await sut.SyncPendingAsync();

        await _queue.DidNotReceive().RemoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncPending_Failure_DoesNotPublishSaleCompletedEvent()
    {
        var bill = CreatePendingBill();
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill]);
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResult<int>.Failure("Error"));

        await sut.SyncPendingAsync();

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<SaleCompletedEvent>());
    }

    // ══════════════════════════════════════════════════════════════
    //  SyncPendingAsync — multiple bills
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncPending_MultipleBills_SyncsEach()
    {
        var bill1 = CreatePendingBill();
        var bill2 = CreatePendingBill();
        var bill3 = CreatePendingBill();
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill1, bill2, bill3]);

        var synced = await sut.SyncPendingAsync();

        Assert.Equal(3, synced);
        await _salesService.Received(3)
            .CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        await _queue.Received(3).RemoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncPending_PartialFailure_ContinuesWithRemaining()
    {
        var bill1 = CreatePendingBill();
        var bill2 = CreatePendingBill();
        var bill3 = CreatePendingBill();
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill1, bill2, bill3]);

        // First and third succeed, second fails
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(
                TransactionResult<int>.Success(1),
                TransactionResult<int>.Failure("Stock issue"),
                TransactionResult<int>.Success(3));

        var synced = await sut.SyncPendingAsync();

        Assert.Equal(2, synced);
        Assert.Equal(OfflineBillStatus.Failed, bill2.Status);
        await _queue.Received(2).RemoveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Offline during sync — abort early
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncPending_GoesOfflineMidSync_AbortsEarly()
    {
        var bill1 = CreatePendingBill();
        var bill2 = CreatePendingBill();
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill1, bill2]);

        // After first bill syncs, simulate going offline
        _salesService.CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                // After first call, mark as offline
                _offlineMode.IsOffline.Returns(true);
                return TransactionResult<int>.Success(1);
            });

        var synced = await sut.SyncPendingAsync();

        Assert.Equal(1, synced);
        // Second bill was never attempted
        await _salesService.Received(1)
            .CreateSaleAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Concurrent sync prevention
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncPending_ConcurrentCalls_OnlyOneRuns()
    {
        var bill = CreatePendingBill();
        var sut = CreateSut();

        var tcs = new TaskCompletionSource();

        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                // Block the first call until we release
                await tcs.Task;
                return (IReadOnlyList<OfflineBill>)[bill];
            });

        // Start first sync (will block)
        var sync1 = sut.SyncPendingAsync();

        // Second sync should return 0 immediately (gate not available)
        var sync2Result = await sut.SyncPendingAsync();

        // Release first sync
        tcs.SetResult();
        var sync1Result = await sync1;

        Assert.Equal(0, sync2Result); // Skipped
        Assert.Equal(1, sync1Result); // Ran
    }

    // ══════════════════════════════════════════════════════════════
    //  IsSyncing
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void IsSyncing_InitiallyFalse()
    {
        var sut = CreateSut();

        Assert.False(sut.IsSyncing);
    }

    // ══════════════════════════════════════════════════════════════
    //  Status bar messages
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncPending_PostsSyncingStatusBar()
    {
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([CreatePendingBill()]);

        await sut.SyncPendingAsync();

        _statusBar.Received(1).Post(
            Arg.Is<string>(s => s.Contains("Syncing")),
            Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task SyncPending_Success_PostsCompletionStatusBar()
    {
        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([CreatePendingBill()]);

        await sut.SyncPendingAsync();

        _statusBar.Received(1).Post(
            Arg.Is<string>(s => s.Contains("Synced")));
    }

    // ══════════════════════════════════════════════════════════════
    //  Failed bills — retried
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncPending_RetriesFailedBills()
    {
        var bill = CreatePendingBill();
        bill.Status = OfflineBillStatus.Failed;
        bill.SyncAttemptCount = 1;
        bill.LastError = "Previous error";

        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([bill]);

        var synced = await sut.SyncPendingAsync();

        Assert.Equal(1, synced);
        Assert.Equal(2, bill.SyncAttemptCount);
        await _queue.Received(1).RemoveAsync(bill.IdempotencyKey, Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Discount fields preserved through sync
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncPending_PreservesDiscountFieldsInSale()
    {
        var bill = new OfflineBill
        {
            IdempotencyKey = Guid.NewGuid(),
            Status = OfflineBillStatus.PendingSync,
            CreatedTime = new DateTime(2026, 2, 22, 13, 0, 0),
            Sale = new CompleteSaleSnapshot
            {
                TotalAmount = 90m,
                PaymentMethod = "Card",
                SaleDate = new DateTime(2026, 2, 22, 13, 0, 0),
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10m,
                DiscountAmount = 10m,
                DiscountReason = "VIP",
                Items = [new SaleItemSnapshot { ProductId = 1, Quantity = 1, UnitPrice = 100m }]
            }
        };

        var sut = CreateSut();
        _queue.GetPendingAsync(Arg.Any<CancellationToken>()).Returns([bill]);

        await sut.SyncPendingAsync();

        await _salesService.Received(1).CreateSaleAsync(
            Arg.Is<Sale>(s =>
                s.DiscountType == DiscountType.Percentage &&
                s.DiscountValue == 10m &&
                s.DiscountAmount == 10m &&
                s.DiscountReason == "VIP"),
            Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════
    //  Dispose
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_UnsubscribesEvent()
    {
        var sut = CreateSut();

        sut.Dispose();

        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<OfflineModeChangedEvent, Task>>());
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose() { }
}
