using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Products.ViewModels;

/// <summary>
/// Manages variants (size × colour combinations) for a single product.
/// Opened from Product Management when a product is selected.
/// </summary>
public partial class VariantManagementViewModel(
    IProductVariantService variantService,
    IProductService productService) : BaseViewModel
{
    // ── Product context ──

    [ObservableProperty]
    public partial Product? Product { get; set; }

    // ── Variants list ──

    [ObservableProperty]
    public partial ObservableCollection<ProductVariant> Variants { get; set; } = [];

    [ObservableProperty]
    public partial ProductVariant? SelectedVariant { get; set; }

    // ── Master lists ──

    [ObservableProperty]
    public partial ObservableCollection<ProductSize> Sizes { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Colour> Colours { get; set; } = [];

    // ── Form fields ──

    [ObservableProperty]
    public partial ProductSize? SelectedSize { get; set; }

    [ObservableProperty]
    public partial Colour? SelectedColour { get; set; }

    [ObservableProperty]
    public partial string Barcode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Quantity { get; set; } = "0";

    [ObservableProperty]
    public partial string AdditionalPrice { get; set; } = "0";

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    // ── Bulk creation ──

    [ObservableProperty]
    public partial ObservableCollection<SelectableItem<ProductSize>> BulkSizes { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<SelectableItem<Colour>> BulkColours { get; set; } = [];

    // ── Selection handling ──

    partial void OnSelectedVariantChanged(ProductVariant? value)
    {
        if (value is null) return;
        SelectedSize = Sizes.FirstOrDefault(s => s.Id == value.SizeId);
        SelectedColour = Colours.FirstOrDefault(c => c.Id == value.ColourId);
        Barcode = value.Barcode ?? string.Empty;
        Quantity = value.Quantity.ToString();
        AdditionalPrice = value.AdditionalPrice.ToString("G");
        IsEditing = true;
        ClearMessages();
    }

    // ── Load ──

    public async Task InitializeAsync(Product product, CancellationToken ct = default)
    {
        Product = product;
        await LoadDataAsync(ct);
    }

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct => await LoadDataAsync(ct));

    private async Task LoadDataAsync(CancellationToken ct)
    {
        if (Product is null) return;

        var variants = await variantService.GetByProductAsync(Product.Id, ct);
        Variants = new ObservableCollection<ProductVariant>(variants);

        var sizes = await productService.GetSizesAsync(ct);
        Sizes = new ObservableCollection<ProductSize>(sizes);

        var colours = await productService.GetColoursAsync(ct);
        Colours = new ObservableCollection<Colour>(colours);

        BulkSizes = new ObservableCollection<SelectableItem<ProductSize>>(
            sizes.Select(s => new SelectableItem<ProductSize>(s, s.Name)));
        BulkColours = new ObservableCollection<SelectableItem<Colour>>(
            colours.Select(c => new SelectableItem<Colour>(c, c.Name)));
    }

    // ── CRUD ──

    [RelayCommand]
    private void ClearForm()
    {
        SelectedVariant = null;
        SelectedSize = null;
        SelectedColour = null;
        Barcode = string.Empty;
        Quantity = "0";
        AdditionalPrice = "0";
        IsEditing = false;
        ClearMessages();
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        if (Product is null) return;

        if (!Validate(v => v
            .Rule(SelectedSize is not null, "Size is required.")
            .Rule(SelectedColour is not null, "Colour is required.")
            .Rule(int.TryParse(Quantity, out var q) && q >= 0, "Quantity must be a non-negative number.")
            .Rule(decimal.TryParse(AdditionalPrice, out _), "Additional price must be a valid number.")))
            return;

        var dto = new ProductVariantDto(
            Product.Id,
            SelectedSize!.Id,
            SelectedColour!.Id,
            string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim(),
            int.Parse(Quantity),
            decimal.Parse(AdditionalPrice));

        if (IsEditing && SelectedVariant is not null)
        {
            await variantService.UpdateAsync(SelectedVariant.Id, dto, ct);
            SuccessMessage = "Variant updated.";
        }
        else
        {
            await variantService.CreateAsync(dto, ct);
            SuccessMessage = "Variant created.";
        }

        await LoadDataAsync(ct);
        ClearForm();
    });

    [RelayCommand]
    private Task ToggleActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedVariant is null) return;
        await variantService.ToggleActiveAsync(SelectedVariant.Id, ct);
        SuccessMessage = $"Variant {(SelectedVariant.IsActive ? "deactivated" : "activated")}.";
        await LoadDataAsync(ct);
    });

    [RelayCommand]
    private Task DeleteAsync() => RunAsync(async ct =>
    {
        if (SelectedVariant is null) return;
        await variantService.DeleteAsync(SelectedVariant.Id, ct);
        SuccessMessage = "Variant deleted.";
        await LoadDataAsync(ct);
        ClearForm();
    });

    [RelayCommand]
    private Task BulkCreateAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        if (Product is null) return;

        var selectedSizes = BulkSizes.Where(s => s.IsSelected).Select(s => s.Item.Id).ToList();
        var selectedColours = BulkColours.Where(c => c.IsSelected).Select(c => c.Item.Id).ToList();

        if (!Validate(v => v
            .Rule(selectedSizes.Count > 0, "Select at least one size.")
            .Rule(selectedColours.Count > 0, "Select at least one colour.")))
            return;

        await variantService.BulkCreateAsync(Product.Id, selectedSizes, selectedColours, ct);

        var total = selectedSizes.Count * selectedColours.Count;
        SuccessMessage = $"Bulk created {total} variant combinations (duplicates skipped).";
        await LoadDataAsync(ct);
    });

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}

/// <summary>Wrapper for multi-select checkboxes in bulk creation.</summary>
public partial class SelectableItem<T>(T item, string displayName) : ObservableObject
{
    public T Item { get; } = item;
    public string DisplayName { get; } = displayName;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
