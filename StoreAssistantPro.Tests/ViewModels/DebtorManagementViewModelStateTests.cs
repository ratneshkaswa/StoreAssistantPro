using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Debtors.Services;
using StoreAssistantPro.Modules.Debtors.ViewModels;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public sealed class DebtorManagementViewModelStateTests : IDisposable
{
    private readonly IDebtorService _debtorService = Substitute.For<IDebtorService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    public DebtorManagementViewModelStateTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public async Task LoadCommand_Should_Restore_Persisted_Search_And_Status_Filter()
    {
        UserPreferencesStore.SetDebtorManagementState(new SearchFilterViewState
        {
            SearchText = "Asha",
            ActiveFilter = "Pending"
        });

        _regional.CurrencySymbol.Returns("Rs.");
        _debtorService.GetStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new DebtorStats(1, 1, 900m, 0m));
        _debtorService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Debtor>>([
                new Debtor
                {
                    Id = 21,
                    Name = "Asha Textiles",
                    Phone = "9999999999",
                    TotalAmount = 900m,
                    PaidAmount = 0m,
                    Date = DateTime.Today
                }
            ]));

        var sut = new DebtorManagementViewModel(_debtorService, _regional);

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Asha", sut.SearchText);
        Assert.Equal("Pending", sut.ActiveStatusFilter);
        Assert.Single(sut.Debtors);
        Assert.Equal("1 debtors", sut.FilterCountText);
    }
}
