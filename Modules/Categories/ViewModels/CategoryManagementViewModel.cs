using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Categories.Services;

namespace StoreAssistantPro.Modules.Categories.ViewModels;

public partial class CategoryManagementViewModel(ICategoryService categoryService) : BaseViewModel
{

    private static readonly TimeSpan NavigationFreshnessWindow = TimeSpan.FromMinutes(2);

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
    private Task ToggleTypeActiveAsync(CategoryType? categoryType) => RunAsync(async ct =>
    {
        if (categoryType is null) return;
        await categoryService.ToggleTypeActiveAsync(categoryType.Id, ct);
        SuccessMessage = $"Type '{categoryType.Name}' {(categoryType.IsActive ? "deactivated" : "activated")}.";
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

    // -- Paging --

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial string PagingInfo { get; set; } = string.Empty;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    private const int PageSize = 25;

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
        CurrentPage = 1;
        await ReloadCategoriesAsync(ct);
    });

    [RelayCommand]
    private Task PreviousPageAsync() => RunAsync(async ct =>
    {
        if (!HasPreviousPage) return;
        CurrentPage--;
        await ReloadCategoriesAsync(ct);
    });

    [RelayCommand]
    private Task NextPageAsync() => RunAsync(async ct =>
    {
        if (!HasNextPage) return;
        CurrentPage++;
        await ReloadCategoriesAsync(ct);
    });

    [RelayCommand]
    private void ExportCategoriesCsv()
    {
        if (Categories.Count == 0) return;
        if (CsvExporter.Export(Categories, "Categories.csv"))
            SuccessMessage = "Categories exported to CSV.";
    }

    [RelayCommand]
    private Task ImportCategoriesCsvAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        var rows = CsvImporter.Import();
        if (rows is null) return;
        if (rows.Count == 0) { ErrorMessage = "CSV file is empty."; return; }

        var names = rows
            .Select(r => r.GetValueOrDefault("Name") ?? r.GetValueOrDefault("Category") ?? "")
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        if (names.Count == 0) { ErrorMessage = "No valid category names found in CSV. Expected column: Name."; return; }

        var imported = await categoryService.ImportBulkAsync(names, ct);
        SuccessMessage = $"Imported {imported} category(ies). {names.Count - imported} skipped (duplicates).";
        await ReloadCategoriesAsync(ct);
    });

    [RelayCommand]
    private void ExportTypesCsv()
    {
        if (CategoryTypes.Count == 0) return;
        if (CsvExporter.Export(CategoryTypes, "CategoryTypes.csv"))
            SuccessMessage = "Category types exported to CSV.";
    }

    // ═══════════════════════════════════════════════════════════════
    //  Load
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private Task LoadAsync() => LoadOnActivateAsync(async ct =>
    {
        await Task.WhenAll(
            ReloadTypesAsync(ct),
            ReloadCategoriesAsync(ct));
    },
        NavigationFreshnessWindow);

    private async Task ReloadTypesAsync(CancellationToken ct)
    {
        var types = await categoryService.GetAllTypesAsync(ct);
        CategoryTypes = new ObservableCollection<CategoryType>(types);
    }

    private async Task ReloadCategoriesAsync(CancellationToken ct)
    {
        var search = string.IsNullOrWhiteSpace(CategorySearchText) ? null : CategorySearchText;
        var result = await categoryService.GetPagedAsync(new PagedQuery(CurrentPage, PageSize), search, ct);
        Categories = new ObservableCollection<Category>(result.Items);
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages == 0 ? 1 : result.TotalPages;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        PagingInfo = TotalCount > 0
            ? $"Page {CurrentPage} of {TotalPages} ({TotalCount} total)"
            : string.Empty;
    }

    public async Task<bool> TryInlineRenameTypeAsync(
        CategoryType? categoryType,
        string? editedName,
        string? originalName,
        CancellationToken ct = default)
    {
        if (categoryType is null)
            return false;

        ClearMessages();

        var fallbackName = string.IsNullOrWhiteSpace(originalName)
            ? categoryType.Name
            : originalName.Trim();
        var trimmedName = editedName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            categoryType.Name = fallbackName;
            ErrorMessage = "Type name is required.";
            return false;
        }

        if (string.Equals(trimmedName, fallbackName, StringComparison.Ordinal))
        {
            categoryType.Name = trimmedName;
            if (SelectedType?.Id == categoryType.Id)
                TypeName = trimmedName;
            return true;
        }

        try
        {
            await categoryService.UpdateTypeAsync(categoryType.Id, trimmedName, ct);
            categoryType.Name = trimmedName;
            if (SelectedType?.Id == categoryType.Id)
                TypeName = trimmedName;

            SuccessMessage = "Category type updated.";
            return true;
        }
        catch (Exception ex)
        {
            categoryType.Name = fallbackName;
            ErrorMessage = ex.Message;
            return false;
        }
    }

    public async Task<bool> TryInlineRenameCategoryAsync(
        Category? category,
        string? editedName,
        string? originalName,
        CancellationToken ct = default)
    {
        if (category is null)
            return false;

        ClearMessages();

        var fallbackName = string.IsNullOrWhiteSpace(originalName)
            ? category.Name
            : originalName.Trim();
        var trimmedName = editedName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            category.Name = fallbackName;
            ErrorMessage = "Category name is required.";
            return false;
        }

        if (string.Equals(trimmedName, fallbackName, StringComparison.Ordinal))
        {
            category.Name = trimmedName;
            if (SelectedCategory?.Id == category.Id)
                CategoryName = trimmedName;
            return true;
        }

        try
        {
            await categoryService.UpdateAsync(
                category.Id,
                new CategoryDto(trimmedName, category.CategoryTypeId),
                ct);
            category.Name = trimmedName;
            if (SelectedCategory?.Id == category.Id)
                CategoryName = trimmedName;

            SuccessMessage = "Category updated.";
            return true;
        }
        catch (Exception ex)
        {
            category.Name = fallbackName;
            ErrorMessage = ex.Message;
            return false;
        }
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}

