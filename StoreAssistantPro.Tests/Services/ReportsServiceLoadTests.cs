using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Expenses.Services;
using StoreAssistantPro.Modules.Reports.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.Services;

public sealed class ReportsServiceLoadTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbFactory = new();
    private readonly IExpenseService _expenseService = Substitute.For<IExpenseService>();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    [Fact]
    public async Task ProductAndProfitReports_Should_HandleLargeSeededDataset()
    {
        await using (var seed = _dbFactory.CreateContext())
        {
            await LargeRetailDatasetSeeder.SeedAsync(seed, productCount: 600, saleCount: 900, itemsPerSale: 4);
        }

        _expenseService.GetMonthlyExpenseReportAsync(2026, 3, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new MonthlyExpenseReport(2026, 3, 500m, 5, [])));

        var sut = new ReportsService(_dbFactory, _expenseService, _perf);
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 31, 23, 59, 59);

        var productReport = await sut.GetProductSalesReportAsync(from, to);
        var grossProfit = await sut.GetGrossProfitReportAsync(from, to);
        var bestSellers = await sut.GetBestSellingProductsAsync(from, to, 10);

        Assert.NotEmpty(productReport);
        Assert.True(grossProfit.SaleCount >= 900);
        Assert.Equal(10, bestSellers.Count);
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }
}
