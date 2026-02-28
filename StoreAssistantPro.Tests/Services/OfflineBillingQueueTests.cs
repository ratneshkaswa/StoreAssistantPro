using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Sales.Models;
using StoreAssistantPro.Modules.Sales.Services;

namespace StoreAssistantPro.Tests.Services;

public class OfflineBillingQueueTests : IDisposable
{
    private readonly string _testDir;

    public OfflineBillingQueueTests()
    {
        _testDir = Path.Combine(
            Path.GetTempPath(),
            "StoreAssistantPro_Tests",
            Guid.NewGuid().ToString());
    }

    private OfflineBillingQueue CreateSut() =>
        new(_testDir, NullLogger<OfflineBillingQueue>.Instance);

    private static OfflineBill CreateBill(
        Guid? key = null,
        OfflineBillStatus status = OfflineBillStatus.PendingSync,
        DateTime? createdTime = null) => new()
    {
        IdempotencyKey = key ?? Guid.NewGuid(),
        Status = status,
        CreatedTime = createdTime ?? new DateTime(2026, 2, 22, 14, 0, 0),
        Sale = new CompleteSaleSnapshot
        {
            TotalAmount = 150.00m,
            PaymentMethod = "Cash",
            SaleDate = new DateTime(2026, 2, 22, 14, 0, 0),
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10m,
            DiscountAmount = 15.00m,
            DiscountReason = "Staff discount",
            Items =
            [
                new SaleItemSnapshot { ProductId = 1, Quantity = 2, UnitPrice = 50.00m },
                new SaleItemSnapshot { ProductId = 3, Quantity = 1, UnitPrice = 65.00m }
            ]
        }
    };

    // ══════════════════════════════════════════════════════════════
    //  Enqueue
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Enqueue_CreatesFileOnDisk()
    {
        var sut = CreateSut();
        var bill = CreateBill();

        await sut.EnqueueAsync(bill);

        var files = Directory.GetFiles(_testDir, "*.json");
        Assert.Single(files);
        Assert.Contains(bill.IdempotencyKey.ToString(), files[0]);
    }

    [Fact]
    public async Task Enqueue_SameKeyTwice_OverwritesFile()
    {
        var sut = CreateSut();
        var key = Guid.NewGuid();
        var bill1 = CreateBill(key: key);
        var bill2 = CreateBill(key: key, status: OfflineBillStatus.Syncing);

        await sut.EnqueueAsync(bill1);
        await sut.EnqueueAsync(bill2);

        var files = Directory.GetFiles(_testDir, "*.json");
        Assert.Single(files);

        var all = await sut.GetAllAsync();
        Assert.Single(all);
        Assert.Equal(OfflineBillStatus.Syncing, all[0].Status);
    }

    [Fact]
    public async Task Enqueue_PreservesAllBillData()
    {
        var sut = CreateSut();
        var bill = CreateBill();

        await sut.EnqueueAsync(bill);

        var loaded = (await sut.GetAllAsync())[0];
        Assert.Equal(bill.IdempotencyKey, loaded.IdempotencyKey);
        Assert.Equal(OfflineBillStatus.PendingSync, loaded.Status);
        Assert.Equal(bill.CreatedTime, loaded.CreatedTime);
        Assert.Equal(150.00m, loaded.Sale.TotalAmount);
        Assert.Equal("Cash", loaded.Sale.PaymentMethod);
        Assert.Equal(DiscountType.Percentage, loaded.Sale.DiscountType);
        Assert.Equal(10m, loaded.Sale.DiscountValue);
        Assert.Equal(15.00m, loaded.Sale.DiscountAmount);
        Assert.Equal("Staff discount", loaded.Sale.DiscountReason);
        Assert.Equal(2, loaded.Sale.Items.Count);
        Assert.Equal(1, loaded.Sale.Items[0].ProductId);
        Assert.Equal(2, loaded.Sale.Items[0].Quantity);
        Assert.Equal(50.00m, loaded.Sale.Items[0].UnitPrice);
    }

    // ══════════════════════════════════════════════════════════════
    //  GetAll
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAll_EmptyQueue_ReturnsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_MultipleBills_OrderedByCreatedTime()
    {
        var sut = CreateSut();
        var bill1 = CreateBill(createdTime: new DateTime(2026, 2, 22, 16, 0, 0));
        var bill2 = CreateBill(createdTime: new DateTime(2026, 2, 22, 14, 0, 0));
        var bill3 = CreateBill(createdTime: new DateTime(2026, 2, 22, 15, 0, 0));

        await sut.EnqueueAsync(bill1);
        await sut.EnqueueAsync(bill2);
        await sut.EnqueueAsync(bill3);

        var result = await sut.GetAllAsync();
        Assert.Equal(3, result.Count);
        Assert.Equal(bill2.IdempotencyKey, result[0].IdempotencyKey);
        Assert.Equal(bill3.IdempotencyKey, result[1].IdempotencyKey);
        Assert.Equal(bill1.IdempotencyKey, result[2].IdempotencyKey);
    }

    [Fact]
    public async Task GetAll_SkipsCorruptFiles()
    {
        var sut = CreateSut();
        var bill = CreateBill();
        await sut.EnqueueAsync(bill);

        // Create a corrupt file
        await File.WriteAllTextAsync(
            Path.Combine(_testDir, "corrupt.json"), "NOT VALID JSON");

        var result = await sut.GetAllAsync();
        Assert.Single(result);
        Assert.Equal(bill.IdempotencyKey, result[0].IdempotencyKey);
    }

    // ══════════════════════════════════════════════════════════════
    //  GetPending
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPending_ReturnsOnlyPendingAndFailed()
    {
        var sut = CreateSut();
        var pending = CreateBill(status: OfflineBillStatus.PendingSync,
            createdTime: new DateTime(2026, 2, 22, 14, 0, 0));
        var syncing = CreateBill(status: OfflineBillStatus.Syncing,
            createdTime: new DateTime(2026, 2, 22, 14, 1, 0));
        var synced = CreateBill(status: OfflineBillStatus.Synced,
            createdTime: new DateTime(2026, 2, 22, 14, 2, 0));
        var failed = CreateBill(status: OfflineBillStatus.Failed,
            createdTime: new DateTime(2026, 2, 22, 14, 3, 0));

        await sut.EnqueueAsync(pending);
        await sut.EnqueueAsync(syncing);
        await sut.EnqueueAsync(synced);
        await sut.EnqueueAsync(failed);

        var result = await sut.GetPendingAsync();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, b => b.IdempotencyKey == pending.IdempotencyKey);
        Assert.Contains(result, b => b.IdempotencyKey == failed.IdempotencyKey);
    }

    // ══════════════════════════════════════════════════════════════
    //  Update
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Update_ChangesStatusOnDisk()
    {
        var sut = CreateSut();
        var bill = CreateBill();
        await sut.EnqueueAsync(bill);

        bill.Status = OfflineBillStatus.Syncing;
        bill.SyncAttemptCount = 1;
        bill.LastSyncAttempt = new DateTime(2026, 2, 22, 15, 0, 0);
        await sut.UpdateAsync(bill);

        var loaded = (await sut.GetAllAsync())[0];
        Assert.Equal(OfflineBillStatus.Syncing, loaded.Status);
        Assert.Equal(1, loaded.SyncAttemptCount);
        Assert.Equal(new DateTime(2026, 2, 22, 15, 0, 0), loaded.LastSyncAttempt);
    }

    [Fact]
    public async Task Update_RecordsErrorMessage()
    {
        var sut = CreateSut();
        var bill = CreateBill();
        await sut.EnqueueAsync(bill);

        bill.Status = OfflineBillStatus.Failed;
        bill.LastError = "Connection timeout";
        await sut.UpdateAsync(bill);

        var loaded = (await sut.GetAllAsync())[0];
        Assert.Equal(OfflineBillStatus.Failed, loaded.Status);
        Assert.Equal("Connection timeout", loaded.LastError);
    }

    // ══════════════════════════════════════════════════════════════
    //  Remove
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Remove_DeletesFileFromDisk()
    {
        var sut = CreateSut();
        var bill = CreateBill();
        await sut.EnqueueAsync(bill);

        await sut.RemoveAsync(bill.IdempotencyKey);

        var files = Directory.GetFiles(_testDir, "*.json");
        Assert.Empty(files);

        var all = await sut.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task Remove_NonExistentKey_DoesNotThrow()
    {
        var sut = CreateSut();

        await sut.RemoveAsync(Guid.NewGuid());
    }

    // ══════════════════════════════════════════════════════════════
    //  Count / PendingCount
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Count_ReturnsTotal()
    {
        var sut = CreateSut();
        await sut.EnqueueAsync(CreateBill());
        await sut.EnqueueAsync(CreateBill());
        await sut.EnqueueAsync(CreateBill());

        Assert.Equal(3, await sut.CountAsync());
    }

    [Fact]
    public async Task PendingCount_ReturnsOnlyPendingAndFailed()
    {
        var sut = CreateSut();
        await sut.EnqueueAsync(CreateBill(status: OfflineBillStatus.PendingSync));
        await sut.EnqueueAsync(CreateBill(status: OfflineBillStatus.Synced));
        await sut.EnqueueAsync(CreateBill(status: OfflineBillStatus.Failed));

        Assert.Equal(2, await sut.PendingCountAsync());
    }

    // ══════════════════════════════════════════════════════════════
    //  Directory creation
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Constructor_CreatesDirectoryIfMissing()
    {
        var customDir = Path.Combine(_testDir, "sub", "dir");
        var sut = new OfflineBillingQueue(customDir,
            NullLogger<OfflineBillingQueue>.Instance);

        Assert.True(Directory.Exists(customDir));
        await sut.EnqueueAsync(CreateBill());
        Assert.Equal(1, await sut.CountAsync());
    }

    // ══════════════════════════════════════════════════════════════
    //  Full workflow
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task FullWorkflow_EnqueueUpdateRemove()
    {
        var sut = CreateSut();
        var bill = CreateBill();

        // Enqueue
        await sut.EnqueueAsync(bill);
        Assert.Equal(1, await sut.PendingCountAsync());

        // Mark syncing
        bill.Status = OfflineBillStatus.Syncing;
        bill.SyncAttemptCount = 1;
        await sut.UpdateAsync(bill);
        Assert.Equal(0, await sut.PendingCountAsync());
        Assert.Equal(1, await sut.CountAsync());

        // Fail
        bill.Status = OfflineBillStatus.Failed;
        bill.LastError = "timeout";
        await sut.UpdateAsync(bill);
        Assert.Equal(1, await sut.PendingCountAsync());

        // Retry → synced
        bill.Status = OfflineBillStatus.Synced;
        bill.SyncAttemptCount = 2;
        bill.LastError = null;
        await sut.UpdateAsync(bill);
        Assert.Equal(0, await sut.PendingCountAsync());

        // Remove
        await sut.RemoveAsync(bill.IdempotencyKey);
        Assert.Equal(0, await sut.CountAsync());
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
