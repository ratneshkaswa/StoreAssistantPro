using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Brands.Services;
using StoreAssistantPro.Modules.Categories.Services;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Modules.Products.ViewModels;

public partial class ProductManagementViewModel(
    IProductService productService,
    ITaxGroupService taxGroupService) : BaseViewModel
{
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

        var dto = new ProductDto(
            ProductName, SelectedProductType, SelectedUnit,
            SelectedTax?.Id, SelectedCategory?.Id, SelectedBrand?.Id, SelectedVendor?.Id,
            SupportsColour, SupportsPattern,
            SupportsSize, SupportsType);

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

    /// <summary>
    /// Callback set by code-behind to open the Variant Management dialog
    /// for the selected product. Avoids direct view references in the ViewModel.
    /// </summary>
    public Action<Product>? OpenVariantsDialog { get; set; }

    [RelayCommand(CanExecute = nameof(CanManageVariants))]
    private void ManageVariants()
    {
        if (SelectedProduct is null) return;
        OpenVariantsDialog?.Invoke(SelectedProduct);
    }

    private bool CanManageVariants() => SelectedProduct is not null;

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
        Taxes = new ObservableCollection<TaxMaster>(taxesTask.Result);
        TaxGroups = new ObservableCollection<TaxGroup>(groupsTask.Result);
        HSNCodes = new ObservableCollection<HSNCode>(codesTask.Result);
        Categories = new ObservableCollection<Category>(categoriesTask.Result);
        Brands = new ObservableCollection<Brand>(brandsTask.Result);
        Vendors = new ObservableCollection<Vendor>(vendorsTask.Result);

        SelectedProduct = selectedProductId.HasValue
            ? Products.FirstOrDefault(p => p.Id == selectedProductId.Value)
            : null;
    }
}

