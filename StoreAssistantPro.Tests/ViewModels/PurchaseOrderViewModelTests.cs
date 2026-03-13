using NSubstitute;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.PurchaseOrders.Services;
using StoreAssistantPro.Modules.PurchaseOrders.ViewModels;
using System.Collections.ObjectModel;

namespace StoreAssistantPro.Tests.ViewModels;

public class PurchaseOrderViewModelTests
{
    private readonly IPurchaseOrderService _purchaseOrderService = Substitute.For<IPurchaseOrderService>();
    private readonly IProductService _productService = Substitute.For<IProductService>();

    private PurchaseOrderViewModel CreateSut() => new(_purchaseOrderService, _productService);

    [Fact]
    public async Task Load_SeedsBlankLineAndLoadsProducts()
    {
        _purchaseOrderService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<PurchaseOrder> { new() { Id = 1, OrderNumber = "PO-1" } });
        _purchaseOrderService.GetActiveSuppliersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Supplier> { new() { Id = 7, Name = "Jaipur Textiles" } });
        _productService.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Product> { new() { Id = 11, Name = "Cotton Shirt" } });

        var sut = CreateSut();

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Single(sut.Products);
        Assert.Single(sut.Suppliers);
        Assert.Single(sut.LineItems);
        Assert.NotNull(sut.SelectedLineItem);
    }

    [Fact]
    public async Task CreateOrder_IncompleteLine_BlocksSave()
    {
        var sut = CreateSut();
        sut.SelectedSupplier = new Supplier { Id = 7, Name = "Jaipur Textiles" };
        sut.LineItems = new ObservableCollection<PurchaseOrderLineInput>
        {
            new() { ProductId = 0, Quantity = 3, UnitCost = 120m }
        };

        await sut.CreateOrderCommand.ExecuteAsync(null);

        Assert.Equal("Complete line 1 with a product, quantity, and unit cost.", sut.ErrorMessage);
        await _purchaseOrderService.DidNotReceive().CreateAsync(Arg.Any<CreatePurchaseOrderDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOrder_ValidLine_CreatesOrderAndResetsForm()
    {
        _purchaseOrderService.CreateAsync(Arg.Any<CreatePurchaseOrderDto>(), Arg.Any<CancellationToken>())
            .Returns(new PurchaseOrder { Id = 1, OrderNumber = "PO-20260313-0001" });
        _purchaseOrderService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<PurchaseOrder>());

        var sut = CreateSut();
        sut.SelectedSupplier = new Supplier { Id = 7, Name = "Jaipur Textiles" };
        sut.Notes = "Urgent";
        sut.LineItems = new ObservableCollection<PurchaseOrderLineInput>
        {
            new() { ProductId = 11, Quantity = 3, UnitCost = 120m }
        };

        await sut.CreateOrderCommand.ExecuteAsync(null);

        Assert.Equal("PO PO-20260313-0001 created.", sut.SuccessMessage);
        Assert.Null(sut.SelectedSupplier);
        Assert.Empty(sut.Notes);
        Assert.Single(sut.LineItems);
        Assert.Equal(0, sut.LineItems[0].ProductId);
        Assert.Equal(0, sut.LineItems[0].Quantity);
        Assert.Equal(0m, sut.LineItems[0].UnitCost);
    }

    [Fact]
    public void RemoveLineAndOrderCommands_Should_Track_Real_Action_State()
    {
        var sut = CreateSut();

        sut.LineItems = new ObservableCollection<PurchaseOrderLineInput> { new() };
        sut.SelectedLineItem = sut.LineItems.Single();

        Assert.False(sut.RemoveLineItemCommand.CanExecute(null));

        sut.LineItems[0].ProductId = 11;
        sut.SelectedLineItem = sut.LineItems[0];

        Assert.True(sut.RemoveLineItemCommand.CanExecute(null));

        sut.SelectedOrder = new PurchaseOrder
        {
            Id = 5,
            OrderNumber = "PO-5",
            Status = PurchaseOrderStatus.Draft,
            Items = new List<PurchaseOrderItem>
            {
                new() { Id = 1, Quantity = 4, QuantityReceived = 0, UnitCost = 100m }
            }
        };

        Assert.True(sut.MarkOrderedCommand.CanExecute(null));
        Assert.False(sut.ReceiveAllCommand.CanExecute(null));
        Assert.True(sut.CancelOrderCommand.CanExecute(null));

        sut.SelectedOrder = new PurchaseOrder
        {
            Id = 6,
            OrderNumber = "PO-6",
            Status = PurchaseOrderStatus.Ordered,
            Items = new List<PurchaseOrderItem>
            {
                new() { Id = 2, Quantity = 4, QuantityReceived = 2, UnitCost = 100m }
            }
        };

        Assert.False(sut.MarkOrderedCommand.CanExecute(null));
        Assert.True(sut.ReceiveAllCommand.CanExecute(null));
        Assert.True(sut.CancelOrderCommand.CanExecute(null));

        sut.SelectedOrder = new PurchaseOrder
        {
            Id = 7,
            OrderNumber = "PO-7",
            Status = PurchaseOrderStatus.Received,
            Items = new List<PurchaseOrderItem>
            {
                new() { Id = 3, Quantity = 4, QuantityReceived = 4, UnitCost = 100m }
            }
        };

        Assert.False(sut.MarkOrderedCommand.CanExecute(null));
        Assert.False(sut.ReceiveAllCommand.CanExecute(null));
        Assert.False(sut.CancelOrderCommand.CanExecute(null));
    }
}
