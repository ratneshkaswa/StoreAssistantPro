using NSubstitute;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Categories.Services;
using StoreAssistantPro.Modules.Categories.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public sealed class CategoryManagementInlineEditTests
{
    [Fact]
    public async Task TryInlineRenameTypeAsync_Should_Persist_Trimmed_Name()
    {
        var categoryService = Substitute.For<ICategoryService>();
        var categoryType = new CategoryType { Id = 3, Name = "Menswear" };
        var viewModel = new CategoryManagementViewModel(categoryService)
        {
            SelectedType = categoryType
        };

        var result = await viewModel.TryInlineRenameTypeAsync(categoryType, "  Men's Wear  ", "Menswear");

        Assert.True(result);
        Assert.Equal("Men's Wear", categoryType.Name);
        Assert.Equal("Men's Wear", viewModel.TypeName);
        Assert.Equal("Category type updated.", viewModel.SuccessMessage);
        await categoryService.Received(1).UpdateTypeAsync(categoryType.Id, "Men's Wear", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryInlineRenameCategoryAsync_Should_Persist_Trimmed_Name()
    {
        var categoryService = Substitute.For<ICategoryService>();
        var category = new Category { Id = 5, Name = "Shirt", CategoryTypeId = 9 };
        var viewModel = new CategoryManagementViewModel(categoryService)
        {
            SelectedCategory = category
        };

        var result = await viewModel.TryInlineRenameCategoryAsync(category, "  Shirts  ", "Shirt");

        Assert.True(result);
        Assert.Equal("Shirts", category.Name);
        Assert.Equal("Shirts", viewModel.CategoryName);
        Assert.Equal("Category updated.", viewModel.SuccessMessage);
        await categoryService.Received(1).UpdateAsync(
            category.Id,
            Arg.Is<CategoryDto>(dto => dto.Name == "Shirts" && dto.CategoryTypeId == category.CategoryTypeId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryInlineRenameCategoryAsync_Should_Revert_Blank_Edits()
    {
        var categoryService = Substitute.For<ICategoryService>();
        var category = new Category { Id = 8, Name = "Original", CategoryTypeId = 4 };
        var viewModel = new CategoryManagementViewModel(categoryService);

        var result = await viewModel.TryInlineRenameCategoryAsync(category, "", "Original");

        Assert.False(result);
        Assert.Equal("Original", category.Name);
        Assert.Equal("Category name is required.", viewModel.ErrorMessage);
        await categoryService.DidNotReceive().UpdateAsync(
            Arg.Any<int>(),
            Arg.Any<CategoryDto>(),
            Arg.Any<CancellationToken>());
    }
}
