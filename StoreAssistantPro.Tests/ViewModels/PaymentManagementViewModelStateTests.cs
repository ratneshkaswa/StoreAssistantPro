using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Payments.Services;
using StoreAssistantPro.Modules.Payments.ViewModels;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public sealed class PaymentManagementViewModelStateTests : IDisposable
{
    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    public PaymentManagementViewModelStateTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public async Task LoadCommand_Should_Restore_Persisted_Search_And_Date_Filter()
    {
        UserPreferencesStore.SetPaymentManagementState(new SearchFilterViewState
        {
            SearchText = "cash",
            ActiveFilter = "Month"
        });

        _regional.CurrencySymbol.Returns("Rs.");
        _paymentService.GetCustomersAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Customer>>(Array.Empty<Customer>()));
        _paymentService.GetStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new PaymentStats(1, 1200m));
        _paymentService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Payment>>([
                new Payment
                {
                    Id = 11,
                    PaymentDate = DateTime.Today,
                    Amount = 1200m,
                    Note = "Cash settlement",
                    Customer = new Customer { Id = 2, Name = "Contoso" }
                }
            ]));

        var sut = new PaymentManagementViewModel(_paymentService, _regional);

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal("cash", sut.SearchText);
        Assert.Equal("Month", sut.ActiveDateFilter);
        Assert.Single(sut.Payments);
        Assert.Equal("1 payments", sut.FilterCountText);
    }
}
