using NSubstitute;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Expenses.Services;
using StoreAssistantPro.Modules.Expenses.ViewModels;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public sealed class ExpenseManagementViewModelStateTests : IDisposable
{
    private readonly IExpenseService _expenseService = Substitute.For<IExpenseService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    public ExpenseManagementViewModelStateTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public async Task LoadCommand_Should_Restore_Persisted_Search_Filter_And_Page()
    {
        UserPreferencesStore.SetExpenseManagementState(new PagedSearchFilterViewState
        {
            SearchText = "rent",
            ActiveFilter = "Week",
            CurrentPage = 2
        });

        _regional.CurrencySymbol.Returns("Rs.");
        _expenseService.GetCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ExpenseCategory>>(Array.Empty<ExpenseCategory>()));
        _expenseService.GetStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new ExpenseStats(500m, 1200m, 700m, 1, 100m, 500m, 300m));
        _expenseService.GetPagedAsync(Arg.Any<PagedQuery>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.ArgAt<PagedQuery>(0);
                return new PagedResult<Expense>(
                [
                    new Expense
                    {
                        Id = 31,
                        Date = DateTime.Today,
                        Category = "Rent",
                        Amount = 500m
                    }
                ], 30, query.Page, query.PageSize);
            });
        _expenseService.GetDepositsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PettyCashDeposit>>(Array.Empty<PettyCashDeposit>()));

        var sut = new ExpenseManagementViewModel(_expenseService, _regional);

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal("rent", sut.SearchText);
        Assert.Equal("Week", sut.ActiveDateFilter);
        Assert.Equal(2, sut.CurrentPage);
        Assert.Equal("30 entries", sut.FilterCountText);
        await _expenseService.Received(1).GetPagedAsync(
            Arg.Is<PagedQuery>(query => query.Page == 2 && query.PageSize == 25),
            "rent",
            "Week",
            Arg.Any<CancellationToken>());
    }
}
