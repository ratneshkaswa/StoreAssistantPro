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

    // ── Enterprise GST dropdowns ──

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

    // ── Category & Brand dropdowns ──

    [ObservableProperty]
    public partial ObservableCollection<Category> Categories { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Brand> Brands { get; set; } = [];

    [ObservableProperty]
    public partial Category? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial Brand? SelectedBrand { get; set; }

    // ── Vendor dropdown ──

    [ObservableProperty]
    public partial ObservableCollection<Vendor> Vendors { get; set; } = [];

    [ObservableProperty]
    public partial Vendor? SelectedVendor { get; set; }

    // ── Form fields ──

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
        if (value is null) return;
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

        LoadMappingForProductCommand.ExecuteAsync(value.Id);
    }

    [RelayCommand]
    private Task LoadMappingForProductAsync(int productId) => RunLoadAsync(async ct =>
    {
        var mapping = await taxGroupService.GetMappingByProductAsync(productId, ct);
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
        var products = await productService.GetAllAsync(ct);
        Products = new ObservableCollection<Product>(products);

        var taxes = await productService.GetActiveTaxesAsync(ct);
        Taxes = new ObservableCollection<TaxMaster>(taxes);

        var groups = await taxGroupService.GetActiveGroupsAsync(ct);
        TaxGroups = new ObservableCollection<TaxGroup>(groups);

        var codes = await taxGroupService.GetActiveHSNCodesAsync(ct);
        HSNCodes = new ObservableCollection<HSNCode>(codes);

        var categories = await productService.GetActiveCategoriesAsync(ct);
        Categories = new ObservableCollection<Category>(categories);

        var brands = await productService.GetActiveBrandsAsync(ct);
        Brands = new ObservableCollection<Brand>(brands);

        var vendors = await productService.GetActiveVendorsAsync(ct);
        Vendors = new ObservableCollection<Vendor>(vendors);
    });

    [RelayCommand]
    private void NewProduct()
    {
        SelectedProduct = null;
        ProductName = string.Empty;
        SelectedProductType = ProductType.Readymade;
        SelectedUnit = ProductUnit.Piece;
        SelectedTax = null;
        SelectedTaxGroup = null;
        SelectedHSNCode = null;
        OverrideAllowed = false;
        SelectedCategory = null;
        SelectedBrand = null;
        SelectedVendor = null;
        SupportsColour = true;
        SupportsSize = true;
        SupportsPattern = false;
        SupportsType = false;
        IsEditing = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
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

        await LoadAsync();
        NewProduct();
    });

    [RelayCommand]
    private Task ToggleActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedProduct is null) return;
        await productService.ToggleActiveAsync(SelectedProduct.Id, ct);
        await LoadAsync();
        SuccessMessage = "Status toggled.";
    });

    /// <summary>
    /// Callback set by code-behind to open the Variant Management dialog
    /// for the selected product. Avoids direct view references in the ViewModel.
    /// </summary>
    public Action<Product>? OpenVariantsDialog { get; set; }

    [RelayCommand]
    private void ManageVariants()
    {
        if (SelectedProduct is null) return;
        OpenVariantsDialog?.Invoke(SelectedProduct);
    }
}
