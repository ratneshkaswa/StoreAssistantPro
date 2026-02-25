using NSubstitute;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class MainWorkspaceViewModelTests
{
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();

    private MainWorkspaceViewModel CreateSut() => new(_dashboardService);

    private static DashboardSummary MakeSummary(
        int totalProducts = 0,
        int lowStockCount = 0,
        int outOfStockCount = 0,
        int overstockCount = 0,
        decimal inventoryValue = 0m,
        decimal inventoryValueAtSale = 0m,
        decimal todaysSales = 0m,
        int todaysTransactions = 0,
        IReadOnlyList<Sale>? recentSales = null,
        IReadOnlyList<Product>? lowStockProducts = null,
        IReadOnlyList<BrandLowStockCount>? lowStockByBrand = null,
        IReadOnlyList<Product>? outOfStockProducts = null,
        IReadOnlyList<BrandInventoryValue>? inventoryValueByBrand = null,
        IReadOnlyList<PaymentMethodSales>? salesByPaymentMethod = null,
        IReadOnlyList<TopSellingProduct>? topSellingProducts = null) =>
        new(totalProducts, lowStockCount, outOfStockCount, overstockCount, inventoryValue, inventoryValueAtSale, todaysSales, todaysTransactions,
            todaysTransactions > 0 ? todaysSales / todaysTransactions : 0,
            recentSales ?? [], lowStockProducts ?? [], lowStockByBrand ?? [],
            outOfStockProducts ?? [], inventoryValueByBrand ?? [],
            0m,
            salesByPaymentMethod ?? [],
            topSellingProducts ?? []);

    [Fact]
    public async Task LoadMainWorkspace_SetsTotalProducts()
    {
        _dashboardService.GetSummaryAsync().Returns(MakeSummary(totalProducts: 3));

        var sut = CreateSut();
        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal(3, sut.TotalProducts);
    }

    [Fact]
    public async Task LoadMainWorkspace_CountsLowStockProducts()
    {
        _dashboardService.GetSummaryAsync().Returns(MakeSummary(
            totalProducts: 4, lowStockCount: 3));

        var sut = CreateSut();
        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal(3, sut.LowStockCount);
    }

    [Fact]
    public async Task LoadMainWorkspace_CalculatesTodaysSalesTotal()
    {
        _dashboardService.GetSummaryAsync().Returns(MakeSummary(
            todaysSales: 150m, todaysTransactions: 2));

        var sut = CreateSut();
        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal(150m, sut.TodaysSales);
        Assert.Equal(2, sut.TodaysTransactions);
    }

    [Fact]
    public async Task LoadMainWorkspace_WithNoData_AllValuesAreZero()
    {
        _dashboardService.GetSummaryAsync().Returns(MakeSummary());

        var sut = CreateSut();
        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal(0, sut.TotalProducts);
        Assert.Equal(0, sut.LowStockCount);
        Assert.Equal(0m, sut.TodaysSales);
        Assert.Equal(0, sut.TodaysTransactions);
    }

    [Fact]
    public async Task LoadMainWorkspace_PopulatesRecentSales()
    {
        var sales = new List<Sale>
        {
            new() { Id = 1, TotalAmount = 100m, PaymentMethod = "Cash" }
        };
        _dashboardService.GetSummaryAsync().Returns(MakeSummary(recentSales: sales));

        var sut = CreateSut();
        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);

        Assert.Single(sut.RecentSales);
        Assert.Equal(100m, sut.RecentSales[0].TotalAmount);
    }

    [Fact]
    public async Task LoadMainWorkspace_PopulatesLowStockProducts()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Widget", Quantity = 2, SalePrice = 50m }
        };
        _dashboardService.GetSummaryAsync().Returns(MakeSummary(lowStockProducts: products));

        var sut = CreateSut();
        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);

        Assert.Single(sut.LowStockProducts);
        Assert.Equal("Widget", sut.LowStockProducts[0].Name);
    }

    [Fact]
    public async Task LoadMainWorkspace_Refresh_ClearsAndRepopulatesCollections()
    {
        var firstSales = new List<Sale>
        {
            new() { Id = 1, TotalAmount = 100m, PaymentMethod = "Cash" }
        };
        _dashboardService.GetSummaryAsync().Returns(MakeSummary(recentSales: firstSales));

        var sut = CreateSut();
        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);
        Assert.Single(sut.RecentSales);

        var secondSales = new List<Sale>
        {
            new() { Id = 2, TotalAmount = 200m, PaymentMethod = "Card" },
            new() { Id = 3, TotalAmount = 300m, PaymentMethod = "Cash" }
        };
        _dashboardService.GetSummaryAsync().Returns(MakeSummary(recentSales: secondSales));

        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);
        Assert.Equal(2, sut.RecentSales.Count);
    }
}
