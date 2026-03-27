using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Modules.Billing.ViewModels;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public sealed class SaleHistoryViewModelStateTests : IDisposable
{
    private readonly ISaleHistoryService _historyService = Substitute.For<ISaleHistoryService>();
    private readonly IReceiptService _receiptService = Substitute.For<IReceiptService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    public SaleHistoryViewModelStateTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public async Task LoadCommand_Should_Restore_Persisted_Filters()
    {
        UserPreferencesStore.SetSaleHistoryState(new SaleHistoryViewState
        {
            DateFrom = new DateTime(2026, 3, 1),
            DateTo = new DateTime(2026, 3, 31),
            InvoiceSearch = "INV-42"
        });
        _historyService.GetPagedAsync(Arg.Any<PagedQuery>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Sale>(Array.Empty<Sale>(), 0, 1, 25));

        var sut = new SaleHistoryViewModel(_historyService, _receiptService, _regional, _eventBus);

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal(new DateTime(2026, 3, 1), sut.DateFrom);
        Assert.Equal(new DateTime(2026, 3, 31), sut.DateTo);
        Assert.Equal("INV-42", sut.InvoiceSearch);
        await _historyService.Received(1).GetPagedAsync(
            Arg.Any<PagedQuery>(),
            new DateTime(2026, 3, 1),
            new DateTime(2026, 3, 31),
            "INV-42",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Changing_Filters_Should_Persist_View_State()
    {
        var sut = new SaleHistoryViewModel(_historyService, _receiptService, _regional, _eventBus)
        {
            DateFrom = new DateTime(2026, 4, 1),
            DateTo = new DateTime(2026, 4, 30),
            InvoiceSearch = "INV-77"
        };

        var snapshot = UserPreferencesStore.GetSaleHistoryState();

        Assert.Equal(new DateTime(2026, 4, 1), snapshot.DateFrom);
        Assert.Equal(new DateTime(2026, 4, 30), snapshot.DateTo);
        Assert.Equal("INV-77", snapshot.InvoiceSearch);
    }
}
