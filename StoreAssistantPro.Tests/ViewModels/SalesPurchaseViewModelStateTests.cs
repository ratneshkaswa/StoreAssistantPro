using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.SalesPurchase.Services;
using StoreAssistantPro.Modules.SalesPurchase.ViewModels;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public sealed class SalesPurchaseViewModelStateTests : IDisposable
{
    private readonly ISalesPurchaseService _service = Substitute.For<ISalesPurchaseService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    public SalesPurchaseViewModelStateTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public async Task LoadCommand_Should_Restore_Search_And_Both_Filters()
    {
        UserPreferencesStore.SetSalesPurchaseManagementState(new DualFilterViewState
        {
            SearchText = "bill",
            PrimaryFilter = "Month",
            SecondaryFilter = "Sales"
        });

        _regional.CurrencySymbol.Returns("Rs.");
        _service.GetStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new SalesPurchaseStats(2000m, 800m, 1200m, 2));
        _service.GetAllAsync(ct: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SalesPurchaseEntry>>([
                new SalesPurchaseEntry
                {
                    Id = 10,
                    Date = DateTime.Today,
                    Note = "Bill 42",
                    Amount = 2000m,
                    Type = "Sales"
                },
                new SalesPurchaseEntry
                {
                    Id = 11,
                    Date = DateTime.Today,
                    Note = "Vendor invoice",
                    Amount = 800m,
                    Type = "Purchase"
                }
            ]));

        var sut = new SalesPurchaseViewModel(
            _service,
            Substitute.For<IToastService>(),
            _regional);

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal("bill", sut.SearchText);
        Assert.Equal("Month", sut.ActiveDateFilter);
        Assert.Equal("Sales", sut.ActiveTypeFilter);
        Assert.Single(sut.Entries);
        Assert.Equal("Bill 42", sut.Entries[0].Note);
        Assert.Equal("1 entries", sut.FilterCountText);
    }
}
