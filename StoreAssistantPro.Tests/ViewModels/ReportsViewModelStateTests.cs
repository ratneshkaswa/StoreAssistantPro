using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Printing;
using StoreAssistantPro.Modules.Reports.Services;
using StoreAssistantPro.Modules.Reports.ViewModels;
using StoreAssistantPro.Tests.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public sealed class ReportsViewModelStateTests : IDisposable
{
    private readonly IReportsService _reportsService = Substitute.For<IReportsService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    public ReportsViewModelStateTests()
    {
        UserPreferencesStore.ClearForTests();
        ConfigureDefaultReportsResponses();
    }

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public async Task LoadCommand_Should_Restore_Persisted_Date_Range_And_Preset()
    {
        UserPreferencesStore.SetReportsState(new ReportsViewState
        {
            DateFrom = new DateTime(2026, 2, 1),
            DateTo = new DateTime(2026, 2, 28),
            ActivePreset = "Last Month"
        });

        var sut = CreateSut();

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal(new DateTime(2026, 2, 1), sut.DateFrom);
        Assert.Equal(new DateTime(2026, 2, 28), sut.DateTo);
        Assert.Equal("Last Month", sut.ActivePreset);
        await _reportsService.Received(1).GetGrossProfitReportAsync(
            new DateTime(2026, 2, 1),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Updating_Date_Range_Should_Persist_View_State()
    {
        var sut = CreateSut();

        sut.DateFrom = new DateTime(2026, 1, 1);
        sut.DateTo = new DateTime(2026, 1, 31);
        sut.ActivePreset = "This Year";

        var state = UserPreferencesStore.GetReportsState();

        Assert.Equal(new DateTime(2026, 1, 1), state.DateFrom);
        Assert.Equal(new DateTime(2026, 1, 31), state.DateTo);
        Assert.Equal("This Year", state.ActivePreset);
    }

    [Fact]
    public async Task Refresh_Should_Update_Report_Context_Summaries()
    {
        var sut = CreateSut();
        sut.DateFrom = new DateTime(2026, 1, 1);
        sut.DateTo = new DateTime(2026, 1, 31);
        sut.ActivePreset = "This Month";

        await sut.RefreshCommand.ExecuteAsync(null);

        Assert.Equal("01 Jan 2026 - 31 Jan 2026", sut.SelectedPeriodSummary);
        Assert.Equal("This Month", sut.SelectedPresetSummary);
        Assert.NotNull(sut.LastRefreshedAt);
        Assert.StartsWith("Updated ", sut.LastUpdatedSummary);
    }

    private ReportsViewModel CreateSut() => new(
        _reportsService,
        Substitute.For<IPrintReportService>(),
        Substitute.For<IPrintPreviewService>(),
        Substitute.For<IAuditService>(),
        _eventBus,
        Substitute.For<IRegionalSettingsService>());

    private void ConfigureDefaultReportsResponses()
    {
        _reportsService.GetDailySalesSummaryAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new DailySalesSummary(DateTime.Today, 0, 0, 0, 0, 0, 0));
        _reportsService.GetGrossProfitReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new GrossProfitReport(DateTime.Today, DateTime.Today, 0, 0, 0, 0, 0, 0));
        _reportsService.GetNetProfitReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new NetProfitReport(DateTime.Today, DateTime.Today, 0, 0, 0, 0, 0, 0));
        _reportsService.GetTaxReportAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new TaxReport(DateTime.Today.Year, DateTime.Today.Month, 0, 0, 0, 0, Array.Empty<HsnTaxSummaryLine>()));
        _reportsService.GetBestSellingProductsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ProductSalesSummary>>(Array.Empty<ProductSalesSummary>()));
        _reportsService.GetSlowMovingProductsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ProductSalesSummary>>(Array.Empty<ProductSalesSummary>()));
        _reportsService.GetSalesByPaymentMethodAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PaymentMethodSummary>>(Array.Empty<PaymentMethodSummary>()));
        _reportsService.GetSalesByUserReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<UserSalesSummary>>(Array.Empty<UserSalesSummary>()));
        _reportsService.GetExpenseReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new ExpenseReport(0, 0, Array.Empty<CategoryBreakdown>(), Array.Empty<MonthlyTotal>(), Array.Empty<Expense>()));
        _reportsService.GetIroningReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new IroningReport(0, 0, 0, 0, Array.Empty<IroningEntry>()));
        _reportsService.GetOrderReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new OrderReport(0, 0, 0, 0, Array.Empty<Order>()));
        _reportsService.GetInwardReportAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new InwardReport(0, 0, Array.Empty<InwardEntry>()));
        _reportsService.GetDebtorReportAsync(Arg.Any<CancellationToken>())
            .Returns(new DebtorReport(0, 0, Array.Empty<TopDebtor>()));
    }
}
