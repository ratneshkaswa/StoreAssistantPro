using NSubstitute;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.GRN.Services;
using StoreAssistantPro.Modules.GRN.ViewModels;
using StoreAssistantPro.Modules.PurchaseOrders.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public sealed class GRNViewModelStateTests : IDisposable
{
    private readonly IGRNService _grnService = Substitute.For<IGRNService>();
    private readonly IPurchaseOrderService _purchaseOrderService = Substitute.For<IPurchaseOrderService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();

    public GRNViewModelStateTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public async Task LoadCommand_Should_Restore_Search_Status_And_CurrentPage()
    {
        UserPreferencesStore.SetGoodsReceivedNotesState(new PagedSearchFilterViewState
        {
            SearchText = "GRN-21",
            ActiveFilter = "Confirmed",
            CurrentPage = 3
        });

        _purchaseOrderService.GetPagedAsync(
                Arg.Any<PagedQuery>(),
                Arg.Any<string?>(),
                Arg.Any<PurchaseOrderStatus?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PagedResult<PurchaseOrder>(
                Array.Empty<PurchaseOrder>(),
                0,
                1,
                100)));
        _purchaseOrderService.GetActiveSuppliersAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Supplier>>(Array.Empty<Supplier>()));
        _grnService.GetPagedAsync(
                Arg.Any<PagedQuery>(),
                Arg.Any<string?>(),
                Arg.Any<GRNStatus?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PagedResult<GoodsReceivedNote>(
                [new GoodsReceivedNote { Id = 21, GRNNumber = "GRN-21", Status = GRNStatus.Confirmed }],
                70,
                3,
                25)));

        var sut = new GRNViewModel(_grnService, _purchaseOrderService, _dialogService);

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal("GRN-21", sut.SearchQuery);
        Assert.Equal(GRNStatus.Confirmed, sut.FilterStatus);
        Assert.Equal(3, sut.CurrentPage);
        await _grnService.Received(1).GetPagedAsync(
            Arg.Is<PagedQuery>(query => query.Page == 3 && query.PageSize == 25),
            "GRN-21",
            GRNStatus.Confirmed,
            null,
            null,
            Arg.Any<CancellationToken>());
    }
}
