using NSubstitute;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Products.ViewModels;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Tests.ViewModels;

public sealed class ProductManagementViewModelTests
{
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly ITaxGroupService _taxGroupService = Substitute.For<ITaxGroupService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    private ProductManagementViewModel CreateSut()
    {
        _regional.CurrencySymbol.Returns("Rs.");

        _productService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>()));
        _productService.GetActiveTaxesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TaxMaster>>(Array.Empty<TaxMaster>()));
        _productService.GetActiveCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Category>>(Array.Empty<Category>()));
        _productService.GetActiveBrandsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Brand>>(Array.Empty<Brand>()));
        _productService.GetActiveVendorsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Vendor>>(Array.Empty<Vendor>()));
        _productService.CreateAsync(Arg.Any<ProductDto>(), Arg.Any<CancellationToken>())
            .Returns(21);
        _productService.UpdateAsync(Arg.Any<int>(), Arg.Any<ProductDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _productService.ToggleActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _taxGroupService.GetActiveGroupsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TaxGroup>>(Array.Empty<TaxGroup>()));
        _taxGroupService.GetActiveHSNCodesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<HSNCode>>(Array.Empty<HSNCode>()));
        _taxGroupService.GetMappingByProductAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProductTaxMapping?>(null));
        _taxGroupService.SetProductMappingAsync(Arg.Any<ProductTaxMappingDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _taxGroupService.RemoveProductMappingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return new ProductManagementViewModel(
            _productService,
            _taxGroupService,
            Substitute.For<INavigationService>(),
            _regional,
            new ProductContextHolder());
    }

    [Fact]
    public void ClearingSelection_ResetsProductFormAndMappingState()
    {
        var sut = CreateSut();
        sut.Taxes = [new TaxMaster { Id = 3, TaxName = "GST 5%" }];
        sut.Categories = [new Category { Id = 4, Name = "Shirts" }];
        sut.Brands = [new Brand { Id = 5, Name = "Acme" }];
        sut.Vendors = [new Vendor { Id = 6, Name = "Jaipur Textiles" }];
        sut.SelectedTaxGroup = new TaxGroup { Id = 11, Name = "Readywear" };
        sut.SelectedHSNCode = new HSNCode { Id = 12, Code = "6109", Description = "T-shirts" };
        sut.OverrideAllowed = true;

        sut.SelectedProduct = new Product
        {
            Id = 10,
            Name = "Oxford Shirt",
            ProductType = ProductType.Readymade,
            Unit = ProductUnit.Piece,
            TaxId = 3,
            CategoryId = 4,
            BrandId = 5,
            VendorId = 6,
            SupportsColour = true,
            SupportsSize = true,
            SupportsPattern = true,
            SupportsType = true
        };

        sut.SelectedProduct = null;

        Assert.False(sut.IsEditing);
        Assert.Empty(sut.ProductName);
        Assert.Null(sut.SelectedTaxGroup);
        Assert.Null(sut.SelectedHSNCode);
        Assert.False(sut.OverrideAllowed);
    }

    [Fact]
    public async Task LoadMappingForProductAsync_IgnoresStaleSelection()
    {
        var sut = CreateSut();
        var currentGroup = new TaxGroup { Id = 21, Name = "Current" };
        var staleGroup = new TaxGroup { Id = 22, Name = "Stale" };
        var currentCode = new HSNCode { Id = 31, Code = "6205", Description = "Shirts" };
        var staleCode = new HSNCode { Id = 32, Code = "6109", Description = "T-shirts" };

        _taxGroupService.GetMappingByProductAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProductTaxMapping?>(new ProductTaxMapping
            {
                ProductId = 1,
                TaxGroupId = staleGroup.Id,
                HSNCodeId = staleCode.Id,
                OverrideAllowed = true
            }));

        sut.TaxGroups = [currentGroup, staleGroup];
        sut.HSNCodes = [currentCode, staleCode];
        sut.SelectedProduct = new Product { Id = 2, Name = "Oxford Shirt" };
        sut.SelectedTaxGroup = currentGroup;
        sut.SelectedHSNCode = currentCode;
        sut.OverrideAllowed = false;

        await sut.LoadMappingForProductCommand.ExecuteAsync(1);

        Assert.Same(currentGroup, sut.SelectedTaxGroup);
        Assert.Same(currentCode, sut.SelectedHSNCode);
        Assert.False(sut.OverrideAllowed);
    }

    [Fact]
    public async Task SaveAsync_PreservesSuccessMessage_WhenFormResets()
    {
        var sut = CreateSut();
        sut.ProductName = "Oxford Shirt";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Product created.", sut.SuccessMessage);
        Assert.False(sut.IsEditing);
        Assert.Empty(sut.ProductName);
    }

    [Fact]
    public async Task ToggleActiveAsync_ReselectsProductAfterReload()
    {
        var sut = CreateSut();
        _productService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Product>>(new[]
            {
                new Product { Id = 10, Name = "Oxford Shirt", IsActive = false }
            }));

        sut.SelectedProduct = new Product { Id = 10, Name = "Oxford Shirt", IsActive = true };

        await sut.ToggleActiveCommand.ExecuteAsync(sut.SelectedProduct);

        Assert.NotNull(sut.SelectedProduct);
        Assert.Equal(10, sut.SelectedProduct!.Id);
        Assert.Equal("Status toggled.", sut.SuccessMessage);
    }

    [Fact]
    public void CurrencySymbol_UsesRegionalSettings()
    {
        var sut = CreateSut();

        Assert.Equal("Rs.", sut.CurrencySymbol);
    }
}
