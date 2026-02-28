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
using StoreAssistantPro.Modules.Brands.Services;
using StoreAssistantPro.Core.Session;

namespace StoreAssistantPro.Modules.Products.ViewModels;

public partial class ProductsViewModel(
    IProductService productService,
    ITaxService taxService,
    IBrandService brandService,
    ISessionService sessionService,
    IDialogService dialogService,
    IMasterPinValidator masterPinValidator,
    INotificationService notificationService,
    ICommandBus commandBus) : BaseViewModel
{
    private const int PageSize = 50;
    private CancellationTokenSource? _searchCts;
    private int _lastLowStockCount = -1;

    // ── Role-based access ──

    public bool CanManageProducts =>
        sessionService.CurrentUserType is UserType.Admin or UserType.Manager;

    public bool CanDeleteProducts =>
        sessionService.CurrentUserType is UserType.Admin;

    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedProduct))]
    public partial Product? SelectedProduct { get; set; }

    public bool HasSelectedProduct => SelectedProduct is not null;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial StockFilter SelectedStockFilter { get; set; } = StockFilter.All;

    public StockFilter[] StockFilterOptions { get; } = Enum.GetValues<StockFilter>();

    [ObservableProperty]
    public partial ActiveFilter SelectedActiveFilter { get; set; } = ActiveFilter.All;

    public ActiveFilter[] ActiveFilterOptions { get; } = Enum.GetValues<ActiveFilter>();

    // ── Brand filter ──

    [ObservableProperty]
    public partial ObservableCollection<BrandFilterItem> BrandFilterOptions { get; set; } = [];

    [ObservableProperty]
    public partial BrandFilterItem? SelectedBrandFilter { get; set; }

    // ── Color filter ──

    [ObservableProperty]
    public partial ObservableCollection<string> ColorFilterOptions { get; set; } = [];

    [ObservableProperty]
    public partial string? SelectedColorFilter { get; set; }

    // ── Tax Profile filter ──

    [ObservableProperty]
    public partial ObservableCollection<TaxProfileFilterItem> TaxProfileFilterOptions { get; set; } = [];

    [ObservableProperty]
    public partial TaxProfileFilterItem? SelectedTaxProfileFilter { get; set; }

    // ── UOM filter ──

    [ObservableProperty]
    public partial ObservableCollection<string> UomFilterOptions { get; set; } = [];

    [ObservableProperty]
    public partial string? SelectedUomFilter { get; set; }

    // ── Multi-select for bulk operations ──

    private IReadOnlyList<Product> _selectedProductsForBulkDelete = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BulkDeleteProductsCommand))]
    public partial int SelectedProductCount { get; set; }

    public bool CanBulkDelete => CanDeleteProducts && SelectedProductCount >= 2;

    public void UpdateSelectedProducts(System.Collections.IList items)
    {
        _selectedProductsForBulkDelete = items.OfType<Product>().ToList();
        SelectedProductCount = _selectedProductsForBulkDelete.Count;
    }

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
    public partial int NewProductMaxStockLevel { get; set; }

    [ObservableProperty]
    public partial bool NewProductIsActive { get; set; } = true;

    [ObservableProperty]
    public partial bool NewProductIsTaxInclusive { get; set; }

    [ObservableProperty]
    public partial string NewProductColor { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsAddFormVisible { get; set; }

    [ObservableProperty]
    public partial bool IsEditFormVisible { get; set; }

    // ── Stock adjustment form ──

    [ObservableProperty]
    public partial bool IsStockAdjustFormVisible { get; set; }

    [ObservableProperty]
    public partial string AdjustStockProductName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int AdjustStockCurrentQty { get; set; }

    [ObservableProperty]
    public partial int AdjustStockQty { get; set; }

    [ObservableProperty]
    public partial string AdjustStockReason { get; set; } = string.Empty;

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
    public partial int EditProductMaxStockLevel { get; set; }

    [ObservableProperty]
    public partial bool EditProductIsActive { get; set; } = true;

    [ObservableProperty]
    public partial bool EditProductIsTaxInclusive { get; set; }

    [ObservableProperty]
    public partial string EditProductColor { get; set; } = string.Empty;

    // ── Tax profile selection ──

    [ObservableProperty]
    public partial ObservableCollection<TaxProfile> AvailableTaxProfiles { get; set; } = [];

    [ObservableProperty]
    public partial TaxProfile? NewProductTaxProfile { get; set; }

    [ObservableProperty]
    public partial TaxProfile? EditProductTaxProfile { get; set; }

    // ── Brand selection ──

    [ObservableProperty]
    public partial ObservableCollection<Brand> AvailableBrands { get; set; } = [];

    [ObservableProperty]
    public partial Brand? NewProductBrand { get; set; }

    [ObservableProperty]
    public partial Brand? EditProductBrand { get; set; }

    // ── Sorting ──

    [ObservableProperty]
    public partial string? SortColumn { get; set; }

    [ObservableProperty]
    public partial bool SortDescending { get; set; }

    [RelayCommand]
    private Task SortByColumnAsync(string columnName)
    {
        if (string.Equals(SortColumn, columnName, StringComparison.OrdinalIgnoreCase))
        {
            SortDescending = !SortDescending;
        }
        else
        {
            SortColumn = columnName;
            SortDescending = false;
        }

        PageIndex = 0;
        return LoadProductsAsync();
    }

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

    partial void OnSelectedBrandFilterChanged(BrandFilterItem? value)
    {
        PageIndex = 0;
        DebounceSearch();
    }

    partial void OnSelectedColorFilterChanged(string? value)
    {
        PageIndex = 0;
        DebounceSearch();
    }

    partial void OnSelectedTaxProfileFilterChanged(TaxProfileFilterItem? value)
    {
        PageIndex = 0;
        DebounceSearch();
    }

    partial void OnSelectedUomFilterChanged(string? value)
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
        if (BrandFilterOptions.Count == 0)
            await LoadBrandFiltersAsync();

        if (ColorFilterOptions.Count == 0)
            await LoadColorFiltersAsync();

        if (TaxProfileFilterOptions.Count == 0)
            await LoadTaxProfileFiltersAsync();

        if (UomFilterOptions.Count == 0)
            await LoadUomFiltersAsync();

        var result = await productService.GetPagedAsync(
            new PagedQuery(PageIndex, PageSize, SearchText, SelectedStockFilter, SelectedActiveFilter,
                SelectedBrandFilter?.BrandId, SortColumn, SortDescending,
                SelectedColorFilter is "All Colors" ? null : SelectedColorFilter,
                SelectedTaxProfileFilter?.TaxProfileId,
                SelectedUomFilter is "All UOM" ? null : SelectedUomFilter), ct);

        Products = new ObservableCollection<Product>(result.Items);
        TotalPages = result.TotalPages;
        TotalCount = result.TotalCount;
        PageIndex = result.PageIndex;

        await CheckLowStockAsync(ct);
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
        await LoadBrandsAsync();
        NewProductName = string.Empty;
        NewProductSalePrice = 0;
        NewProductCostPrice = 0;
        NewProductQuantity = 1;
        NewProductHSNCode = string.Empty;
        NewProductBarcode = string.Empty;
        NewProductUOM = "pcs";
        NewProductMinStockLevel = 0;
        NewProductMaxStockLevel = 0;
        NewProductIsActive = true;
        NewProductIsTaxInclusive = false;
        NewProductColor = string.Empty;
        NewProductTaxProfile = AvailableTaxProfiles.FirstOrDefault(p => p.IsDefault);
        NewProductBrand = null;
        IsEditFormVisible = false;
        IsStockAdjustFormVisible = false;
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
        await LoadBrandsAsync();
        NewProductName = SelectedProduct.Name + " (Copy)";
        NewProductSalePrice = SelectedProduct.SalePrice;
        NewProductCostPrice = SelectedProduct.CostPrice;
        NewProductQuantity = 0;
        NewProductHSNCode = SelectedProduct.HSNCode ?? string.Empty;
        NewProductBarcode = string.Empty;
        NewProductUOM = SelectedProduct.UOM;
        NewProductMinStockLevel = SelectedProduct.MinStockLevel;
        NewProductMaxStockLevel = SelectedProduct.MaxStockLevel;
        NewProductIsActive = SelectedProduct.IsActive;
        NewProductIsTaxInclusive = SelectedProduct.IsTaxInclusive;
        NewProductColor = SelectedProduct.Color ?? string.Empty;
        NewProductTaxProfile = AvailableTaxProfiles
            .FirstOrDefault(p => p.Id == SelectedProduct.TaxProfileId);
        NewProductBrand = AvailableBrands
            .FirstOrDefault(b => b.Id == SelectedProduct.BrandId);
        IsEditFormVisible = false;
        IsStockAdjustFormVisible = false;
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
            NewProductTaxProfile?.Id, NewProductBrand?.Id, NewProductHSNCode, NewProductBarcode, NewProductUOM,
            NewProductMinStockLevel, NewProductMaxStockLevel, NewProductIsActive, NewProductIsTaxInclusive, NewProductColor));

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
        await LoadBrandsAsync();
        EditProductName = SelectedProduct.Name;
        EditProductSalePrice = SelectedProduct.SalePrice;
        EditProductCostPrice = SelectedProduct.CostPrice;
        EditProductQuantity = SelectedProduct.Quantity;
        EditProductHSNCode = SelectedProduct.HSNCode ?? string.Empty;
        EditProductBarcode = SelectedProduct.Barcode ?? string.Empty;
        EditProductUOM = SelectedProduct.UOM;
        EditProductMinStockLevel = SelectedProduct.MinStockLevel;
        EditProductMaxStockLevel = SelectedProduct.MaxStockLevel;
        EditProductIsActive = SelectedProduct.IsActive;
        EditProductIsTaxInclusive = SelectedProduct.IsTaxInclusive;
        EditProductColor = SelectedProduct.Color ?? string.Empty;
        EditProductTaxProfile = AvailableTaxProfiles
            .FirstOrDefault(p => p.Id == SelectedProduct.TaxProfileId);
        EditProductBrand = AvailableBrands
            .FirstOrDefault(b => b.Id == SelectedProduct.BrandId);
        IsAddFormVisible = false;
        IsStockAdjustFormVisible = false;
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
            EditProductBrand?.Id, EditProductHSNCode, EditProductBarcode, EditProductUOM,
            EditProductMinStockLevel, EditProductMaxStockLevel, EditProductIsActive, EditProductIsTaxInclusive,
            EditProductColor, product.RowVersion));

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

    // ── Stock adjustment ──

    [RelayCommand]
    private void ShowStockAdjustForm()
    {
        if (SelectedProduct is null) return;

        if (!CanManageProducts)
        {
            ErrorMessage = "Only administrators and managers can adjust stock.";
            return;
        }

        ErrorMessage = string.Empty;
        AdjustStockProductName = SelectedProduct.Name;
        AdjustStockCurrentQty = SelectedProduct.Quantity;
        AdjustStockQty = 0;
        AdjustStockReason = string.Empty;
        IsAddFormVisible = false;
        IsEditFormVisible = false;
        IsStockAdjustFormVisible = true;
    }

    [RelayCommand]
    private void CancelStockAdjust()
    {
        IsStockAdjustFormVisible = false;
    }

    [RelayCommand]
    private async Task SaveStockAdjustAsync()
    {
        if (!Validate(v => v
            .Rule(SelectedProduct is not null, "No product selected.")
            .Rule(AdjustStockQty != 0, "Adjustment quantity cannot be zero.")))
            return;

        var product = SelectedProduct!;
        var result = await commandBus.SendAsync(new AdjustStockCommand(
            product.Id, AdjustStockQty,
            string.IsNullOrWhiteSpace(AdjustStockReason) ? null : AdjustStockReason.Trim(),
            product.RowVersion));

        if (result.Succeeded)
        {
            IsStockAdjustFormVisible = false;
            await LoadProductsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Stock adjustment failed.";
            await LoadProductsAsync();
        }
    }

    private async Task LoadTaxProfilesAsync()
    {
        var profiles = await taxService.GetAllProfilesAsync();
        AvailableTaxProfiles = new ObservableCollection<TaxProfile>(
            profiles.Where(p => p.IsActive));
    }

    private async Task LoadBrandsAsync()
    {
        var brands = await brandService.GetAllAsync();
        AvailableBrands = new ObservableCollection<Brand>(
            brands.Where(b => b.IsActive));
    }

    private async Task LoadBrandFiltersAsync()
    {
        var brands = await brandService.GetAllAsync();
        var items = new List<BrandFilterItem> { new(null, "All Brands") };
        items.AddRange(brands
            .Where(b => b.IsActive)
            .Select(b => new BrandFilterItem(b.Id, b.Name)));
        BrandFilterOptions = new ObservableCollection<BrandFilterItem>(items);
        SelectedBrandFilter ??= BrandFilterOptions[0];
    }

    private async Task LoadColorFiltersAsync()
    {
        var allProducts = await productService.GetAllAsync();
        var colors = allProducts
            .Where(p => !string.IsNullOrWhiteSpace(p.Color))
            .Select(p => p.Color!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();
        colors.Insert(0, "All Colors");
        ColorFilterOptions = new ObservableCollection<string>(colors);
        SelectedColorFilter ??= ColorFilterOptions[0];
    }

    private async Task LoadTaxProfileFiltersAsync()
    {
        var profiles = await taxService.GetAllProfilesAsync();
        var items = new List<TaxProfileFilterItem> { new(null, "All Tax Profiles") };
        items.AddRange(profiles
            .Where(p => p.IsActive)
            .Select(p => new TaxProfileFilterItem(p.Id, p.ProfileName)));
        TaxProfileFilterOptions = new ObservableCollection<TaxProfileFilterItem>(items);
        SelectedTaxProfileFilter ??= TaxProfileFilterOptions[0];
    }

    private async Task LoadUomFiltersAsync()
    {
        var allProducts = await productService.GetAllAsync();
        var uoms = allProducts
            .Select(p => p.UOM)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(u => u)
            .ToList();
        uoms.Insert(0, "All UOM");
        UomFilterOptions = new ObservableCollection<string>(uoms);
        SelectedUomFilter ??= UomFilterOptions[0];
    }

    private async Task CheckLowStockAsync(CancellationToken ct)
    {
        var count = await productService.GetLowStockCountAsync(ct);
        if (count != _lastLowStockCount && count > 0)
        {
            await notificationService.PostAsync(
                "Low Stock Alert",
                $"⚠ {count} product{(count == 1 ? " is" : "s are")} below minimum stock level.",
                AppNotificationLevel.Warning);
        }
        _lastLowStockCount = count;
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
            sb.AppendLine("Name,SalePrice,CostPrice,Quantity,HSNCode,Barcode,UOM,MinStockLevel,MaxStockLevel,IsActive,IsTaxInclusive,Brand,Color");

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
                    p.MaxStockLevel,
                    p.IsActive,
                    p.IsTaxInclusive,
                    EscapeCsv(p.Brand?.Name ?? ""),
                    EscapeCsv(p.Color ?? "")));
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

    [RelayCommand]
    private async Task ExportLowStockAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            Title = "Export Low Stock Products to CSV",
            FileName = "LowStock_Export.csv"
        };

        if (dialog.ShowDialog() != true) return;

        ErrorMessage = string.Empty;

        try
        {
            var allProducts = await productService.GetAllAsync();
            var lowStock = allProducts.Where(p => p.IsActive && p.IsLowStock).ToList();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Name,SalePrice,CostPrice,Quantity,MinStockLevel,Barcode,UOM,Brand,Color");

            foreach (var p in lowStock)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(p.Name),
                    p.SalePrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    p.CostPrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    p.Quantity,
                    p.MinStockLevel,
                    EscapeCsv(p.Barcode ?? ""),
                    EscapeCsv(p.UOM),
                    EscapeCsv(p.Brand?.Name ?? ""),
                    EscapeCsv(p.Color ?? "")));
            }

            await System.IO.File.WriteAllTextAsync(dialog.FileName, sb.ToString());
            ErrorMessage = $"Exported {lowStock.Count} low-stock products to {System.IO.Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanBulkDelete))]
    private async Task BulkDeleteProductsAsync()
    {
        ErrorMessage = string.Empty;

        if (_selectedProductsForBulkDelete.Count < 2) return;

        if (!CanDeleteProducts)
        {
            ErrorMessage = "Only administrators can delete products.";
            return;
        }

        if (!dialogService.Confirm(
            $"Delete {_selectedProductsForBulkDelete.Count} products?\n\nThis action cannot be undone.",
            "Bulk Delete Products"))
            return;

        if (!await masterPinValidator.ValidateAsync("Enter Master PIN to bulk-delete products."))
        {
            ErrorMessage = "Master PIN validation failed. Bulk delete cancelled.";
            return;
        }

        var items = _selectedProductsForBulkDelete
            .Select(p => new BulkDeleteItem(p.Id, p.Name, p.RowVersion))
            .ToList();

        var result = await commandBus.SendAsync<BulkDeleteProductsCommand, BulkDeleteProductsResult>(
            new BulkDeleteProductsCommand(items));

        if (result.Succeeded && result.Value is { } bulkResult)
        {
            var msg = $"Deleted {bulkResult.Deleted} products.";
            if (bulkResult.Failed > 0)
                msg += $" {bulkResult.Failed} could not be deleted (modified by another user): {string.Join(", ", bulkResult.FailedNames)}.";
            ErrorMessage = msg;
            SelectedProduct = null;
            await LoadProductsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Bulk delete failed.";
            await LoadProductsAsync();
        }
    }
}

/// <summary>
/// Item for the brand filter dropdown. <c>BrandId == null</c> means "All Brands".
/// </summary>
public sealed record BrandFilterItem(int? BrandId, string DisplayName)
{
    public override string ToString() => DisplayName;
}

/// <summary>
/// Item for the tax profile filter dropdown. <c>TaxProfileId == null</c> means "All Tax Profiles".
/// </summary>
public sealed record TaxProfileFilterItem(int? TaxProfileId, string DisplayName)
{
    public override string ToString() => DisplayName;
}
