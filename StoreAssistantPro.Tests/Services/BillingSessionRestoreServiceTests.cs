using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Models;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Tests.Services;

public class BillingSessionRestoreServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly IPricingCalculationService _pricing = new PricingCalculationService();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    // Seed IDs
    private int _productAId;
    private int _productBId;
    private int _productNoStockId;

    private const decimal ProductAPrice = 100m;
    private const decimal ProductBPrice = 250m;
    private const decimal TaxRate18 = 18m;

    public BillingSessionRestoreServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        using var db = new AppDbContext(_dbOptions);

        // Tax setup: 18% GST = CGST 9% + SGST 9%
        var cgst = new TaxMaster { TaxName = "CGST 9%", TaxRate = 9m, IsActive = true };
        var sgst = new TaxMaster { TaxName = "SGST 9%", TaxRate = 9m, IsActive = true };
        db.TaxMasters.AddRange(cgst, sgst);
        db.SaveChanges();

        var profile18 = new TaxProfile
        {
            ProfileName = "GST 18%",
            IsActive = true,
            Items = [
                new TaxProfileItem { TaxMasterId = cgst.Id },
                new TaxProfileItem { TaxMasterId = sgst.Id }
            ]
        };
        db.TaxProfiles.Add(profile18);
        db.SaveChanges();

        // Products
        var productA = new Product
        {
            Name = "Widget A",
            SalePrice = ProductAPrice,
            Quantity = 50,
            TaxProfileId = profile18.Id,
            IsTaxInclusive = false
        };
        var productB = new Product
        {
            Name = "Widget B",
            SalePrice = ProductBPrice,
            Quantity = 3,
            TaxProfileId = profile18.Id,
            IsTaxInclusive = false
        };
        var noStock = new Product
        {
            Name = "Out of Stock Item",
            SalePrice = 500m,
            Quantity = 0,
            TaxProfileId = profile18.Id,
            IsTaxInclusive = false
        };
        db.Products.AddRange(productA, productB, noStock);
        db.SaveChanges();

        _productAId = productA.Id;
        _productBId = productB.Id;
        _productNoStockId = noStock.Id;
    }

    private IDbContextFactory<AppDbContext> CreateFactory()
    {
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(_dbOptions)));
        return factory;
    }

    private BillingSessionRestoreService CreateSut() =>
        new(CreateFactory(), _pricing, _perf,
            NullLogger<BillingSessionRestoreService>.Instance);

    private static BillingSession CreateSession(SerializedCart cart) => new()
    {
        Id = 1,
        SessionId = cart.SessionId,
        UserId = 1,
        IsActive = true,
        SerializedBillData = JsonSerializer.Serialize(cart),
        CreatedTime = DateTime.UtcNow,
        LastUpdated = DateTime.UtcNow
    };

    // ── Happy path: all items restored ─────────────────────────────

    [Fact]
    public async Task Restore_AllItemsValid_ReturnsAllItems()
    {
        var cart = new SerializedCart
        {
            SessionId = Guid.NewGuid(),
            Items =
            [
                new SerializedCartItem
                {
                    ProductId = _productAId, ProductName = "Widget A",
                    Quantity = 2, UnitPrice = ProductAPrice,
                    TaxRate = TaxRate18, IsTaxInclusive = false
                },
                new SerializedCartItem
                {
                    ProductId = _productBId, ProductName = "Widget B",
                    Quantity = 1, UnitPrice = ProductBPrice,
                    TaxRate = TaxRate18, IsTaxInclusive = false
                }
            ]
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Empty(result.SkippedItems);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public async Task Restore_AllItemsValid_RecalculatesTotals()
    {
        var cart = new SerializedCart
        {
            SessionId = Guid.NewGuid(),
            Items =
            [
                new SerializedCartItem
                {
                    ProductId = _productAId, ProductName = "Widget A",
                    Quantity = 2, UnitPrice = ProductAPrice,
                    TaxRate = TaxRate18, IsTaxInclusive = false
                }
            ]
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.NotNull(result);
        // 100 × 2 = 200 subtotal, 18% tax = 36
        Assert.Equal(200m, result.Subtotal);
        Assert.Equal(36m, result.TotalTax);
        Assert.Equal(236m, result.GrandTotal);
    }

    [Fact]
    public async Task Restore_SessionId_MatchesCart()
    {
        var sessionId = Guid.NewGuid();
        var cart = new SerializedCart
        {
            SessionId = sessionId,
            Items =
            [
                new SerializedCartItem
                {
                    ProductId = _productAId, ProductName = "Widget A",
                    Quantity = 1, UnitPrice = ProductAPrice,
                    TaxRate = TaxRate18, IsTaxInclusive = false
                }
            ]
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.Equal(sessionId, result!.SessionId);
    }

    // ── Deleted product → skipped ──────────────────────────────────

    [Fact]
    public async Task Restore_DeletedProduct_SkippedWithReason()
    {
        var cart = new SerializedCart
        {
            SessionId = Guid.NewGuid(),
            Items =
            [
                new SerializedCartItem
                {
                    ProductId = _productAId, ProductName = "Widget A",
                    Quantity = 1, UnitPrice = ProductAPrice,
                    TaxRate = TaxRate18, IsTaxInclusive = false
                },
                new SerializedCartItem
                {
                    ProductId = 99999, ProductName = "Deleted Product",
                    Quantity = 3, UnitPrice = 50m,
                    TaxRate = 5m, IsTaxInclusive = false
                }
            ]
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Single(result.SkippedItems);
        Assert.Equal(SkipReason.ProductDeleted, result.SkippedItems[0].Reason);
        Assert.Equal("Deleted Product", result.SkippedItems[0].ProductName);
        Assert.True(result.HasWarnings);
    }

    // ── Out of stock → skipped ─────────────────────────────────────

    [Fact]
    public async Task Restore_OutOfStock_SkippedWithReason()
    {
        var cart = new SerializedCart
        {
            SessionId = Guid.NewGuid(),
            Items =
            [
                new SerializedCartItem
                {
                    ProductId = _productAId, ProductName = "Widget A",
                    Quantity = 1, UnitPrice = ProductAPrice,
                    TaxRate = TaxRate18, IsTaxInclusive = false
                },
                new SerializedCartItem
                {
                    ProductId = _productNoStockId, ProductName = "Out of Stock Item",
                    Quantity = 2, UnitPrice = 500m,
                    TaxRate = TaxRate18, IsTaxInclusive = false
                }
            ]
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Single(result.SkippedItems);
        Assert.Equal(SkipReason.OutOfStock, result.SkippedItems[0].Reason);
    }

    // ── Insufficient stock → quantity clamped ──────────────────────

    [Fact]
    public async Task Restore_InsufficientStock_ClampsQuantity()
    {
        // ProductB has Quantity=3, saved cart has Quantity=10
        var cart = new SerializedCart
        {
            SessionId = Guid.NewGuid(),
            Items =
            [
                new SerializedCartItem
                {
                    ProductId = _productBId, ProductName = "Widget B",
                    Quantity = 10, UnitPrice = ProductBPrice,
                    TaxRate = TaxRate18, IsTaxInclusive = false
                }
            ]
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(3, result.Items[0].Quantity);
    }

    // ── Price changed → uses current price, flags change ───────────

    [Fact]
    public async Task Restore_PriceChanged_UsesCurrentPrice()
    {
        var cart = new SerializedCart
        {
            SessionId = Guid.NewGuid(),
            Items =
            [
                new SerializedCartItem
                {
                    ProductId = _productAId, ProductName = "Widget A",
                    Quantity = 1, UnitPrice = 80m, // was 80, now 100
                    TaxRate = TaxRate18, IsTaxInclusive = false
                }
            ]
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.NotNull(result);
        Assert.Equal(ProductAPrice, result.Items[0].UnitPrice);
        Assert.True(result.Items[0].PriceChanged);
    }

    [Fact]
    public async Task Restore_PriceUnchanged_FlagIsFalse()
    {
        var cart = new SerializedCart
        {
            SessionId = Guid.NewGuid(),
            Items =
            [
                new SerializedCartItem
                {
                    ProductId = _productAId, ProductName = "Widget A",
                    Quantity = 1, UnitPrice = ProductAPrice,
                    TaxRate = TaxRate18, IsTaxInclusive = false
                }
            ]
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.False(result!.Items[0].PriceChanged);
    }

    // ── Discount restored ──────────────────────────────────────────

    [Fact]
    public async Task Restore_WithDiscount_RestoresDiscount()
    {
        var cart = new SerializedCart
        {
            SessionId = Guid.NewGuid(),
            Items =
            [
                new SerializedCartItem
                {
                    ProductId = _productAId, ProductName = "Widget A",
                    Quantity = 1, UnitPrice = ProductAPrice,
                    TaxRate = TaxRate18, IsTaxInclusive = false
                }
            ],
            Discount = new SerializedDiscount
            {
                Type = DiscountType.Percentage,
                Value = 10m,
                Reason = "Loyalty"
            }
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.NotNull(result);
        Assert.Equal(DiscountType.Percentage, result.Discount.Type);
        Assert.Equal(10m, result.Discount.Value);
        Assert.Equal("Loyalty", result.Discount.Reason);
    }

    [Fact]
    public async Task Restore_NoDiscount_ReturnsNone()
    {
        var cart = new SerializedCart
        {
            SessionId = Guid.NewGuid(),
            Items =
            [
                new SerializedCartItem
                {
                    ProductId = _productAId, ProductName = "Widget A",
                    Quantity = 1, UnitPrice = ProductAPrice,
                    TaxRate = TaxRate18, IsTaxInclusive = false
                }
            ]
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.Equal(DiscountType.None, result!.Discount.Type);
    }

    // ── All items skipped → returns null ────────────────────────────

    [Fact]
    public async Task Restore_AllItemsSkipped_ReturnsNull()
    {
        var cart = new SerializedCart
        {
            SessionId = Guid.NewGuid(),
            Items =
            [
                new SerializedCartItem
                {
                    ProductId = 99999, ProductName = "Deleted",
                    Quantity = 1, UnitPrice = 50m,
                    TaxRate = 5m, IsTaxInclusive = false
                }
            ]
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.Null(result);
    }

    // ── Empty / corrupt JSON → returns null ────────────────────────

    [Fact]
    public async Task Restore_EmptyItems_ReturnsNull()
    {
        var cart = new SerializedCart
        {
            SessionId = Guid.NewGuid(),
            Items = []
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.Null(result);
    }

    [Fact]
    public async Task Restore_CorruptJson_ReturnsNull()
    {
        var session = new BillingSession
        {
            Id = 1,
            SessionId = Guid.NewGuid(),
            UserId = 1,
            IsActive = true,
            SerializedBillData = "{{not valid json",
            CreatedTime = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        var result = await CreateSut().RestoreAsync(session);

        Assert.Null(result);
    }

    [Fact]
    public async Task Restore_EmptyJsonObject_ReturnsNull()
    {
        var session = new BillingSession
        {
            Id = 1,
            SessionId = Guid.NewGuid(),
            UserId = 1,
            IsActive = true,
            SerializedBillData = "{}",
            CreatedTime = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        var result = await CreateSut().RestoreAsync(session);

        Assert.Null(result);
    }

    // ── Tax profile resolution ─────────────────────────────────────

    [Fact]
    public async Task Restore_UsesCurrentTaxRate()
    {
        var cart = new SerializedCart
        {
            SessionId = Guid.NewGuid(),
            Items =
            [
                new SerializedCartItem
                {
                    ProductId = _productAId, ProductName = "Widget A",
                    Quantity = 1, UnitPrice = ProductAPrice,
                    TaxRate = 5m, // saved rate was 5%, current profile is 18%
                    IsTaxInclusive = false
                }
            ]
        };

        var result = await CreateSut().RestoreAsync(CreateSession(cart));

        Assert.NotNull(result);
        // Should use current 18% rate, not saved 5%
        Assert.Equal(TaxRate18, result.Items[0].TaxRate);
        Assert.Equal(18m, result.TotalTax); // 100 × 18% = 18
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
