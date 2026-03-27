using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Reports.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.Services;

public sealed class ReportsServiceLoadTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbFactory = new();
    private readonly IReferenceDataCache _cache = new ReferenceDataCache();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    [Fact]
    public async Task ProductAndProfitReports_Should_HandleLargeSeededDataset()
    {
        await using (var seed = _dbFactory.CreateContext())
        {
            await LargeRetailDatasetSeeder.SeedAsync(seed, productCount: 600, saleCount: 900, itemsPerSale: 4);
        }

        var sut = new ReportsService(_dbFactory, _perf, _cache);
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 31, 23, 59, 59);

        var productReport = await sut.GetProductSalesReportAsync(from, to);
        var grossProfit = await sut.GetGrossProfitReportAsync(from, to);
        var bestSellers = await sut.GetBestSellingProductsAsync(from, to, 10);

        Assert.NotEmpty(productReport);
        Assert.True(grossProfit.SaleCount >= 900);
        Assert.Equal(10, bestSellers.Count);
    }

    [Fact]
    public async Task RepeatedRangeQueries_ShouldBenefitFromCache_OnLargeSeededDataset()
    {
        await using (var seed = _dbFactory.CreateContext())
        {
            await LargeRetailDatasetSeeder.SeedAsync(seed, productCount: 900, saleCount: 1400, itemsPerSale: 4);
        }

        var sut = new ReportsService(_dbFactory, _perf, _cache);
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 31, 23, 59, 59);

        var firstStopwatch = Stopwatch.StartNew();
        var first = await sut.GetProductSalesReportAsync(from, to);
        firstStopwatch.Stop();

        var secondStopwatch = Stopwatch.StartNew();
        var second = await sut.GetProductSalesReportAsync(from, to);
        secondStopwatch.Stop();

        Assert.Equal(first.Count, second.Count);
        Assert.True(secondStopwatch.Elapsed <= firstStopwatch.Elapsed + TimeSpan.FromMilliseconds(25),
            $"Expected cached report load to be no slower than cold load. Cold={firstStopwatch.Elapsed.TotalMilliseconds:F1}ms, Cached={secondStopwatch.Elapsed.TotalMilliseconds:F1}ms.");
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }
}
