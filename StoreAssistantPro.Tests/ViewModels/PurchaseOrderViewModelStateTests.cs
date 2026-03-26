using NSubstitute;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.PurchaseOrders.Services;
using StoreAssistantPro.Modules.PurchaseOrders.ViewModels;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public sealed class PurchaseOrderViewModelStateTests : IDisposable
{
    private readonly IPurchaseOrderService _purchaseOrderService = Substitute.For<IPurchaseOrderService>();
    private readonly IProductService _productService = Substitute.For<IProductService>();

    public PurchaseOrderViewModelStateTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public async Task LoadCommand_Should_Restore_Search_Status_And_CurrentPage()
    {
        UserPreferencesStore.SetPurchaseOrdersState(new PagedSearchFilterViewState
        {
            SearchText = "PO-17",
            ActiveFilter = "Ordered",
            CurrentPage = 2
        });

        _purchaseOrderService.GetActiveSuppliersAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Supplier>>(Array.Empty<Supplier>()));
        _productService.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>()));
        _purchaseOrderService.GetPagedAsync(
                Arg.Any<PagedQuery>(),
                Arg.Any<string?>(),
                Arg.Any<PurchaseOrderStatus?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PagedResult<PurchaseOrder>(
                [new PurchaseOrder { Id = 17, OrderNumber = "PO-17", Status = PurchaseOrderStatus.Ordered }],
                30,
                2,
                25)));

        var sut = new PurchaseOrderViewModel(_purchaseOrderService, _productService);

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal("PO-17", sut.SearchQuery);
        Assert.Equal(PurchaseOrderStatus.Ordered, sut.FilterStatus);
        Assert.Equal(2, sut.CurrentPage);
        await _purchaseOrderService.Received(1).GetPagedAsync(
            Arg.Is<PagedQuery>(query => query.Page == 2 && query.PageSize == 25),
            "PO-17",
            PurchaseOrderStatus.Ordered,
            null,
            null,
            Arg.Any<CancellationToken>());
    }
}
