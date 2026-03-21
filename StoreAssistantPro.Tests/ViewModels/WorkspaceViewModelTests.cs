using NSubstitute;
using StoreAssistantPro.Core.Controls;
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
        _regional.CurrencySymbol.Returns("₹");
        _regional.Now.Returns(new DateTime(2026, 3, 19, 10, 30, 0));
        _regional.FormatCurrency(Arg.Any<decimal>()).Returns(ci => $"₹{ci.Arg<decimal>():N0}");
        _regional.FormatDateTime(Arg.Any<DateTime>())
            .Returns(ci => ci.Arg<DateTime>().ToString("dd-MMM HH:mm"));
    }

    private WorkspaceViewModel CreateSut() => new(_dashboardService, _regional);

    [Fact]
    public void CreateSut_DoesNotThrow()
    {
        var sut = CreateSut();

        Assert.NotNull(sut);
    }

    [Fact]
    public async Task LoadCommand_PopulatesDashboardDisplayState()
    {
        var now = new DateTime(2026, 3, 19, 10, 30, 0);
        _regional.Now.Returns(now);

        var summary = new DashboardSummary
        {
            TodaySales = 5000m,
            TodayTransactions = 3,
            ThisMonthSales = 125000m,
            ThisMonthTransactions = 18,
            TotalProducts = 42,
            LowStockCount = 5,
            OutOfStockCount = 2,
            PendingOrdersCount = 4,
            OverdueOrdersCount = 1,
            OutstandingReceivables = 45000m,
            RecentSales =
            [
                new RecentSaleItem("INV-101", now.AddMinutes(-5), 2500m, "UPI", 3)
            ],
            TopProducts =
            [
                new TopProductItem("Cotton Shirt", 18, 45000m)
            ]
        };

        _dashboardService.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(summary);
        var sut = CreateSut();

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal(5000m, sut.TodaySales);
        Assert.Equal("₹5K", sut.TodaySalesCompact);
        Assert.Equal("₹1.2L", sut.ThisMonthSalesCompact);
        Assert.Equal("42", sut.TotalProductsCompact);
        Assert.Equal("₹45K", sut.ReceivablesCompact);
        Assert.Equal("Updated Just now", sut.LastUpdatedText);
        Assert.True(sut.HasAlertBanner);
        Assert.Equal(InfoBarSeverity.Error, sut.AlertSeverity);
        Assert.Single(sut.RecentSalesDisplay);
        Assert.Equal("5 min ago", sut.RecentSalesDisplay[0].RelativeDate);
        Assert.Single(sut.TopProductsDisplay);
        Assert.Equal("₹45K", sut.TopProductsDisplay[0].RevenueCompact);
    }
}
