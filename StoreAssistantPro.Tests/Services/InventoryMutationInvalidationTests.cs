using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.Inventory.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.Services;

public sealed class InventoryMutationInvalidationTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbFactory = new();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    public InventoryMutationInvalidationTests()
        => _regional.Now.Returns(new DateTime(2026, 3, 27, 12, 0, 0));

    [Fact]
    public async Task AdjustStockAsync_ShouldPublishDataChangedEvent()
    {
        await SeedProductAsync();
        var sut = new InventoryService(_dbFactory, _regional, _perf, _eventBus);

        await sut.AdjustStockAsync(new StockAdjustmentDto(1, null, 25, AdjustmentReason.Correction, "Cycle count", 1));

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SalesDataChangedEvent>(e => e.Reason == "InventoryStockAdjusted"));
    }

    [Fact]
    public async Task ImportStockAsync_ShouldPublishDataChangedEvent_WhenRowsChangeStock()
    {
        await SeedProductAsync();
        var sut = new InventoryService(_dbFactory, _regional, _perf, _eventBus);

        var updated = await sut.ImportStockAsync(
            [new Dictionary<string, string> { ["Name"] = "Blue Shirt", ["Quantity"] = "9" }],
            userId: 1);

        Assert.Equal(1, updated);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SalesDataChangedEvent>(e => e.Reason == "InventoryCsvImported"));
    }

    [Fact]
    public async Task CompleteStockTakeAsync_ShouldPublishDataChangedEvent_WhenStockChanges()
    {
        await SeedProductAsync();
        var sut = new StockTakeService(_dbFactory, _regional, _eventBus);

        var started = await sut.StartAsync("Cycle count", 1);
        var reloaded = await sut.GetByIdAsync(started.Id);
        var item = Assert.Single(reloaded!.Items);
        await sut.UpdateCountAsync(item.Id, 13);

        var result = await sut.CompleteAsync(started.Id, 1);

        Assert.Equal(1, result.Adjusted);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SalesDataChangedEvent>(e => e.Reason == "StockTakeCompleted"));
    }

    public void Dispose() => _dbFactory.Dispose();

    private async Task SeedProductAsync()
    {
        await using var seed = _dbFactory.CreateContext();
        seed.UserCredentials.Add(new UserCredential
        {
            Id = 1,
            UserType = UserType.Admin,
            PinHash = "test"
        });
        seed.Products.Add(new Product
        {
            Id = 1,
            Name = "Blue Shirt",
            SalePrice = 250m,
            CostPrice = 125m,
            Quantity = 10,
            IsActive = true,
            MinStockLevel = 2
        });
        await seed.SaveChangesAsync();
    }
}
