using NSubstitute;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Data;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Commands;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Products.ViewModels;
using StoreAssistantPro.Modules.Tax.Services;
using StoreAssistantPro.Modules.Brands.Services;

namespace StoreAssistantPro.Tests.ViewModels;

public class ProductsViewModelTests
{
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly ITaxService _taxService = Substitute.For<ITaxService>();
    private readonly IBrandService _brandService = Substitute.For<IBrandService>();
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IMasterPinValidator _masterPinValidator = Substitute.For<IMasterPinValidator>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly ICommandBus _commandBus = Substitute.For<ICommandBus>();

    public ProductsViewModelTests()
    {
        _sessionService.CurrentUserType.Returns(UserType.Admin);
        _dialogService.Confirm(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _masterPinValidator.ValidateAsync(Arg.Any<string>()).Returns(true);
        _commandBus.SendAsync(Arg.Any<SaveProductCommand>())
            .Returns(CommandResult.Success());
        _commandBus.SendAsync(Arg.Any<UpdateProductCommand>())
            .Returns(CommandResult.Success());
        _commandBus.SendAsync(Arg.Any<DeleteProductCommand>())
            .Returns(CommandResult.Success());
        _taxService.GetAllProfilesAsync()
            .Returns(new List<TaxProfile>());
        _brandService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Brand>());
    }

    private ProductsViewModel CreateSut() =>
        new(_productService, _taxService, _brandService, _sessionService, _dialogService, _masterPinValidator, _notificationService, _commandBus);

    private void SetupPagedReturn(IReadOnlyList<Product> items, int totalCount = -1)
    {
        var count = totalCount < 0 ? items.Count : totalCount;
        _productService.GetPagedAsync(Arg.Any<PagedQuery>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var query = ci.Arg<PagedQuery>();
                return new PagedResult<Product>(items, count, query.PageIndex, query.PageSize);
            });
    }

    [Fact]
    public async Task LoadProducts_PopulatesProductsList()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Widget", SalePrice = 9.99m, Quantity = 10 },
            new() { Id = 2, Name = "Gadget", SalePrice = 19.99m, Quantity = 5 }
        };
        SetupPagedReturn(products);

        var sut = CreateSut();
        await sut.LoadProductsCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Products.Count);
    }

    [Fact]
    public async Task LoadProducts_UpdatesPagingState()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Widget", SalePrice = 9.99m, Quantity = 10 }
        };
        SetupPagedReturn(products, totalCount: 75);

        var sut = CreateSut();
        await sut.LoadProductsCommand.ExecuteAsync(null);

        Assert.Equal(75, sut.TotalCount);
        Assert.Equal(2, sut.TotalPages);
        Assert.Equal(0, sut.PageIndex);
        Assert.True(sut.HasNextPage);
        Assert.False(sut.HasPreviousPage);
    }

    [Fact]
    public async Task LoadProducts_WithSearchText_PassesSearchToService()
    {
        SetupPagedReturn([]);

        var sut = CreateSut();
        sut.SearchText = "Red";
        await sut.LoadProductsCommand.ExecuteAsync(null);

        await _productService.Received().GetPagedAsync(
            Arg.Is<PagedQuery>(q => q.SearchTerm == "Red" && q.PageIndex == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchText_ResetsPageIndex()
    {
        SetupPagedReturn([]);

        var sut = CreateSut();
        sut.PageIndex = 3;
        sut.SearchText = "Apple";

        Assert.Equal(0, sut.PageIndex);
    }

    [Fact]
    public async Task SaveProduct_CallsCommandBus()
    {
        SetupPagedReturn([]);

        var sut = CreateSut();
        sut.NewProductName = "New Item";
        sut.NewProductSalePrice = 15.50m;
        sut.NewProductQuantity = 25;

        await sut.SaveProductCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<SaveProductCommand>(c =>
            c.Name == "New Item" && c.SalePrice == 15.50m && c.Quantity == 25));
    }

    [Fact]
    public async Task SaveProduct_WithEmptyName_DoesNotCallService()
    {
        var sut = CreateSut();
        sut.NewProductName = "   ";
        sut.NewProductSalePrice = 10m;
        sut.NewProductQuantity = 1;

        await sut.SaveProductCommand.ExecuteAsync(null);

        await _commandBus.DidNotReceive().SendAsync(Arg.Any<SaveProductCommand>());
    }

    [Fact]
    public async Task SaveProduct_NegativePrice_SetsErrorMessage()
    {
        var sut = CreateSut();
        sut.NewProductName = "Test";
        sut.NewProductSalePrice = -5m;
        sut.NewProductQuantity = 1;

        await sut.SaveProductCommand.ExecuteAsync(null);

        Assert.Equal("Sale price cannot be negative.", sut.ErrorMessage);
        await _commandBus.DidNotReceive().SendAsync(Arg.Any<SaveProductCommand>());
    }

    [Fact]
    public async Task DeleteProduct_ReloadsCurrentPage()
    {
        var product = new Product { Id = 1, Name = "ToDelete", SalePrice = 5m, Quantity = 1, RowVersion = [1, 2, 3] };
        SetupPagedReturn([product]);

        var sut = CreateSut();
        await sut.LoadProductsCommand.ExecuteAsync(null);

        sut.SelectedProduct = product;

        // After delete, reload returns empty page
        SetupPagedReturn([]);
        await sut.DeleteProductCommand.ExecuteAsync(null);

        await _commandBus.Received(1).SendAsync(Arg.Is<DeleteProductCommand>(c =>
            c.ProductId == 1));
        Assert.Null(sut.SelectedProduct);
    }

    [Fact]
    public async Task ShowAddForm_TogglesVisibility()
    {
        var sut = CreateSut();
        Assert.False(sut.IsAddFormVisible);

        await sut.ShowAddFormCommand.ExecuteAsync(null);
        Assert.True(sut.IsAddFormVisible);

        sut.CancelAddCommand.Execute(null);
        Assert.False(sut.IsAddFormVisible);
    }

    [Fact]
    public async Task ShowEditForm_WithoutSelection_DoesNotOpen()
    {
        var sut = CreateSut();
        sut.SelectedProduct = null;

        await sut.ShowEditFormCommand.ExecuteAsync(null);

        Assert.False(sut.IsEditFormVisible);
    }

    [Fact]
    public async Task ShowEditForm_PopulatesEditFields()
    {
        var sut = CreateSut();
        sut.SelectedProduct = new Product { Id = 1, Name = "Test", SalePrice = 9.99m, Quantity = 42 };

        await sut.ShowEditFormCommand.ExecuteAsync(null);

        Assert.True(sut.IsEditFormVisible);
        Assert.Equal("Test", sut.EditProductName);
        Assert.Equal(9.99m, sut.EditProductSalePrice);
        Assert.Equal(42, sut.EditProductQuantity);
    }

    // ── Role-based access tests ──

    [Fact]
    public async Task ShowAddForm_AsUser_SetsErrorMessage()
    {
        _sessionService.CurrentUserType.Returns(UserType.User);
        var sut = CreateSut();

        await sut.ShowAddFormCommand.ExecuteAsync(null);

        Assert.False(sut.IsAddFormVisible);
        Assert.Contains("administrators and managers", sut.ErrorMessage);
    }

    [Fact]
    public async Task ShowEditForm_AsUser_SetsErrorMessage()
    {
        _sessionService.CurrentUserType.Returns(UserType.User);
        var sut = CreateSut();
        sut.SelectedProduct = new Product { Id = 1, Name = "Test", SalePrice = 5m, Quantity = 1 };

        await sut.ShowEditFormCommand.ExecuteAsync(null);

        Assert.False(sut.IsEditFormVisible);
        Assert.Contains("administrators and managers", sut.ErrorMessage);
    }

    [Fact]
    public async Task DeleteProduct_AsManager_SetsErrorMessage()
    {
        _sessionService.CurrentUserType.Returns(UserType.Manager);
        var sut = CreateSut();
        sut.SelectedProduct = new Product { Id = 1, Name = "Test", SalePrice = 5m, Quantity = 1 };

        await sut.DeleteProductCommand.ExecuteAsync(null);

        Assert.Contains("administrators", sut.ErrorMessage);
        await _commandBus.DidNotReceive().SendAsync(Arg.Any<DeleteProductCommand>());
    }

    [Fact]
    public void CanManageProducts_Admin_ReturnsTrue()
    {
        _sessionService.CurrentUserType.Returns(UserType.Admin);
        var sut = CreateSut();
        Assert.True(sut.CanManageProducts);
    }

    [Fact]
    public void CanManageProducts_User_ReturnsFalse()
    {
        _sessionService.CurrentUserType.Returns(UserType.User);
        var sut = CreateSut();
        Assert.False(sut.CanManageProducts);
    }

    // ── Confirm delete tests ──

    [Fact]
    public async Task DeleteProduct_Cancelled_DoesNotDelete()
    {
        _dialogService.Confirm(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var product = new Product { Id = 1, Name = "Keep", SalePrice = 5m, Quantity = 1, RowVersion = [1] };
        SetupPagedReturn([product]);

        var sut = CreateSut();
        await sut.LoadProductsCommand.ExecuteAsync(null);
        sut.SelectedProduct = product;

        await sut.DeleteProductCommand.ExecuteAsync(null);

        await _commandBus.DidNotReceive().SendAsync(Arg.Any<DeleteProductCommand>());
        Assert.Single(sut.Products);
    }

    [Fact]
    public async Task DeleteProduct_MasterPinFailed_DoesNotDelete()
    {
        _masterPinValidator.ValidateAsync(Arg.Any<string>()).Returns(false);
        var product = new Product { Id = 1, Name = "Keep", SalePrice = 5m, Quantity = 1, RowVersion = [1] };
        SetupPagedReturn([product]);

        var sut = CreateSut();
        await sut.LoadProductsCommand.ExecuteAsync(null);
        sut.SelectedProduct = product;

        await sut.DeleteProductCommand.ExecuteAsync(null);

        await _commandBus.DidNotReceive().SendAsync(Arg.Any<DeleteProductCommand>());
        Assert.Contains("Master PIN validation failed", sut.ErrorMessage);
        Assert.Single(sut.Products);
    }

    [Fact]
    public async Task DeleteProduct_Success_CallsMasterPinValidator()
    {
        var product = new Product { Id = 1, Name = "ToDelete", SalePrice = 5m, Quantity = 1, RowVersion = [1] };
        SetupPagedReturn([product]);

        var sut = CreateSut();
        await sut.LoadProductsCommand.ExecuteAsync(null);
        sut.SelectedProduct = product;

        await sut.DeleteProductCommand.ExecuteAsync(null);

        await _masterPinValidator.Received(1).ValidateAsync(Arg.Any<string>());
        await _commandBus.Received(1).SendAsync(Arg.Any<DeleteProductCommand>());
    }
}
