using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Inward.Services;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Vendors.Services;

namespace StoreAssistantPro.Modules.Inward.ViewModels;

public partial class InwardEntryViewModel(
    IInwardService inwardService,
    IVendorService vendorService,
    IProductService productService,
    IRegionalSettingsService regional) : BaseViewModel
{
    // ── Step tracking ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(IsLastStep))]
    public partial int CurrentStep { get; set; } = 1;

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool CanGoBack => CurrentStep > 1;
    public bool CanGoNext => CurrentStep < 3;
    public bool IsLastStep => CurrentStep == 3;

    // ── Step 1: Parcel count ──

    [ObservableProperty]
    public partial int ParcelCount { get; set; } = 1;

    public ObservableCollection<int> ParcelCounts { get; } =
        new(Enumerable.Range(1, 10));

    // ── Step 2: Transport charges ──

    [ObservableProperty]
    public partial string TransportCharges { get; set; } = "0";

    // ── Step 3: Parcels ──

    [ObservableProperty]
    public partial ObservableCollection<ParcelEntryModel> Parcels { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Vendor> Vendors { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Colour> Colours { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ProductSize> Sizes { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ProductPattern> Patterns { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ProductVariantType> VariantTypes { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<string> ParcelNumbers { get; set; } = [];

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var vendors = await vendorService.GetActiveAsync(ct);
        Vendors = new ObservableCollection<Vendor>(vendors);

        var products = await productService.GetActiveAsync(ct);
        Products = new ObservableCollection<Product>(products);

        var colours = await productService.GetColoursAsync(ct);
        Colours = new ObservableCollection<Colour>(colours);

        var sizes = await productService.GetSizesAsync(ct);
        Sizes = new ObservableCollection<ProductSize>(sizes);

        var patterns = await productService.GetPatternsAsync(ct);
        Patterns = new ObservableCollection<ProductPattern>(patterns);

        var variantTypes = await productService.GetVariantTypesAsync(ct);
        VariantTypes = new ObservableCollection<ProductVariantType>(variantTypes);
    });

    [RelayCommand]
    private void Next()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (CurrentStep == 1)
        {
            if (ParcelCount < 1 || ParcelCount > 10)
            {
                ErrorMessage = "Select 1–10 parcels.";
                return;
            }
            CurrentStep = 2;
        }
        else if (CurrentStep == 2)
        {
            if (!decimal.TryParse(TransportCharges, out var charges) || charges < 0)
            {
                ErrorMessage = "Enter a valid transport charge (≥ 0).";
                return;
            }

            GenerateParcels();
            CurrentStep = 3;
        }
    }

    [RelayCommand]
    private void Back()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        if (CurrentStep > 1) CurrentStep--;
    }

    [RelayCommand]
    private Task ConfirmStepAsync() => CurrentStep < 3
        ? Task.Run(() => Next())
        : SaveAsync();

    private async void GenerateParcels()
    {
        var date = regional.Now;
        ParcelNumbers = await inwardService.GenerateParcelNumbersAsync(date, ParcelCount);

        var parcels = new ObservableCollection<ParcelEntryModel>();
        var chargePerParcel = decimal.TryParse(TransportCharges, out var total)
            ? Math.Round(total / ParcelCount, 2) : 0m;

        for (var i = 0; i < ParcelCount; i++)
        {
            var parcel = new ParcelEntryModel
            {
                ParcelNumber = ParcelNumbers[i],
                TransportCharge = chargePerParcel
            };

            for (var j = 0; j < 3; j++)
                parcel.ProductRows.Add(new ProductRowModel());

            parcels.Add(parcel);
        }

        Parcels = parcels;
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        var parcelDtos = new List<InwardParcelDto>();
        foreach (var parcel in Parcels)
        {
            var productDtos = new List<InwardProductDto>();
            foreach (var row in parcel.ProductRows)
            {
                if (row.SelectedProduct is null) continue;

                if (!decimal.TryParse(row.Quantity, out var qty) || qty <= 0)
                {
                    ErrorMessage = $"Invalid quantity in parcel {parcel.ParcelNumber}.";
                    return;
                }

                productDtos.Add(new InwardProductDto(
                    row.SelectedProduct.Id, qty,
                    row.SelectedColour?.Id,
                    row.SelectedSize?.Id,
                    row.SelectedPattern?.Id,
                    row.SelectedVariantType?.Id));
            }

            if (productDtos.Count == 0)
            {
                ErrorMessage = $"Parcel {parcel.ParcelNumber} needs at least one product.";
                return;
            }

            parcelDtos.Add(new InwardParcelDto(
                parcel.SelectedVendor?.Id,
                parcel.TransportCharge,
                null,
                productDtos));
        }

        var dto = new InwardEntryDto(
            regional.Now,
            null,
            decimal.TryParse(TransportCharges, out var tc) ? tc : 0,
            null,
            parcelDtos);

        await inwardService.CreateAsync(dto, ct);
        SuccessMessage = "Inward entry saved successfully.";
    });
}

/// <summary>UI model for a single parcel entry in the wizard.</summary>
public partial class ParcelEntryModel : ObservableObject
{
    [ObservableProperty]
    public partial string ParcelNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Vendor? SelectedVendor { get; set; }

    [ObservableProperty]
    public partial decimal TransportCharge { get; set; }

    public ObservableCollection<ProductRowModel> ProductRows { get; } = [];
}

/// <summary>UI model for a single product row within a parcel.</summary>
public partial class ProductRowModel : ObservableObject
{
    [ObservableProperty]
    public partial Product? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial string Quantity { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Colour? SelectedColour { get; set; }

    [ObservableProperty]
    public partial ProductSize? SelectedSize { get; set; }

    [ObservableProperty]
    public partial ProductPattern? SelectedPattern { get; set; }

    [ObservableProperty]
    public partial ProductVariantType? SelectedVariantType { get; set; }
}
