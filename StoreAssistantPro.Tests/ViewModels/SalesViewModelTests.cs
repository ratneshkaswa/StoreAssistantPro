using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Sales.Commands;
using StoreAssistantPro.Modules.Sales.Services;
using StoreAssistantPro.Modules.Sales.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class SalesViewModelTests
{
    private readonly ISalesService _salesService = Substitute.For<ISalesService>();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    public SalesViewModelTests()
    {
        _sessionService.CurrentUserType.Returns(UserType.Admin);
        _commandBus.SendAsync(Arg.Any<CompleteSaleCommand>())
            .Returns(CommandResult.Success());
        _regional.Now.Returns(new DateTime(2026, 1, 15, 10, 30, 0));
    }

    private SalesViewModel CreateSut() =>
        new(_salesService, _productService, _sessionService, _commandBus, _regional);

    [Fact]
    public async Task LoadSales_PopulatesSalesList()
    {
        var sales = new List<Sale>
        {
            new() { Id = 1, TotalAmount = 50m, PaymentMethod = "Cash", Items = [] },
            new() { Id = 2, TotalAmount = 75m, PaymentMethod = "Card", Items = [] }
        };
        _salesService.GetAllAsync().Returns(sales);

        var sut = CreateSut();
        await sut.LoadSalesCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Sales.Count);
    }

    [Fact]
    public async Task FilterByDate_CallsServiceWithDateRange()
    {
        _salesService.GetSalesByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Enumerable.Empty<Sale>());

        var sut = CreateSut();
        sut.FilterFrom = new DateTime(2026, 1, 1);
        sut.FilterTo = new DateTime(2026, 1, 31);

        await sut.FilterByDateCommand.ExecuteAsync(null);

        await _salesService.Received(1).GetSalesByDateRangeAsync(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 2, 1));
    }

    [Fact]
    public void AddToCart_AddsItemFromSelectedProduct()
    {
        var product = new Product { Id = 1, Name = "Widget", Price = 10m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 3;

        sut.AddToCartCommand.Execute(null);

        Assert.Single(sut.CartItems);
        Assert.Equal(1, sut.CartItems[0].ProductId);
        Assert.Equal(3, sut.CartItems[0].Quantity);
        Assert.Equal(10m, sut.CartItems[0].UnitPrice);
        Assert.Equal(30m, sut.CartTotal);
    }

    [Fact]
    public void AddToCart_WithNoSelection_DoesNothing()
    {
        var sut = CreateSut();
        sut.SelectedProduct = null;
        sut.CartQuantity = 1;

        sut.AddToCartCommand.Execute(null);

        Assert.Empty(sut.CartItems);
    }

    [Fact]
    public void AddToCart_ExceedingStock_SetsErrorMessage()
    {
        var product = new Product { Id = 1, Name = "Limited", Price = 10m, Quantity = 2 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 5;

        sut.AddToCartCommand.Execute(null);

        Assert.Contains("Only 2 available", sut.ErrorMessage);
        Assert.Empty(sut.CartItems);
    }

    [Fact]
    public void AddToCart_SameProductTwice_IncrementsQuantity()
    {
        var product = new Product { Id = 1, Name = "Widget", Price = 10m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 2;
        sut.AddToCartCommand.Execute(null);

        sut.SelectedProduct = product;
        sut.CartQuantity = 3;
        sut.AddToCartCommand.Execute(null);

        Assert.Single(sut.CartItems);
        Assert.Equal(5, sut.CartItems[0].Quantity);
        Assert.Equal(50m, sut.CartTotal);
    }

    [Fact]
    public void RemoveFromCart_RemovesItemAndUpdatesTotal()
    {
        var product = new Product { Id = 1, Name = "Widget", Price = 10m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 2;
        sut.AddToCartCommand.Execute(null);

        var cartItem = sut.CartItems[0];
        sut.RemoveFromCartCommand.Execute(cartItem);

        Assert.Empty(sut.CartItems);
        Assert.Equal(0m, sut.CartTotal);
    }

    [Fact]
    public async Task CompleteSale_WithEmptyCart_DoesNotCallService()
    {
        var sut = CreateSut();
        await sut.CompleteSaleCommand.ExecuteAsync(null);

        await _commandBus.DidNotReceive().SendAsync(Arg.Any<CompleteSaleCommand>());
    }

    [Fact]
    public async Task CompleteSale_CreatesAndReloads()
    {
        _salesService.GetAllAsync().Returns(Enumerable.Empty<Sale>());
        var product = new Product { Id = 1, Name = "Widget", Price = 10m, Quantity = 50 };

        var sut = CreateSut();
        sut.SelectedProduct = product;
        sut.CartQuantity = 2;
        sut.PaymentMethod = "Card";
        sut.AddToCartCommand.Execute(null);

        await sut.CompleteSaleCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<CompleteSaleCommand>(c =>
            c.TotalAmount == 20m &&
            c.PaymentMethod == "Card" &&
            c.Items.Count == 1));
        Assert.False(sut.IsNewSaleVisible);
    }

    [Fact]
    public async Task ShowNewSale_LoadsAvailableProductsAndResetsForm()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "InStock", Price = 10m, Quantity = 5 },
            new() { Id = 2, Name = "OutOfStock", Price = 20m, Quantity = 0 }
        };
        _productService.GetAllAsync().Returns(products);

        var sut = CreateSut();
        await sut.ShowNewSaleCommand.ExecuteAsync(null);

        Assert.True(sut.IsNewSaleVisible);
        Assert.Single(sut.AvailableProducts);
        Assert.Equal("InStock", sut.AvailableProducts[0].Name);
        Assert.Empty(sut.CartItems);
        Assert.Equal(0m, sut.CartTotal);
        Assert.Equal("Cash", sut.PaymentMethod);
    }

    [Fact]
    public void CancelNewSale_HidesForm()
    {
        var sut = CreateSut();
        sut.IsNewSaleVisible = true;

        sut.CancelNewSaleCommand.Execute(null);

        Assert.False(sut.IsNewSaleVisible);
    }

    // ── Role-based access tests ──

    [Fact]
    public async Task ShowNewSale_AsUser_SetsErrorMessage()
    {
        _sessionService.CurrentUserType.Returns(UserType.User);
        var sut = CreateSut();

        await sut.ShowNewSaleCommand.ExecuteAsync(null);

        Assert.False(sut.IsNewSaleVisible);
        Assert.Contains("administrators and managers", sut.ErrorMessage);
    }

    [Fact]
    public void CanCreateSales_Manager_ReturnsTrue()
    {
        _sessionService.CurrentUserType.Returns(UserType.Manager);
        var sut = CreateSut();
        Assert.True(sut.CanCreateSales);
    }

    [Fact]
    public void CanCreateSales_User_ReturnsFalse()
    {
        _sessionService.CurrentUserType.Returns(UserType.User);
        var sut = CreateSut();
        Assert.False(sut.CanCreateSales);
    }

    // ── Sale detail tests ──

    [Fact]
    public void HasSelectedSale_WithSelection_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.SelectedSale = new Sale { Id = 1, TotalAmount = 50m, PaymentMethod = "Cash", Items = [] };
        Assert.True(sut.HasSelectedSale);
    }

    [Fact]
    public void HasSelectedSale_WithoutSelection_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.SelectedSale = null;
        Assert.False(sut.HasSelectedSale);
    }
}
