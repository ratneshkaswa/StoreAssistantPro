using NSubstitute;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class MainWorkspaceViewModelTests
{
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();

    private MainWorkspaceViewModel CreateSut() => new(_dashboardService);

    [Fact]
    public async Task LoadMainWorkspace_SetsTotalProducts()
    {
        _dashboardService.GetSummaryAsync().Returns(new DashboardSummary(
            TotalProducts: 3,
            LowStockCount: 0,
            TodaysSales: 0m,
            TodaysTransactions: 0));

        var sut = CreateSut();
        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal(3, sut.TotalProducts);
    }

    [Fact]
    public async Task LoadMainWorkspace_CountsLowStockProducts()
    {
        _dashboardService.GetSummaryAsync().Returns(new DashboardSummary(
            TotalProducts: 4,
            LowStockCount: 3,
            TodaysSales: 0m,
            TodaysTransactions: 0));

        var sut = CreateSut();
        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal(3, sut.LowStockCount);
    }

    [Fact]
    public async Task LoadMainWorkspace_CalculatesTodaysSalesTotal()
    {
        _dashboardService.GetSummaryAsync().Returns(new DashboardSummary(
            TotalProducts: 0,
            LowStockCount: 0,
            TodaysSales: 150m,
            TodaysTransactions: 2));

        var sut = CreateSut();
        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal(150m, sut.TodaysSales);
        Assert.Equal(2, sut.TodaysTransactions);
    }

    [Fact]
    public async Task LoadMainWorkspace_WithNoData_AllValuesAreZero()
    {
        _dashboardService.GetSummaryAsync().Returns(new DashboardSummary(
            TotalProducts: 0,
            LowStockCount: 0,
            TodaysSales: 0m,
            TodaysTransactions: 0));

        var sut = CreateSut();
        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal(0, sut.TotalProducts);
        Assert.Equal(0, sut.LowStockCount);
        Assert.Equal(0m, sut.TodaysSales);
        Assert.Equal(0, sut.TodaysTransactions);
    }
}
