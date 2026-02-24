using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Data;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Products.Commands;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Tax.Services;
using StoreAssistantPro.Core.Session;

namespace StoreAssistantPro.Modules.Products.ViewModels;

public partial class ProductsViewModel(
    IProductService productService,
    ITaxService taxService,
    ISessionService sessionService,
    IDialogService dialogService,
    IMasterPinValidator masterPinValidator,
    ICommandBus commandBus) : BaseViewModel
{
    private const int PageSize = 50;
    private CancellationTokenSource? _searchCts;

    // ── Role-based access ──

    public bool CanManageProducts =>
        sessionService.CurrentUserType is UserType.Admin or UserType.Manager;

    public bool CanDeleteProducts =>
        sessionService.CurrentUserType is UserType.Admin;

    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    [ObservableProperty]
    public partial Product? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial StockFilter SelectedStockFilter { get; set; } = StockFilter.All;

    public StockFilter[] StockFilterOptions { get; } = Enum.GetValues<StockFilter>();

    [ObservableProperty]
    public partial ActiveFilter SelectedActiveFilter { get; set; } = ActiveFilter.All;

    public ActiveFilter[] ActiveFilterOptions { get; } = Enum.GetValues<ActiveFilter>();

    // ── Paging state ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int PageIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int TotalPages { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    public partial int TotalCount { get; set; }

    public string PageDisplay => TotalPages > 0
        ? $"Page {PageIndex + 1} of {TotalPages} ({TotalCount} items)"
        : "No results";

    public bool HasPreviousPage => PageIndex > 0;
    public bool HasNextPage => PageIndex < TotalPages - 1;

    // ── Add form ──

    [ObservableProperty]
    public partial string NewProductName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal NewProductSalePrice { get; set; }

    [ObservableProperty]
    public partial decimal NewProductCostPrice { get; set; }

    [ObservableProperty]
    public partial int NewProductQuantity { get; set; }

    [ObservableProperty]
    public partial string NewProductHSNCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewProductBarcode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewProductUOM { get; set; } = "pcs";

    [ObservableProperty]
    public partial int NewProductMinStockLevel { get; set; }

    [ObservableProperty]
    public partial bool NewProductIsActive { get; set; } = true;

    [ObservableProperty]
    public partial bool NewProductIsTaxInclusive { get; set; }

    [ObservableProperty]
    public partial bool IsAddFormVisible { get; set; }

    [ObservableProperty]
    public partial bool IsEditFormVisible { get; set; }

    [ObservableProperty]
    public partial string EditProductName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal EditProductSalePrice { get; set; }

    [ObservableProperty]
    public partial decimal EditProductCostPrice { get; set; }

    [ObservableProperty]
    public partial int EditProductQuantity { get; set; }

    [ObservableProperty]
    public partial string EditProductHSNCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditProductBarcode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditProductUOM { get; set; } = "pcs";

    [ObservableProperty]
    public partial int EditProductMinStockLevel { get; set; }

    [ObservableProperty]
    public partial bool EditProductIsActive { get; set; } = true;

    [ObservableProperty]
    public partial bool EditProductIsTaxInclusive { get; set; }

    // ── Tax profile selection ──

    [ObservableProperty]
    public partial ObservableCollection<TaxProfile> AvailableTaxProfiles { get; set; } = [];

    [ObservableProperty]
    public partial TaxProfile? NewProductTaxProfile { get; set; }

    [ObservableProperty]
    public partial TaxProfile? EditProductTaxProfile { get; set; }

    // ── Search (server-side with debounce) ──

    partial void OnSearchTextChanged(string value)
    {
        PageIndex = 0;
        DebounceSearch();
    }

    partial void OnSelectedStockFilterChanged(StockFilter value)
    {
        PageIndex = 0;
        DebounceSearch();
    }

    partial void OnSelectedActiveFilterChanged(ActiveFilter value)
    {
        PageIndex = 0;
        DebounceSearch();
    }

    private async void DebounceSearch()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        try
        {
            await Task.Delay(300, ct);
            await LoadProductsAsync();
        }
        catch (OperationCanceledException)
        {
            // Debounce — superseded by newer keystroke.
        }
    }

    // ── Data loading ──

    [RelayCommand]
    private Task LoadProductsAsync() => RunLoadAsync(async ct =>
    {
        var result = await productService.GetPagedAsync(
            new PagedQuery(PageIndex, PageSize, SearchText, SelectedStockFilter, SelectedActiveFilter), ct);

        Products = new ObservableCollection<Product>(result.Items);
        TotalPages = result.TotalPages;
        TotalCount = result.TotalCount;
        PageIndex = result.PageIndex;
    });

    [RelayCommand(CanExecute = nameof(HasNextPage))]
    private Task NextPageAsync()
    {
        PageIndex++;
        return LoadProductsAsync();
    }

    [RelayCommand(CanExecute = nameof(HasPreviousPage))]
    private Task PreviousPageAsync()
    {
        PageIndex--;
        return LoadProductsAsync();
    }

    [RelayCommand]
    private async Task ShowAddFormAsync()
    {
        if (!CanManageProducts)
        {
            ErrorMessage = "Only administrators and managers can add products.";
            return;
        }

        ErrorMessage = string.Empty;
        await LoadTaxProfilesAsync();
        NewProductName = string.Empty;
        NewProductSalePrice = 0;
        NewProductCostPrice = 0;
        NewProductQuantity = 1;
        NewProductHSNCode = string.Empty;
        NewProductBarcode = string.Empty;
        NewProductUOM = "pcs";
        NewProductMinStockLevel = 0;
        NewProductIsActive = true;
        NewProductIsTaxInclusive = false;
        NewProductTaxProfile = AvailableTaxProfiles.FirstOrDefault(p => p.IsDefault);
        IsEditFormVisible = false;
        IsAddFormVisible = true;
    }

    [RelayCommand]
    private async Task DuplicateProductAsync()
    {
        if (SelectedProduct is null) return;

        if (!CanManageProducts)
        {
            ErrorMessage = "Only administrators and managers can duplicate products.";
            return;
        }

        ErrorMessage = string.Empty;
        await LoadTaxProfilesAsync();
        NewProductName = SelectedProduct.Name + " (Copy)";
        NewProductSalePrice = SelectedProduct.SalePrice;
        NewProductCostPrice = SelectedProduct.CostPrice;
        NewProductQuantity = 0;
        NewProductHSNCode = SelectedProduct.HSNCode ?? string.Empty;
        NewProductBarcode = string.Empty;
        NewProductUOM = SelectedProduct.UOM;
        NewProductMinStockLevel = SelectedProduct.MinStockLevel;
        NewProductIsActive = SelectedProduct.IsActive;
        NewProductIsTaxInclusive = SelectedProduct.IsTaxInclusive;
        NewProductTaxProfile = AvailableTaxProfiles
            .FirstOrDefault(p => p.Id == SelectedProduct.TaxProfileId);
        IsEditFormVisible = false;
        IsAddFormVisible = true;
    }

    [RelayCommand]
    private void CancelAdd()
    {
        IsAddFormVisible = false;
    }

    [RelayCommand]
    private async Task SaveProductAsync()
    {
        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(NewProductName), "Product name is required.")
            .Rule(InputValidator.IsNonNegative(NewProductSalePrice), "Sale price cannot be negative.")
            .Rule(InputValidator.IsNonNegative(NewProductQuantity), "Quantity cannot be negative.")))
            return;

        var result = await commandBus.SendAsync(new SaveProductCommand(
            NewProductName.Trim(), NewProductSalePrice, NewProductCostPrice, NewProductQuantity,
            NewProductTaxProfile?.Id, NewProductHSNCode, NewProductBarcode, NewProductUOM,
            NewProductMinStockLevel, NewProductIsActive, NewProductIsTaxInclusive));

        if (result.Succeeded)
        {
            IsAddFormVisible = false;
            await LoadProductsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Save failed.";
        }
    }

    [RelayCommand]
    private async Task ShowEditFormAsync()
    {
        if (SelectedProduct is null) return;

        if (!CanManageProducts)
        {
            ErrorMessage = "Only administrators and managers can edit products.";
            return;
        }

        ErrorMessage = string.Empty;
        await LoadTaxProfilesAsync();
        EditProductName = SelectedProduct.Name;
        EditProductSalePrice = SelectedProduct.SalePrice;
        EditProductCostPrice = SelectedProduct.CostPrice;
        EditProductQuantity = SelectedProduct.Quantity;
        EditProductHSNCode = SelectedProduct.HSNCode ?? string.Empty;
        EditProductBarcode = SelectedProduct.Barcode ?? string.Empty;
        EditProductUOM = SelectedProduct.UOM;
        EditProductMinStockLevel = SelectedProduct.MinStockLevel;
        EditProductIsActive = SelectedProduct.IsActive;
        EditProductIsTaxInclusive = SelectedProduct.IsTaxInclusive;
        EditProductTaxProfile = AvailableTaxProfiles
            .FirstOrDefault(p => p.Id == SelectedProduct.TaxProfileId);
        IsAddFormVisible = false;
        IsEditFormVisible = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditFormVisible = false;
    }

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (!Validate(v => v
            .Rule(SelectedProduct is not null, "No product selected.")
            .Rule(InputValidator.IsRequired(EditProductName), "Product name is required.")
            .Rule(InputValidator.IsNonNegative(EditProductSalePrice), "Sale price cannot be negative.")
            .Rule(InputValidator.IsNonNegative(EditProductQuantity), "Quantity cannot be negative.")))
            return;

        var product = SelectedProduct!;
        var result = await commandBus.SendAsync(new UpdateProductCommand(
            product.Id, EditProductName.Trim(), EditProductSalePrice,
            EditProductCostPrice, EditProductQuantity, EditProductTaxProfile?.Id,
            EditProductHSNCode, EditProductBarcode, EditProductUOM,
            EditProductMinStockLevel, EditProductIsActive, EditProductIsTaxInclusive, product.RowVersion));

        if (result.Succeeded)
        {
            IsEditFormVisible = false;
            await LoadProductsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Update failed.";
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteProductAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedProduct is null) return;

        if (!CanDeleteProducts)
        {
            ErrorMessage = "Only administrators can delete products.";
            return;
        }

        if (!dialogService.Confirm(
            $"Delete '{SelectedProduct.Name}'?\n\nThis action cannot be undone.",
            "Delete Product"))
            return;

        if (!await masterPinValidator.ValidateAsync("Enter Master PIN to delete this product."))
        {
            ErrorMessage = "Master PIN validation failed. Delete cancelled.";
            return;
        }

        var result = await commandBus.SendAsync(
            new DeleteProductCommand(SelectedProduct.Id, SelectedProduct.RowVersion));

        if (result.Succeeded)
        {
            SelectedProduct = null;
            await LoadProductsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Delete failed.";
            await LoadProductsAsync();
        }
    }

    private async Task LoadTaxProfilesAsync()
    {
        var profiles = await taxService.GetAllProfilesAsync();
        AvailableTaxProfiles = new ObservableCollection<TaxProfile>(
            profiles.Where(p => p.IsActive));
    }

    [RelayCommand]
    private async Task ImportProductsAsync()
    {
        if (!CanManageProducts)
        {
            ErrorMessage = "Only administrators and managers can import products.";
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Title = "Import Products from CSV"
        };

        if (dialog.ShowDialog() != true) return;

        ErrorMessage = string.Empty;
        var result = await commandBus.SendAsync<ImportProductsCommand, ImportProductsResult>(
            new ImportProductsCommand(dialog.FileName));

        if (result.Succeeded && result.Value is { } importResult)
        {
            var msg = $"Imported {importResult.Imported} products.";
            if (importResult.Skipped > 0)
                msg += $" Skipped {importResult.Skipped} rows.";
            ErrorMessage = msg;
            await LoadProductsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Import failed.";
        }
    }

    [RelayCommand]
    private async Task ExportProductsAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            Title = "Export Products to CSV",
            FileName = "Products_Export.csv"
        };

        if (dialog.ShowDialog() != true) return;

        ErrorMessage = string.Empty;

        try
        {
            var allProducts = await productService.GetAllAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Name,SalePrice,CostPrice,Quantity,HSNCode,Barcode,UOM,MinStockLevel,IsActive,IsTaxInclusive");

            foreach (var p in allProducts)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(p.Name),
                    p.SalePrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    p.CostPrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    p.Quantity,
                    EscapeCsv(p.HSNCode ?? ""),
                    EscapeCsv(p.Barcode ?? ""),
                    EscapeCsv(p.UOM),
                    p.MinStockLevel,
                    p.IsActive,
                    p.IsTaxInclusive));
            }

            await System.IO.File.WriteAllTextAsync(dialog.FileName, sb.ToString());
            ErrorMessage = $"Exported {allProducts.Count()} products to {System.IO.Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
