using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.PurchaseOrders.Services;

namespace StoreAssistantPro.Tests.Services;

public class PurchaseOrderServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    private PurchaseOrderService CreateSut()
    {
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(_dbOptions)));

        _regional.Now.Returns(new DateTime(2026, 3, 13, 10, 30, 0));

        return new PurchaseOrderService(factory, _perf, _regional);
    }

    [Fact]
    public async Task CreateAsync_MissingProduct_ThrowsMeaningfulError()
    {
        await using (var db = new AppDbContext(_dbOptions))
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
        await using (var db = new AppDbContext(_dbOptions))
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

        await using var verifyDb = new AppDbContext(_dbOptions);
        var stored = await verifyDb.PurchaseOrders.Include(x => x.Items).SingleAsync();
        Assert.Equal(7, stored.SupplierId);
        Assert.Equal(new DateTime(2026, 3, 20), stored.ExpectedDate);
        Assert.Equal("Urgent", stored.Notes);
        Assert.Single(stored.Items);
        Assert.Equal(11, stored.Items.First().ProductId);
        Assert.Equal(3, stored.Items.First().Quantity);
        Assert.Equal(120m, stored.Items.First().UnitCost);
    }

    public void Dispose()
    {
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
