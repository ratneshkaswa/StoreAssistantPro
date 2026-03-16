using NSubstitute;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Products.ViewModels;
using StoreAssistantPro.Modules.Tax.Services;
using StoreAssistantPro.Modules.Tax.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public sealed class QuickAccessActionStateTests
{
    [Fact]
    public void ProductManageVariantsCommand_Should_Require_A_Selected_Product()
    {
        var productService = Substitute.For<IProductService>();
        var taxGroupService = Substitute.For<ITaxGroupService>();
        var viewModel = new ProductManagementViewModel(
            productService, taxGroupService,
            Substitute.For<INavigationService>(), new ProductContextHolder());

        Assert.False(viewModel.ManageVariantsCommand.CanExecute(null));

        viewModel.SelectedProduct = new Product { Id = 5, Name = "Oxford Shirt" };

        Assert.True(viewModel.ManageVariantsCommand.CanExecute(null));

        viewModel.SelectedProduct = null;

        Assert.False(viewModel.ManageVariantsCommand.CanExecute(null));
    }

    [Fact]
    public void VariantDeleteCommand_Should_Require_A_Selected_Variant()
    {
        var variantService = Substitute.For<IProductVariantService>();
        var productService = Substitute.For<IProductService>();
        var viewModel = new VariantManagementViewModel(
            variantService, productService,
            new ProductContextHolder(), Substitute.For<INavigationService>());

        Assert.False(viewModel.DeleteCommand.CanExecute(null));

        viewModel.SelectedVariant = new ProductVariant { Id = 9, SizeId = 1, ColourId = 2 };

        Assert.True(viewModel.DeleteCommand.CanExecute(null));

        viewModel.SelectedVariant = null;

        Assert.False(viewModel.DeleteCommand.CanExecute(null));
    }

    [Fact]
    public void TaxCommands_Should_Track_Selection_State_And_Clear_Stale_Slab_Selection()
    {
        var taxService = Substitute.For<ITaxService>();
        var taxGroupService = Substitute.For<ITaxGroupService>();
        var regionalSettings = Substitute.For<IRegionalSettingsService>();
        var viewModel = new TaxManagementViewModel(taxService, taxGroupService, regionalSettings);

        var tax = new TaxMaster { Id = 3, TaxName = "GST 5%" };
        var firstSlab = new TaxSlab { Id = 11, GSTPercent = 5, PriceFrom = 0, PriceTo = 1000 };
        var secondSlab = new TaxSlab { Id = 12, GSTPercent = 12, PriceFrom = 1001, PriceTo = TaxSlab.MaxPrice };
        var firstGroup = new TaxGroup { Id = 4, Name = "GST Garments", Slabs = new List<TaxSlab> { firstSlab } };
        var secondGroup = new TaxGroup { Id = 5, Name = "GST Premium", Slabs = new List<TaxSlab> { secondSlab } };

        Assert.False(viewModel.DeleteRateCommand.CanExecute(null));
        Assert.False(viewModel.AddSlabCommand.CanExecute(null));
        Assert.False(viewModel.DeleteSlabCommand.CanExecute(null));

        viewModel.SelectedTax = tax;
        Assert.True(viewModel.DeleteRateCommand.CanExecute(null));

        viewModel.SelectedGroup = firstGroup;
        Assert.True(viewModel.AddSlabCommand.CanExecute(null));
        Assert.False(viewModel.DeleteSlabCommand.CanExecute(null));
        Assert.Single(viewModel.GroupSlabs);

        viewModel.SelectedSlab = firstSlab;
        Assert.True(viewModel.DeleteSlabCommand.CanExecute(null));

        viewModel.SelectedGroup = secondGroup;

        Assert.Null(viewModel.SelectedSlab);
        Assert.False(viewModel.DeleteSlabCommand.CanExecute(null));
        Assert.Single(viewModel.GroupSlabs);
        Assert.Equal(secondSlab.Id, viewModel.GroupSlabs.Single().Id);
        Assert.Equal(string.Empty, viewModel.SlabGST);
        Assert.Equal(string.Empty, viewModel.SlabPriceFrom);
        Assert.Equal(string.Empty, viewModel.SlabPriceTo);
    }
}
