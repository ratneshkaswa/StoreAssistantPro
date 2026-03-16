using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Brands.Services;
using StoreAssistantPro.Modules.Categories.Services;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Modules.Products.ViewModels;

public partial class ProductManagementViewModel(
    IProductService productService,
    ITaxGroupService taxGroupService,
    INavigationService navigationService,
    ProductContextHolder productContextHolder) : BaseViewModel
{
    private List<Product> _allProducts = [];

    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    [ObservableProperty]
    public partial Product? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<TaxMaster> Taxes { get; set; } = [];

    // â”€â”€ Enterprise GST dropdowns â”€â”€

    [ObservableProperty]
    public partial ObservableCollection<TaxGroup> TaxGroups { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<HSNCode> HSNCodes { get; set; } = [];

    [ObservableProperty]
    public partial TaxGroup? SelectedTaxGroup { get; set; }

    [ObservableProperty]
    public partial HSNCode? SelectedHSNCode { get; set; }

    [ObservableProperty]
    public partial bool OverrideAllowed { get; set; }

    // â”€â”€ Category & Brand dropdowns â”€â”€

    [ObservableProperty]
    public partial ObservableCollection<Category> Categories { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Brand> Brands { get; set; } = [];

    [ObservableProperty]
    public partial Category? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial Brand? SelectedBrand { get; set; }

    // â”€â”€ Vendor dropdown â”€â”€

    [ObservableProperty]
    public partial ObservableCollection<Vendor> Vendors { get; set; } = [];

    [ObservableProperty]
    public partial Vendor? SelectedVendor { get; set; }

    // ── Search & filter ──

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Category? FilterCategory { get; set; }

    [ObservableProperty]
    public partial string FilterStockStatus { get; set; } = "All";

    [ObservableProperty]
    public partial string FilterCountText { get; set; } = string.Empty;

    public ObservableCollection<string> StockStatusOptions { get; } =
        ["All", "In Stock", "Low Stock", "Out of Stock"];

    // â”€â”€ Form fields â”€â”€

    [ObservableProperty]
    public partial string ProductName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ProductType SelectedProductType { get; set; } = ProductType.Readymade;

    [ObservableProperty]
    public partial ProductUnit SelectedUnit { get; set; } = ProductUnit.Piece;

    [ObservableProperty]
    public partial TaxMaster? SelectedTax { get; set; }

    [ObservableProperty]
    public partial string SalePriceText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CostPriceText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Barcode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTaxInclusive { get; set; }

    [ObservableProperty]
    public partial bool SupportsColour { get; set; } = true;

    [ObservableProperty]
    public partial bool SupportsSize { get; set; } = true;

    [ObservableProperty]
    public partial bool SupportsPattern { get; set; }

    [ObservableProperty]
    public partial bool SupportsType { get; set; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    public ObservableCollection<ProductType> ProductTypes { get; } =
        [ProductType.Readymade, ProductType.GarmentCloth];

    public ObservableCollection<ProductUnit> ProductUnits { get; } =
        [ProductUnit.Piece, ProductUnit.Meter];

    partial void OnSelectedProductChanged(Product? value)
    {
        ManageVariantsCommand.NotifyCanExecuteChanged();

        if (value is null)
        {
            ResetForm(clearMessages: false);
            return;
        }

        PopulateForm(value);
        ClearMappingSelection();
        _ = LoadMappingForProductCommand.ExecuteAsync(value.Id);
    }

    private void PopulateForm(Product value)
    {
        ProductName = value.Name;
        SelectedProductType = value.ProductType;
        SelectedUnit = value.Unit;
        SelectedTax = Taxes.FirstOrDefault(t => t.Id == value.TaxId);
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == value.CategoryId);
        SelectedBrand = Brands.FirstOrDefault(b => b.Id == value.BrandId);
        SelectedVendor = Vendors.FirstOrDefault(v => v.Id == value.VendorId);
        SalePriceText = value.SalePrice > 0 ? value.SalePrice.ToString("F0") : string.Empty;
        CostPriceText = value.CostPrice > 0 ? value.CostPrice.ToString("F0") : string.Empty;
        Barcode = value.Barcode ?? string.Empty;
        IsTaxInclusive = value.IsTaxInclusive;
        SupportsColour = value.SupportsColour;
        SupportsSize = value.SupportsSize;
        SupportsPattern = value.SupportsPattern;
        SupportsType = value.SupportsType;
        IsEditing = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private Task LoadMappingForProductAsync(int productId) => RunLoadAsync(async ct =>
    {
        var mapping = await taxGroupService.GetMappingByProductAsync(productId, ct);
        if (SelectedProduct?.Id != productId)
            return;

        if (mapping is not null)
        {
            SelectedTaxGroup = TaxGroups.FirstOrDefault(g => g.Id == mapping.TaxGroupId);
            SelectedHSNCode = HSNCodes.FirstOrDefault(h => h.Id == mapping.HSNCodeId);
            OverrideAllowed = mapping.OverrideAllowed;
        }
        else
        {
            SelectedTaxGroup = null;
            SelectedHSNCode = null;
            OverrideAllowed = false;
        }
    });

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await ReloadProductsAsync(ct);
    });

    [RelayCommand]
    private void NewProduct()
    {
        SelectedProduct = null;
        ResetForm(clearMessages: true);
    }

    private void ResetForm(bool clearMessages)
    {
        ProductName = string.Empty;
        SelectedProductType = ProductType.Readymade;
        SelectedUnit = ProductUnit.Piece;
        SelectedTax = null;
        ClearMappingSelection();
        SelectedCategory = null;
        SelectedBrand = null;
        SelectedVendor = null;
        SalePriceText = string.Empty;
        CostPriceText = string.Empty;
        Barcode = string.Empty;
        IsTaxInclusive = false;
        SupportsColour = true;
        SupportsSize = true;
        SupportsPattern = false;
        SupportsType = false;
        IsEditing = false;

        if (clearMessages)
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(ProductName), "Product name is required.")))
            return;

        decimal salePrice = 0, costPrice = 0;
        if (!string.IsNullOrWhiteSpace(SalePriceText) &&
            (!decimal.TryParse(SalePriceText, out salePrice) || salePrice < 0))
        {
            ErrorMessage = "Sale price must be a valid positive number.";
            return;
        }
        if (!string.IsNullOrWhiteSpace(CostPriceText) &&
            (!decimal.TryParse(CostPriceText, out costPrice) || costPrice < 0))
        {
            ErrorMessage = "Cost price must be a valid positive number.";
            return;
        }

        var dto = new ProductDto(
            ProductName, SelectedProductType, SelectedUnit,
            SelectedTax?.Id, SelectedCategory?.Id, SelectedBrand?.Id, SelectedVendor?.Id,
            SupportsColour, SupportsPattern,
            SupportsSize, SupportsType,
            salePrice, costPrice,
            string.IsNullOrWhiteSpace(Barcode) ? null : Barcode,
            IsTaxInclusive);

        int productId;
        if (IsEditing && SelectedProduct is not null)
        {
            await productService.UpdateAsync(SelectedProduct.Id, dto, ct);
            productId = SelectedProduct.Id;
            SuccessMessage = "Product updated.";
        }
        else
        {
            productId = await productService.CreateAsync(dto, ct);
            SuccessMessage = "Product created.";
        }

        // Save enterprise tax mapping if both group and HSN are selected
        if (productId > 0 && SelectedTaxGroup is not null && SelectedHSNCode is not null)
        {
            await taxGroupService.SetProductMappingAsync(
                new ProductTaxMappingDto(productId, SelectedTaxGroup.Id, SelectedHSNCode.Id, OverrideAllowed), ct);
        }
        else if (productId > 0 && SelectedTaxGroup is null)
        {
            // Clear mapping if group was deselected
            await taxGroupService.RemoveProductMappingAsync(productId, ct);
        }

        await ReloadProductsAsync(ct);
        SelectedProduct = null;
        ResetForm(clearMessages: false);
    });

    [RelayCommand]
    private Task ToggleActiveAsync(Product? product) => RunAsync(async ct =>
    {
        if (product is null) return;
        await productService.ToggleActiveAsync(product.Id, ct);
        await ReloadProductsAsync(ct, product.Id);
        SuccessMessage = "Status toggled.";
    });

    [RelayCommand(CanExecute = nameof(CanManageVariants))]
    private void ManageVariants()
    {
        if (SelectedProduct is null) return;
        productContextHolder.SelectedProduct = SelectedProduct;
        navigationService.NavigateTo("VariantManagement");
    }

    private bool CanManageVariants() => SelectedProduct is not null;

    [RelayCommand]
    private void Search() => ApplyFilters();

    [RelayCommand]
    private void ExportCsv()
    {
        if (Products.Count == 0) return;
        if (CsvExporter.Export(Products, "Products.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    [RelayCommand]
    private void SetStockFilter(string status)
    {
        FilterStockStatus = status;
        ApplyFilters();
    }

    partial void OnFilterCategoryChanged(Category? value) => ApplyFilters();

    private void ApplyFilters()
    {
        IEnumerable<Product> query = _allProducts;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (p.Barcode is not null && p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (FilterCategory is not null)
            query = query.Where(p => p.CategoryId == FilterCategory.Id);

        query = FilterStockStatus switch
        {
            "In Stock" => query.Where(p => p.Quantity > 0 && !p.IsLowStock),
            "Low Stock" => query.Where(p => p.IsLowStock),
            "Out of Stock" => query.Where(p => p.Quantity == 0),
            _ => query
        };

        var list = query.ToList();
        Products = new ObservableCollection<Product>(list);
        FilterCountText = (FilterStockStatus == "All" && FilterCategory is null && string.IsNullOrWhiteSpace(SearchText))
            ? string.Empty
            : $"{list.Count} of {_allProducts.Count}";
    }

    private void ClearMappingSelection()
    {
        SelectedTaxGroup = null;
        SelectedHSNCode = null;
        OverrideAllowed = false;
    }

    private async Task ReloadProductsAsync(CancellationToken ct, int? selectedProductId = null)
    {
        var productsTask = productService.GetAllAsync(ct);
        var taxesTask = productService.GetActiveTaxesAsync(ct);
        var groupsTask = taxGroupService.GetActiveGroupsAsync(ct);
        var codesTask = taxGroupService.GetActiveHSNCodesAsync(ct);
        var categoriesTask = productService.GetActiveCategoriesAsync(ct);
        var brandsTask = productService.GetActiveBrandsAsync(ct);
        var vendorsTask = productService.GetActiveVendorsAsync(ct);

        await Task.WhenAll(
            productsTask,
            taxesTask,
            groupsTask,
            codesTask,
            categoriesTask,
            brandsTask,
            vendorsTask);

        Products = new ObservableCollection<Product>(productsTask.Result);
        _allProducts = [.. productsTask.Result];
        Taxes = new ObservableCollection<TaxMaster>(taxesTask.Result);
        TaxGroups = new ObservableCollection<TaxGroup>(groupsTask.Result);
        HSNCodes = new ObservableCollection<HSNCode>(codesTask.Result);
        Categories = new ObservableCollection<Category>(categoriesTask.Result);
        Brands = new ObservableCollection<Brand>(brandsTask.Result);
        Vendors = new ObservableCollection<Vendor>(vendorsTask.Result);

        ApplyFilters();

        SelectedProduct = selectedProductId.HasValue
            ? Products.FirstOrDefault(p => p.Id == selectedProductId.Value)
            : null;
    }
}

