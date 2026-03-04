using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Products.ViewModels;

public partial class ProductManagementViewModel(IProductService productService) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    [ObservableProperty]
    public partial Product? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<TaxMaster> Taxes { get; set; } = [];

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
        SupportsColour = value.SupportsColour;
        SupportsSize = value.SupportsSize;
        SupportsPattern = value.SupportsPattern;
        SupportsType = value.SupportsType;
        IsEditing = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var products = await productService.GetAllAsync(ct);
        Products = new ObservableCollection<Product>(products);

        var taxes = await productService.GetActiveTaxesAsync(ct);
        Taxes = new ObservableCollection<TaxMaster>(taxes);
    });

    [RelayCommand]
    private void NewProduct()
    {
        SelectedProduct = null;
        ProductName = string.Empty;
        SelectedProductType = ProductType.Readymade;
        SelectedUnit = ProductUnit.Piece;
        SelectedTax = null;
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
            SelectedTax?.Id, SupportsColour, SupportsPattern,
            SupportsSize, SupportsType);

        if (IsEditing && SelectedProduct is not null)
        {
            await productService.UpdateAsync(SelectedProduct.Id, dto, ct);
            SuccessMessage = "Product updated.";
        }
        else
        {
            await productService.CreateAsync(dto, ct);
            SuccessMessage = "Product created.";
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
}
