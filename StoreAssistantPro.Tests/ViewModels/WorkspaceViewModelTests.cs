using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class WorkspaceViewModelTests
{
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    public WorkspaceViewModelTests()
    {
        _dashboardService.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(DashboardSummary.Empty);
        _regional.FormatCurrency(Arg.Any<decimal>()).Returns(ci => $"\u20B9{ci.Arg<decimal>():N0}");
    }

    private WorkspaceViewModel CreateSut() => new(_dashboardService, _regional);

    [Fact]
    public void CreateSut_DoesNotThrow()
    {
        var sut = CreateSut();

        Assert.NotNull(sut);
    }

    [Fact]
    public async Task LoadCommand_PopulatesKpis()
    {
        var summary = new DashboardSummary
        {
            TodaySales = 5000m,
            TodayTransactions = 3,
            TotalProducts = 42,
            LowStockCount = 5,
        };
        _dashboardService.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(summary);
        var sut = CreateSut();

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal(5000m, sut.TodaySales);
        Assert.Equal(3, sut.TodayTransactions);
        Assert.Equal(42, sut.TotalProducts);
        Assert.Equal(5, sut.LowStockCount);
    }
}
