using NSubstitute;
using StoreAssistantPro.Core.Controls;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class WorkspaceViewModelTests
{
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    public WorkspaceViewModelTests()
    {
        _dashboardService.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(DashboardSummary.Empty);
        _regional.CurrencySymbol.Returns("Ã¢â€šÂ¹");
        _regional.Now.Returns(new DateTime(2026, 3, 19, 10, 30, 0));
        _regional.FormatCurrency(Arg.Any<decimal>()).Returns(ci => $"Ã¢â€šÂ¹{ci.Arg<decimal>():N0}");
        _regional.FormatDateTime(Arg.Any<DateTime>())
            .Returns(ci => ci.Arg<DateTime>().ToString("dd-MMM HH:mm"));
    }

    private WorkspaceViewModel CreateSut() => new(_dashboardService, _regional, _eventBus);

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
            TodayReturns = 100m,
            TodayNetSales = 4900m,
            AverageBillValue = 1700m,
            ThisMonthSales = 125000m,
            ThisMonthTransactions = 18,
            PreviousDaySales = 4000m,
            PreviousDayReturns = 200m,
            PreviousDayNetSales = 3800m,
            PreviousDayAverageBillValue = 1500m,
            PreviousMonthSales = 100000m,
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
            TopProductsToday =
            [
                new TopProductItem("Cotton Shirt", 18, 45000m)
            ]
        };

        _dashboardService.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(summary);
        var sut = CreateSut();

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal(5000m, sut.TodaySales);
        Assert.Equal("Ã¢â€šÂ¹5K", sut.TodaySalesCompact);
        Assert.Equal("Ã¢â€šÂ¹1.2L", sut.ThisMonthSalesCompact);
        Assert.Equal("42", sut.TotalProductsCompact);
        Assert.Equal("Ã¢â€šÂ¹45K", sut.ReceivablesCompact);
        Assert.Equal("Updated Just now", sut.LastUpdatedText);
        Assert.True(sut.HasAlertBanner);
        Assert.Equal(InfoBarSeverity.Error, sut.AlertSeverity);
        Assert.Equal("↑", sut.TodaySalesTrend.Glyph);
        Assert.Equal("25% vs yesterday", sut.TodaySalesTrend.Label);
        Assert.Equal(KpiTrendTone.Positive, sut.TodaySalesTrend.Tone);
        Assert.Equal("↓", sut.TodayReturnsTrend.Glyph);
        Assert.Equal(KpiTrendTone.Positive, sut.TodayReturnsTrend.Tone);
        Assert.Equal("25% vs last month", sut.ThisMonthSalesTrend.Label);
        Assert.Single(sut.RecentSalesDisplay);
        Assert.Equal("5 min ago", sut.RecentSalesDisplay[0].RelativeDate);
        Assert.Single(sut.TopProductsTodayDisplay);
        Assert.Equal("Ã¢â€šÂ¹45K", sut.TopProductsTodayDisplay[0].RevenueCompact);
    }

    [Fact]
    public async Task OnNavigatedTo_Should_Not_Refetch_When_Data_Is_Still_Fresh()
    {
        var sut = CreateSut();

        await sut.OnNavigatedTo();
        await sut.OnNavigatedTo();

        await _dashboardService.Received(1).GetSummaryAsync(Arg.Any<CancellationToken>());
        sut.OnNavigatedFrom();
    }

    [Fact]
    public async Task RefreshCommand_When_ViewingPastDate_Should_Use_SelectedDate_Summary()
    {
        var selectedDate = new DateTime(2026, 3, 18);
        _dashboardService.GetSummaryForDateAsync(selectedDate, Arg.Any<CancellationToken>())
            .Returns(DashboardSummary.Empty);

        var sut = CreateSut();
        sut.SelectedDate = selectedDate;

        await sut.RefreshCommand.ExecuteAsync(null);

        await _dashboardService.Received().GetSummaryForDateAsync(selectedDate, Arg.Any<CancellationToken>());
        await _dashboardService.DidNotReceive().GetSummaryAsync(Arg.Any<CancellationToken>());
    }
}
