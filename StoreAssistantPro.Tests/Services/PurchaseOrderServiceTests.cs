using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.PurchaseOrders.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.Services;

public class PurchaseOrderServiceTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbFactory = new();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IReferenceDataCache _referenceDataCache = Substitute.For<IReferenceDataCache>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private PurchaseOrderService CreateSut()
    {
        _regional.Now.Returns(new DateTime(2026, 3, 13, 10, 30, 0));

        return new PurchaseOrderService(_dbFactory, _perf, _regional, _referenceDataCache, _eventBus);
    }

    [Fact]
    public async Task CreateAsync_MissingProduct_ThrowsMeaningfulError()
    {
        await using (var db = _dbFactory.CreateContext())
        {
            db.Suppliers.Add(new Supplier { Id = 7, Name = "Jaipur Textiles", IsActive = true });
            await db.SaveChangesAsync();
        }

        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(
            new CreatePurchaseOrderDto(
                SupplierId: 7,
                ExpectedDate: null,
                Notes: null,
                Items: [new PurchaseOrderLineDto(99, 2, 120m)])));

        Assert.Equal("Product Id 99 no longer exists.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ValidOrder_PersistsDraftPurchaseOrder()
    {
        await using (var db = _dbFactory.CreateContext())
        {
            db.Suppliers.Add(new Supplier { Id = 7, Name = "Jaipur Textiles", IsActive = true });
            db.Products.Add(new Product { Id = 11, Name = "Cotton Shirt", SalePrice = 999m, CostPrice = 500m, Quantity = 10 });
            await db.SaveChangesAsync();
        }

        var sut = CreateSut();

        var po = await sut.CreateAsync(new CreatePurchaseOrderDto(
            SupplierId: 7,
            ExpectedDate: new DateTime(2026, 3, 20),
            Notes: "Urgent",
            Items: [new PurchaseOrderLineDto(11, 3, 120m)]));

        Assert.Equal(PurchaseOrderStatus.Draft, po.Status);
        Assert.Single(po.Items);

        await using var verifyDb = _dbFactory.CreateContext();
        var stored = await verifyDb.PurchaseOrders.Include(x => x.Items).SingleAsync();
        Assert.Equal(7, stored.SupplierId);
        Assert.Equal(new DateTime(2026, 3, 20), stored.ExpectedDate);
        Assert.Equal("Urgent", stored.Notes);
        Assert.Single(stored.Items);
        Assert.Equal(11, stored.Items.First().ProductId);
        Assert.Equal(3, stored.Items.First().Quantity);
        Assert.Equal(120m, stored.Items.First().UnitCost);
        await _eventBus.Received(1).PublishAsync(Arg.Any<SalesDataChangedEvent>());
    }

    [Fact]
    public async Task ReceiveItemsAsync_Should_UpdateStock_AndPublishDataChangedEvent()
    {
        await using (var db = _dbFactory.CreateContext())
        {
            db.Suppliers.Add(new Supplier { Id = 7, Name = "Jaipur Textiles", IsActive = true });
            db.Products.Add(new Product { Id = 11, Name = "Cotton Shirt", SalePrice = 999m, CostPrice = 500m, Quantity = 10 });
            db.PurchaseOrders.Add(new PurchaseOrder
            {
                Id = 21,
                SupplierId = 7,
                OrderNumber = "PO-20260313-0001",
                OrderDate = new DateTime(2026, 3, 13, 10, 30, 0),
                Status = PurchaseOrderStatus.Ordered,
                Items =
                [
                    new PurchaseOrderItem
                    {
                        Id = 31,
                        ProductId = 11,
                        Quantity = 5,
                        UnitCost = 120m,
                        QuantityReceived = 0
                    }
                ]
            });
            await db.SaveChangesAsync();
        }

        var sut = CreateSut();

        await sut.ReceiveItemsAsync(21, [new ReceiveLineDto(31, 5)]);

        await using var verifyDb = _dbFactory.CreateContext();
        var product = await verifyDb.Products.SingleAsync(p => p.Id == 11);
        var po = await verifyDb.PurchaseOrders.Include(x => x.Items).SingleAsync(x => x.Id == 21);

        Assert.Equal(15, product.Quantity);
        Assert.Equal(PurchaseOrderStatus.Received, po.Status);
        Assert.Equal(5, po.Items.Single().QuantityReceived);
        await _eventBus.Received(1).PublishAsync(Arg.Any<SalesDataChangedEvent>());
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }
}
