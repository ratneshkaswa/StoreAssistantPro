using NSubstitute;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Products.ViewModels;
using StoreAssistantPro.Modules.Tax.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public sealed class ProductManagementViewModelStateTests : IDisposable
{
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly ITaxGroupService _taxGroupService = Substitute.For<ITaxGroupService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    public ProductManagementViewModelStateTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public async Task LoadCommand_Should_Restore_Persisted_Filters()
    {
        UserPreferencesStore.SetProductManagementState(new ProductManagementViewState
        {
            SearchText = "Oxford",
            FilterCategoryId = 4,
            FilterBrandId = 6,
            FilterStockStatus = "In Stock"
        });

        _regional.CurrencySymbol.Returns("Rs.");
        _productService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Product>>([
                new Product
                {
                    Id = 10,
                    Name = "Oxford Shirt",
                    CategoryId = 4,
                    BrandId = 6,
                    Quantity = 12,
                    MinStockLevel = 5
                }
            ]));
        _productService.GetActiveTaxesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TaxMaster>>(Array.Empty<TaxMaster>()));
        _productService.GetActiveCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Category>>([
                new Category { Id = 4, Name = "Shirts" }
            ]));
        _productService.GetActiveBrandsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Brand>>([
                new Brand { Id = 6, Name = "Acme" }
            ]));
        _productService.GetActiveVendorsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Vendor>>(Array.Empty<Vendor>()));
        _taxGroupService.GetActiveGroupsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TaxGroup>>(Array.Empty<TaxGroup>()));
        _taxGroupService.GetActiveHSNCodesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<HSNCode>>(Array.Empty<HSNCode>()));

        var sut = new ProductManagementViewModel(
            _productService,
            _taxGroupService,
            Substitute.For<INavigationService>(),
            _regional,
            new ProductContextHolder());

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Oxford", sut.SearchText);
        Assert.Equal("In Stock", sut.FilterStockStatus);
        Assert.NotNull(sut.FilterCategory);
        Assert.Equal(4, sut.FilterCategory!.Id);
        Assert.NotNull(sut.FilterBrand);
        Assert.Equal(6, sut.FilterBrand!.Id);
        Assert.Single(sut.Products);
        Assert.Equal("1 of 1", sut.FilterCountText);
    }
}
