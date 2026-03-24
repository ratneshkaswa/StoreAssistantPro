using NSubstitute;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Brands.Services;
using StoreAssistantPro.Modules.Brands.ViewModels;
using StoreAssistantPro.Modules.Categories.Services;
using StoreAssistantPro.Modules.Categories.ViewModels;
using StoreAssistantPro.Modules.Customers.Services;
using StoreAssistantPro.Modules.Customers.ViewModels;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Products.ViewModels;
using StoreAssistantPro.Modules.Tax.Services;
using StoreAssistantPro.Modules.Tax.ViewModels;
using StoreAssistantPro.Modules.Vendors.Services;
using StoreAssistantPro.Modules.Vendors.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public sealed class QuickAccessToggleCommandTests
{
    [Fact]
    public async Task BrandCustomerAndVendorToggleCommands_Should_Use_Clicked_Row_When_No_Item_Is_Selected()
    {
        var brandService = Substitute.For<IBrandService>();
        var customerService = Substitute.For<ICustomerService>();
        var vendorService = Substitute.For<IVendorService>();

        var brand = new Brand { Id = 7, Name = "Acme", IsActive = true };
        var customer = new Customer { Id = 11, Name = "Walk-in", IsActive = true };
        var vendor = new Vendor { Id = 13, Name = "North Supply", IsActive = true };

        brandService.ToggleActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        customerService.ToggleActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        vendorService.ToggleActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        brandService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Brand>>(new List<Brand> { brand }));
        customerService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Customer>>(new List<Customer> { customer }));
        vendorService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Vendor>>(new List<Vendor> { vendor }));

        var ledgerService = Substitute.For<IVendorLedgerService>();
        var regionalService = Substitute.For<IRegionalSettingsService>();
        var appStateService = Substitute.For<IAppStateService>();

        var brandViewModel = new BrandManagementViewModel(brandService);
        var customerViewModel = new CustomerManagementViewModel(customerService);
        var vendorViewModel = new VendorManagementViewModel(vendorService, ledgerService, regionalService, appStateService);

        await brandViewModel.ToggleActiveCommand.ExecuteAsync(brand);
        await customerViewModel.ToggleActiveCommand.ExecuteAsync(customer);
        await vendorViewModel.ToggleActiveCommand.ExecuteAsync(vendor);

        await brandService.Received(1).ToggleActiveAsync(brand.Id, Arg.Any<CancellationToken>());
        await customerService.Received(1).ToggleActiveAsync(customer.Id, Arg.Any<CancellationToken>());
        await vendorService.Received(1).ToggleActiveAsync(vendor.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProductAndVariantToggleCommands_Should_Use_Clicked_Row_When_No_Item_Is_Selected()
    {
        var productService = Substitute.For<IProductService>();
        var taxGroupService = Substitute.For<ITaxGroupService>();
        var variantService = Substitute.For<IProductVariantService>();

        var product = new Product { Id = 17, Name = "Oxford Shirt", IsActive = true };
        var variant = new ProductVariant
        {
            Id = 19,
            ProductId = product.Id,
            SizeId = 1,
            ColourId = 2,
            IsActive = true
        };

        productService.ToggleActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        variantService.ToggleActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        productService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Product>>(new List<Product> { product }));
        productService.GetActiveTaxesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TaxMaster>>(new List<TaxMaster>()));
        productService.GetActiveCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Category>>(new List<Category>()));
        productService.GetActiveBrandsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Brand>>(new List<Brand>()));
        productService.GetActiveVendorsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Vendor>>(new List<Vendor>()));
        var regionalSettings = Substitute.For<IRegionalSettingsService>();
        taxGroupService.GetActiveGroupsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TaxGroup>>(new List<TaxGroup>()));
        taxGroupService.GetActiveHSNCodesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<HSNCode>>(new List<HSNCode>()));

        variantService.GetByProductAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ProductVariant>>(new List<ProductVariant> { variant }));
        productService.GetSizesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ProductSize>>(new List<ProductSize>()));
        productService.GetColoursAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Colour>>(new List<Colour>()));

        var productViewModel = new ProductManagementViewModel(
            productService, taxGroupService,
            Substitute.For<INavigationService>(), regionalSettings, new ProductContextHolder());
        var variantViewModel = new VariantManagementViewModel(
            variantService, productService,
            new ProductContextHolder(), Substitute.For<INavigationService>())
        {
            Product = product
        };

        await productViewModel.ToggleActiveCommand.ExecuteAsync(product);
        await variantViewModel.ToggleActiveCommand.ExecuteAsync(variant);

        await productService.Received(1).ToggleActiveAsync(product.Id, Arg.Any<CancellationToken>());
        await variantService.Received(1).ToggleActiveAsync(variant.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CategoryToggleCommands_Should_Use_Clicked_Row_When_No_Item_Is_Selected()
    {
        var categoryService = Substitute.For<ICategoryService>();
        var categoryType = new CategoryType { Id = 23, Name = "Men's Wear", IsActive = true };
        var category = new Category { Id = 29, Name = "Shirts", IsActive = true, CategoryTypeId = categoryType.Id };

        categoryService.ToggleTypeActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        categoryService.ToggleActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        categoryService.GetAllTypesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CategoryType>>(new List<CategoryType> { categoryType }));
        categoryService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Category>>(new List<Category> { category }));

        var viewModel = new CategoryManagementViewModel(categoryService);

        await viewModel.ToggleTypeActiveCommand.ExecuteAsync(categoryType);
        await viewModel.ToggleCategoryActiveCommand.ExecuteAsync(category);

        await categoryService.Received(1).ToggleTypeActiveAsync(categoryType.Id, Arg.Any<CancellationToken>());
        await categoryService.Received(1).ToggleActiveAsync(category.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TaxToggleCommands_Should_Use_Clicked_Row_When_No_Item_Is_Selected()
    {
        var taxService = Substitute.For<ITaxService>();
        var taxGroupService = Substitute.For<ITaxGroupService>();
        var regionalSettings = Substitute.For<IRegionalSettingsService>();
        var group = new TaxGroup { Id = 31, Name = "GST Garments", IsActive = true };
        var hsnCode = new HSNCode { Id = 37, Code = "6109", Description = "T-shirts", IsActive = true };

        taxGroupService.ToggleGroupActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        taxGroupService.ToggleHSNActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        taxGroupService.GetAllGroupsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TaxGroup>>(new List<TaxGroup> { group }));
        taxGroupService.GetAllHSNCodesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<HSNCode>>(new List<HSNCode> { hsnCode }));

        var viewModel = new TaxManagementViewModel(taxService, taxGroupService, regionalSettings);

        await viewModel.ToggleGroupActiveCommand.ExecuteAsync(group);
        await viewModel.ToggleHSNActiveCommand.ExecuteAsync(hsnCode);

        await taxGroupService.Received(1).ToggleGroupActiveAsync(group.Id, Arg.Any<CancellationToken>());
        await taxGroupService.Received(1).ToggleHSNActiveAsync(hsnCode.Id, Arg.Any<CancellationToken>());
    }
}
