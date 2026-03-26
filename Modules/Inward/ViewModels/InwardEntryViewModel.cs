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
    public string CurrencySymbol => regional.CurrencySymbol;

    // â”€â”€ Step tracking â”€â”€

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

    // â”€â”€ Step 1: Parcel count â”€â”€

    [ObservableProperty]
    public partial int ParcelCount { get; set; } = 1;

    public ObservableCollection<int> ParcelCounts { get; } =
        new(Enumerable.Range(1, 10));

    // â”€â”€ Step 2: Transport charges â”€â”€

    [ObservableProperty]
    public partial string TransportCharges { get; set; } = "0";

    // â”€â”€ Step 3: Parcels â”€â”€

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
        var vendorsTask = vendorService.GetActiveAsync(ct);
        var productsTask = productService.GetActiveAsync(ct);
        var coloursTask = productService.GetColoursAsync(ct);
        var sizesTask = productService.GetSizesAsync(ct);
        var patternsTask = productService.GetPatternsAsync(ct);
        var variantTypesTask = productService.GetVariantTypesAsync(ct);

        await Task.WhenAll(vendorsTask, productsTask, coloursTask, sizesTask, patternsTask, variantTypesTask);

        Vendors = new ObservableCollection<Vendor>(vendorsTask.Result);
        Products = new ObservableCollection<Product>(productsTask.Result);
        Colours = new ObservableCollection<Colour>(coloursTask.Result);
        Sizes = new ObservableCollection<ProductSize>(sizesTask.Result);
        Patterns = new ObservableCollection<ProductPattern>(patternsTask.Result);
        VariantTypes = new ObservableCollection<ProductVariantType>(variantTypesTask.Result);
    });

    [RelayCommand]
    private async Task NextAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (CurrentStep == 1)
        {
            if (ParcelCount < 1 || ParcelCount > 10)
            {
                ErrorMessage = "Select 1-10 parcels.";
                return;
            }
            CurrentStep = 2;
        }
        else if (CurrentStep == 2)
        {
            if (!decimal.TryParse(TransportCharges, out var charges) || charges < 0)
            {
                ErrorMessage = "Enter a valid transport charge (0 or more).";
                return;
            }

            await GenerateParcelsAsync();
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
        ? NextAsync()
        : SaveAsync();

    private async Task GenerateParcelsAsync()
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

            parcel.AddBlankRow();

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
            for (var rowIndex = 0; rowIndex < parcel.ProductRows.Count; rowIndex++)
            {
                var row = parcel.ProductRows[rowIndex];

                if (row.SelectedProduct is null)
                {
                    if (row.HasEnteredValues)
                    {
                        ErrorMessage = $"Select a product for parcel {parcel.ParcelNumber}, row {rowIndex + 1}.";
                        return;
                    }

                    continue;
                }

                if (!decimal.TryParse(row.Quantity, out var qty) || qty <= 0)
                {
                    ErrorMessage = $"Enter a valid quantity for parcel {parcel.ParcelNumber}, row {rowIndex + 1}.";
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
        ResetEntry();
    });

    private void ResetEntry()
    {
        CurrentStep = 1;
        ParcelCount = 1;
        TransportCharges = "0";
        Parcels = [];
        ParcelNumbers = [];
    }
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

    [RelayCommand]
    private void AddProductRow() => AddBlankRow();

    [RelayCommand(CanExecute = nameof(CanRemoveProductRow))]
    private void RemoveProductRow(ProductRowModel? row)
    {
        if (row is null)
        {
            return;
        }

        ProductRows.Remove(row);
        row.Owner = null;

        if (ProductRows.Count == 0)
        {
            AddBlankRow();
            return;
        }

        NotifyProductRowCommandStates();
    }

    internal void AddBlankRow()
    {
        var row = new ProductRowModel
        {
            Owner = this
        };

        ProductRows.Add(row);
        NotifyProductRowCommandStates();
    }

    private bool CanRemoveProductRow(ProductRowModel? row) =>
        row is not null && (ProductRows.Count > 1 || row.HasEnteredValues);

    internal void NotifyProductRowCommandStates() =>
        RemoveProductRowCommand.NotifyCanExecuteChanged();
}

/// <summary>UI model for a single product row within a parcel.</summary>
public partial class ProductRowModel : ObservableObject
{
    internal ParcelEntryModel? Owner { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedProduct))]
    [NotifyPropertyChangedFor(nameof(HasEnteredValues))]
    [NotifyPropertyChangedFor(nameof(CanSelectColour))]
    [NotifyPropertyChangedFor(nameof(CanSelectSize))]
    [NotifyPropertyChangedFor(nameof(CanSelectPattern))]
    [NotifyPropertyChangedFor(nameof(CanSelectVariantType))]
    public partial Product? SelectedProduct { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEnteredValues))]
    public partial string Quantity { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEnteredValues))]
    public partial Colour? SelectedColour { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEnteredValues))]
    public partial ProductSize? SelectedSize { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEnteredValues))]
    public partial ProductPattern? SelectedPattern { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEnteredValues))]
    public partial ProductVariantType? SelectedVariantType { get; set; }

    public bool HasSelectedProduct => SelectedProduct is not null;

    public bool CanSelectColour => SelectedProduct?.SupportsColour == true;

    public bool CanSelectSize => SelectedProduct?.SupportsSize == true;

    public bool CanSelectPattern => SelectedProduct?.SupportsPattern == true;

    public bool CanSelectVariantType => SelectedProduct?.SupportsType == true;

    public bool HasEnteredValues =>
        SelectedProduct is not null
        || !string.IsNullOrWhiteSpace(Quantity)
        || SelectedColour is not null
        || SelectedSize is not null
        || SelectedPattern is not null
        || SelectedVariantType is not null;

    partial void OnSelectedProductChanged(Product? value)
    {
        if (!CanSelectColour)
        {
            SelectedColour = null;
        }

        if (!CanSelectSize)
        {
            SelectedSize = null;
        }

        if (!CanSelectPattern)
        {
            SelectedPattern = null;
        }

        if (!CanSelectVariantType)
        {
            SelectedVariantType = null;
        }

        Owner?.NotifyProductRowCommandStates();
    }

    partial void OnQuantityChanged(string value) => Owner?.NotifyProductRowCommandStates();

    partial void OnSelectedColourChanged(Colour? value) => Owner?.NotifyProductRowCommandStates();

    partial void OnSelectedSizeChanged(ProductSize? value) => Owner?.NotifyProductRowCommandStates();

    partial void OnSelectedPatternChanged(ProductPattern? value) => Owner?.NotifyProductRowCommandStates();

    partial void OnSelectedVariantTypeChanged(ProductVariantType? value) => Owner?.NotifyProductRowCommandStates();
}


