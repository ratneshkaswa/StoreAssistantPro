using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Categories.Services;

namespace StoreAssistantPro.Modules.Categories.ViewModels;

public partial class CategoryManagementViewModel(ICategoryService categoryService) : BaseViewModel
{
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Tab Navigation (Types / Categories)
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTabTypes))]
    [NotifyPropertyChangedFor(nameof(IsTabCategories))]
    public partial int ActiveTab { get; set; }

    public bool IsTabTypes => ActiveTab == 0;
    public bool IsTabCategories => ActiveTab == 1;

    [RelayCommand]
    private void SwitchTab(string tab)
    {
        ActiveTab = int.TryParse(tab, out var t) ? t : 0;
        ClearMessages();
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Tab 0 â€” Category Types
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [ObservableProperty]
    public partial ObservableCollection<CategoryType> CategoryTypes { get; set; } = [];

    [ObservableProperty]
    public partial CategoryType? SelectedType { get; set; }

    [ObservableProperty]
    public partial string TypeName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditingType { get; set; }

    partial void OnSelectedTypeChanged(CategoryType? value)
    {
        if (value is null) return;
        TypeName = value.Name;
        IsEditingType = true;
        ClearMessages();
    }

    [RelayCommand]
    private void ClearTypeForm()
    {
        SelectedType = null;
        TypeName = string.Empty;
        IsEditingType = false;
        ClearMessages();
    }

    [RelayCommand]
    private Task SaveTypeAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(TypeName), "Type name is required.")))
            return;

        if (IsEditingType && SelectedType is not null)
        {
            await categoryService.UpdateTypeAsync(SelectedType.Id, TypeName.Trim(), ct);
            SuccessMessage = "Category type updated.";
        }
        else
        {
            await categoryService.CreateTypeAsync(TypeName.Trim(), ct);
            SuccessMessage = "Category type created.";
        }

        await ReloadTypesAsync(ct);
        ClearTypeForm();
    });

    [RelayCommand]
    private Task ToggleTypeActiveAsync(CategoryType? categoryType) => RunAsync(async ct =>
    {
        if (categoryType is null) return;
        await categoryService.ToggleTypeActiveAsync(categoryType.Id, ct);
        SuccessMessage = $"Type '{categoryType.Name}' {(categoryType.IsActive ? "deactivated" : "activated")}.";
        await ReloadTypesAsync(ct);
    });

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Tab 1 â€” Categories
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [ObservableProperty]
    public partial ObservableCollection<Category> Categories { get; set; } = [];

    [ObservableProperty]
    public partial Category? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial string CategorySearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CategoryName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CategoryType? SelectedCategoryType { get; set; }

    [ObservableProperty]
    public partial bool IsEditingCategory { get; set; }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        if (value is null) return;
        CategoryName = value.Name;
        SelectedCategoryType = CategoryTypes.FirstOrDefault(t => t.Id == value.CategoryTypeId);
        IsEditingCategory = true;
        ClearMessages();
    }

    [RelayCommand]
    private void ClearCategoryForm()
    {
        SelectedCategory = null;
        CategoryName = string.Empty;
        SelectedCategoryType = null;
        IsEditingCategory = false;
        ClearMessages();
    }

    [RelayCommand]
    private Task SaveCategoryAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(CategoryName), "Category name is required.")))
            return;

        var dto = new CategoryDto(CategoryName.Trim(), SelectedCategoryType?.Id);

        if (IsEditingCategory && SelectedCategory is not null)
        {
            await categoryService.UpdateAsync(SelectedCategory.Id, dto, ct);
            SuccessMessage = "Category updated.";
        }
        else
        {
            await categoryService.CreateAsync(dto, ct);
            SuccessMessage = "Category created.";
        }

        await ReloadCategoriesAsync(ct);
        ClearCategoryForm();
    });

    [RelayCommand]
    private Task ToggleCategoryActiveAsync(Category? category) => RunAsync(async ct =>
    {
        if (category is null) return;
        await categoryService.ToggleActiveAsync(category.Id, ct);
        SuccessMessage = $"Category '{category.Name}' {(category.IsActive ? "deactivated" : "activated")}.";
        await ReloadCategoriesAsync(ct);
    });

    [RelayCommand]
    private Task SearchCategoriesAsync() => RunAsync(async ct =>
    {
        if (string.IsNullOrWhiteSpace(CategorySearchText))
        {
            await ReloadCategoriesAsync(ct);
            return;
        }
        var results = await categoryService.SearchAsync(CategorySearchText, ct);
        Categories = new ObservableCollection<Category>(results);
    });

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Load
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await Task.WhenAll(
            ReloadTypesAsync(ct),
            ReloadCategoriesAsync(ct));
    });

    private async Task ReloadTypesAsync(CancellationToken ct)
    {
        var types = await categoryService.GetAllTypesAsync(ct);
        CategoryTypes = new ObservableCollection<CategoryType>(types);
    }

    private async Task ReloadCategoriesAsync(CancellationToken ct)
    {
        var categories = await categoryService.GetAllAsync(ct);
        Categories = new ObservableCollection<Category>(categories);
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}

