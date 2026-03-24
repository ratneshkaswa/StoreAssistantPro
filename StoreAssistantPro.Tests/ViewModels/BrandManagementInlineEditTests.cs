using NSubstitute;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Brands.Services;
using StoreAssistantPro.Modules.Brands.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public sealed class BrandManagementInlineEditTests
{
    [Fact]
    public async Task TryInlineRenameAsync_Should_Persist_Trimmed_Name_And_Update_Form_State()
    {
        var brandService = Substitute.For<IBrandService>();
        var brand = new Brand { Id = 7, Name = "Legacy" };
        var viewModel = new BrandManagementViewModel(brandService)
        {
            SelectedBrand = brand
        };

        var result = await viewModel.TryInlineRenameAsync(brand, "  Raymond  ", "Legacy");

        Assert.True(result);
        Assert.Equal("Raymond", brand.Name);
        Assert.Equal("Raymond", viewModel.BrandName);
        Assert.Equal("Brand updated.", viewModel.SuccessMessage);
        await brandService.Received(1).UpdateAsync(brand.Id, "Raymond", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryInlineRenameAsync_Should_Revert_Blank_Edits()
    {
        var brandService = Substitute.For<IBrandService>();
        var brand = new Brand { Id = 11, Name = "Original" };
        var viewModel = new BrandManagementViewModel(brandService);

        var result = await viewModel.TryInlineRenameAsync(brand, "   ", "Original");

        Assert.False(result);
        Assert.Equal("Original", brand.Name);
        Assert.Equal("Brand name is required.", viewModel.ErrorMessage);
        await brandService.DidNotReceive().UpdateAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
