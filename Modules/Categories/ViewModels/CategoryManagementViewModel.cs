using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Categories.Services;

namespace StoreAssistantPro.Modules.Categories.ViewModels;

public partial class CategoryManagementViewModel(ICategoryService categoryService) : BaseViewModel
{
    // ═══════════════════════════════════════════════════════════════
    //  Tab Navigation (Types / Categories)
    // ═══════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════
    //  Tab 0 — Category Types
    // ═══════════════════════════════════════════════════════════════

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
    private Task ToggleTypeActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedType is null) return;
        await categoryService.ToggleTypeActiveAsync(SelectedType.Id, ct);
        SuccessMessage = $"Type '{SelectedType.Name}' {(SelectedType.IsActive ? "deactivated" : "activated")}.";
        await ReloadTypesAsync(ct);
    });

    // ═══════════════════════════════════════════════════════════════
    //  Tab 1 — Categories
    // ═══════════════════════════════════════════════════════════════

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
    private Task ToggleCategoryActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedCategory is null) return;
        await categoryService.ToggleActiveAsync(SelectedCategory.Id, ct);
        SuccessMessage = $"Category '{SelectedCategory.Name}' {(SelectedCategory.IsActive ? "deactivated" : "activated")}.";
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

    // ═══════════════════════════════════════════════════════════════
    //  Load
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await ReloadTypesAsync(ct);
        await ReloadCategoriesAsync(ct);
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
