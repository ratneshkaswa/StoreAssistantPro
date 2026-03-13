using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Inventory.Services;
using StoreAssistantPro.Modules.Inventory.ViewModels;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Tests.ViewModels;

public sealed class InventoryViewModelTests
{
    private readonly IInventoryService _inventoryService = Substitute.For<IInventoryService>();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();

    private InventoryViewModel CreateSut()
    {
        _appState.CurrentUserType.Returns(UserType.Admin);
        _regional.FormatCurrency(Arg.Any<decimal>())
            .Returns(call => $"Rs. {call.Arg<decimal>():0.00}");
        return new InventoryViewModel(_inventoryService, _productService, _regional, _appState);
    }

    [Fact]
    public void AdjustCommand_Should_Require_SelectedProduct()
    {
        var sut = CreateSut();

        Assert.False(sut.AdjustCommand.CanExecute(null));

        sut.SelectedProduct = new Product { Id = 11, Name = "Oxford Shirt", Quantity = 8 };

        Assert.True(sut.AdjustCommand.CanExecute(null));
    }

    [Fact]
    public async Task AdjustAsync_ReloadsAlerts_And_ReselectsAdjustedProduct()
    {
        _inventoryService.AdjustStockAsync(Arg.Any<StockAdjustmentDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _inventoryService.GetRecentAdjustmentsAsync(100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StockAdjustment>>(new List<StockAdjustment>()));
        _inventoryService.GetLowStockProductsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Product>>(new List<Product>()));
        _inventoryService.GetOutOfStockProductsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Product>>(new List<Product>()));
        _inventoryService.GetTotalStockValueAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(123m));
        _productService.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Product>>(new List<Product>
            {
                new() { Id = 11, Name = "Oxford Shirt", Quantity = 25, SalePrice = 999m, CostPrice = 500m }
            }));

        var sut = CreateSut();
        sut.SelectedProduct = new Product { Id = 11, Name = "Oxford Shirt", Quantity = 8 };
        sut.NewQuantity = "25";
        sut.AdjustmentNotes = "Cycle count";

        await sut.AdjustCommand.ExecuteAsync(null);

        await _inventoryService.Received(1).GetLowStockProductsAsync(Arg.Any<CancellationToken>());
        await _inventoryService.Received(1).GetOutOfStockProductsAsync(Arg.Any<CancellationToken>());
        await _inventoryService.Received(1).GetTotalStockValueAsync(Arg.Any<CancellationToken>());
        Assert.NotNull(sut.SelectedProduct);
        Assert.Equal(11, sut.SelectedProduct!.Id);
        Assert.Equal("25", sut.NewQuantity);
        Assert.Equal(string.Empty, sut.AdjustmentNotes);
    }
}
