using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Reports.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.Services;

public sealed class ReportsServiceFixtureTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbFactory = new();
    private readonly IReferenceDataCache _cache = new ReferenceDataCache();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    [Fact]
    public async Task GetDailySalesSummaryAsync_ShouldIncludeReturnsTaxAndDiscount()
    {
        var reportDate = new DateTime(2026, 3, 25);

        await using (var seed = _dbFactory.CreateContext())
        {
            var product = new Product
            {
                Name = "Daily Report Shirt",
                SalePrice = 100m,
                CostPrice = 60m,
                Quantity = 20,
                IsActive = true
            };
            seed.Products.Add(product);
            await seed.SaveChangesAsync();

            var sale = new Sale
            {
                InvoiceNumber = "INV-20260325-0001",
                SaleDate = reportDate.AddHours(10),
                TotalAmount = 190m,
                PaymentMethod = "Cash",
                DiscountType = DiscountType.Amount,
                DiscountValue = 10m,
                DiscountAmount = 10m,
                IdempotencyKey = Guid.NewGuid()
            };
            seed.Sales.Add(sale);
            await seed.SaveChangesAsync();

            seed.SaleItems.Add(new SaleItem
            {
                SaleId = sale.Id,
                ProductId = product.Id,
                Quantity = 2,
                UnitPrice = 100m,
                ItemDiscountRate = 0m,
                ItemFlatDiscount = 0m,
                TaxRate = 5m,
                TaxAmount = 19m,
                IsTaxInclusive = false
            });
            seed.SaleReturns.Add(new SaleReturn
            {
                SaleId = sale.Id,
                SaleItemId = 1,
                ReturnNumber = "RET-0001",
                Quantity = 1,
                RefundAmount = 50m,
                CreditNoteNumber = "CN-0001",
                Reason = "Damaged",
                ReturnDate = reportDate.AddHours(12)
            });
            seed.Sales.Add(new Sale
            {
                InvoiceNumber = "INV-20260324-0009",
                SaleDate = reportDate.AddDays(-1).AddHours(9),
                TotalAmount = 999m,
                PaymentMethod = "Cash",
                IdempotencyKey = Guid.NewGuid()
            });
            await seed.SaveChangesAsync();
        }

        var sut = new ReportsService(_dbFactory, _perf, _cache);

        var summary = await sut.GetDailySalesSummaryAsync(reportDate);

        Assert.Equal(1, summary.SaleCount);
        Assert.Equal(190m, summary.TotalSales);
        Assert.Equal(50m, summary.TotalReturns);
        Assert.Equal(140m, summary.NetSales);
        Assert.Equal(19m, summary.TotalTax);
        Assert.Equal(10m, summary.TotalDiscount);
    }

    [Fact]
    public async Task ProfitReports_ShouldUseDeterministicFixtureValues()
    {
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 31, 23, 59, 59);

        await using (var seed = _dbFactory.CreateContext())
        {
            var productA = new Product
            {
                Name = "Gross Product A",
                SalePrice = 100m,
                CostPrice = 60m,
                Quantity = 10,
                IsActive = true
            };
            var productB = new Product
            {
                Name = "Gross Product B",
                SalePrice = 150m,
                CostPrice = 90m,
                Quantity = 10,
                IsActive = true
            };

            seed.Products.AddRange(productA, productB);
            await seed.SaveChangesAsync();

            var saleOne = new Sale
            {
                InvoiceNumber = "INV-20260301-0001",
                SaleDate = from.AddDays(2),
                TotalAmount = 200m,
                PaymentMethod = "Cash",
                IdempotencyKey = Guid.NewGuid(),
                CashierRole = "Admin"
            };
            var saleTwo = new Sale
            {
                InvoiceNumber = "INV-20260301-0002",
                SaleDate = from.AddDays(5),
                TotalAmount = 150m,
                PaymentMethod = "Card",
                IdempotencyKey = Guid.NewGuid(),
                CashierRole = "Cashier"
            };

            seed.Sales.AddRange(saleOne, saleTwo);
            await seed.SaveChangesAsync();

            seed.SaleItems.AddRange(
                new SaleItem
                {
                    SaleId = saleOne.Id,
                    ProductId = productA.Id,
                    Quantity = 2,
                    UnitPrice = 100m,
                    TaxRate = 5m,
                    TaxAmount = 10m
                },
                new SaleItem
                {
                    SaleId = saleTwo.Id,
                    ProductId = productB.Id,
                    Quantity = 1,
                    UnitPrice = 150m,
                    TaxRate = 12m,
                    TaxAmount = 18m
                });

            await seed.SaveChangesAsync();
        }

        var sut = new ReportsService(_dbFactory, _perf, _cache);

        var gross = await sut.GetGrossProfitReportAsync(from, to);
        var net = await sut.GetNetProfitReportAsync(from, to);

        Assert.Equal(350m, gross.TotalRevenue);
        Assert.Equal(210m, gross.TotalCostOfGoodsSold);
        Assert.Equal(140m, gross.GrossProfit);
        Assert.Equal(40m, net.TotalExpenses);
        Assert.Equal(100m, net.NetProfit);
        Assert.Equal(40m, gross.GrossMarginPercent);
        Assert.Equal(28.6m, net.NetMarginPercent);
    }

    [Fact]
    public async Task GetSalesByPaymentMethodAsync_ShouldReturnStablePercentages()
    {
        var from = new DateTime(2026, 3, 1);
        var to = from.AddDays(2);

        await using (var seed = _dbFactory.CreateContext())
        {
            seed.Sales.AddRange(
                new Sale
                {
                    InvoiceNumber = "INV-001",
                    SaleDate = from.AddHours(1),
                    TotalAmount = 100m,
                    PaymentMethod = "Cash",
                    IdempotencyKey = Guid.NewGuid()
                },
                new Sale
                {
                    InvoiceNumber = "INV-002",
                    SaleDate = from.AddHours(2),
                    TotalAmount = 50m,
                    PaymentMethod = "UPI",
                    IdempotencyKey = Guid.NewGuid()
                },
                new Sale
                {
                    InvoiceNumber = "INV-003",
                    SaleDate = from.AddHours(3),
                    TotalAmount = 50m,
                    PaymentMethod = "Cash",
                    IdempotencyKey = Guid.NewGuid()
                });

            await seed.SaveChangesAsync();
        }

        var sut = new ReportsService(_dbFactory, _perf, _cache);

        var summary = await sut.GetSalesByPaymentMethodAsync(from, to);

        Assert.Collection(summary,
            cash =>
            {
                Assert.Equal("Cash", cash.PaymentMethod);
                Assert.Equal(2, cash.SaleCount);
                Assert.Equal(150m, cash.TotalAmount);
                Assert.Equal(75m, cash.Percentage);
            },
            upi =>
            {
                Assert.Equal("UPI", upi.PaymentMethod);
                Assert.Equal(1, upi.SaleCount);
                Assert.Equal(50m, upi.TotalAmount);
                Assert.Equal(25m, upi.Percentage);
            });
    }

    [Fact]
    public async Task InvalidateCache_ShouldForceReportsToSeeFreshSalesData()
    {
        var reportDate = new DateTime(2026, 3, 26);

        await using (var seed = _dbFactory.CreateContext())
        {
            seed.Sales.Add(new Sale
            {
                InvoiceNumber = "INV-CACHE-001",
                SaleDate = reportDate.AddHours(9),
                TotalAmount = 100m,
                PaymentMethod = "Cash",
                IdempotencyKey = Guid.NewGuid()
            });

            await seed.SaveChangesAsync();
        }

        var sut = new ReportsService(_dbFactory, _perf, _cache);

        var first = await sut.GetDailySalesSummaryAsync(reportDate);

        await using (var mutate = _dbFactory.CreateContext())
        {
            mutate.Sales.Add(new Sale
            {
                InvoiceNumber = "INV-CACHE-002",
                SaleDate = reportDate.AddHours(10),
                TotalAmount = 50m,
                PaymentMethod = "UPI",
                IdempotencyKey = Guid.NewGuid()
            });

            await mutate.SaveChangesAsync();
        }

        var cached = await sut.GetDailySalesSummaryAsync(reportDate);
        sut.InvalidateCache();
        var refreshed = await sut.GetDailySalesSummaryAsync(reportDate);

        Assert.Equal(1, first.SaleCount);
        Assert.Equal(1, cached.SaleCount);
        Assert.Equal(2, refreshed.SaleCount);
        Assert.Equal(150m, refreshed.TotalSales);
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }
}
