using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Firm.Services;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Settings.Services;
using StoreAssistantPro.Modules.Tax.Services;
using StoreAssistantPro.Modules.Vendors.Services;

namespace StoreAssistantPro.Modules.Startup.ViewModels;

public partial class SetupWizardViewModel(
    IFirmService firmService,
    ITaxService taxService,
    IVendorService vendorService,
    IProductService productService,
    ISystemSettingsService settingsService) : BaseViewModel
{
    public const int TotalSteps = 4;

    // ── Step navigation ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(IsStep4))]
    [NotifyPropertyChangedFor(nameof(StepDisplay))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(IsLastStep))]
    [NotifyPropertyChangedFor(nameof(NextButtonText))]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    public partial int CurrentStep { get; set; } = 1;

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;
    public bool CanGoBack => CurrentStep > 1;
    public bool IsLastStep => CurrentStep == TotalSteps;
    public string StepDisplay => $"Step {CurrentStep} of {TotalSteps}";
    public string NextButtonText => IsLastStep ? "✅ Finish Setup" : "Next ▶";

    public string StepTitle => CurrentStep switch
    {
        1 => "📋 Step 1 — Firm Details",
        2 => "💰 Step 2 — Tax Slabs",
        3 => "🏭 Step 3 — Vendors",
        4 => "📦 Step 4 — Products",
        _ => string.Empty
    };

    public Action<bool?>? RequestClose { get; set; }

    // ═══════════════════════════════════════════════════════════════
    //  Step 1 — Firm Setup
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial string FirmName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FirmAddress { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FirmPhone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FirmEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FirmGSTIN { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool FirmSaved { get; set; }

    // ═══════════════════════════════════════════════════════════════
    //  Step 2 — Tax Setup
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<TaxMaster> Taxes { get; set; } = [];

    [ObservableProperty]
    public partial string TaxName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SlabPercent { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════════
    //  Step 3 — Vendor Setup (optional — user can skip)
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<Vendor> Vendors { get; set; } = [];

    [ObservableProperty]
    public partial string VendorName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string VendorPhone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string VendorCity { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════════
    //  Step 4 — Product Setup (optional — user can skip)
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<TaxMaster> ProductTaxes { get; set; } = [];

    [ObservableProperty]
    public partial string ProductName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TaxMaster? SelectedProductTax { get; set; }

    // ═══════════════════════════════════════════════════════════════
    //  Load
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var firm = await firmService.GetFirmAsync(ct);
        if (firm is not null)
        {
            FirmName = firm.FirmName;
            FirmAddress = firm.Address;
            FirmPhone = firm.Phone;
            FirmEmail = firm.Email;
            FirmGSTIN = firm.GSTNumber ?? string.Empty;
        }

        await ReloadTaxesAsync(ct);
        await ReloadVendorsAsync(ct);
        await ReloadProductsAsync(ct);
    });

    // ═══════════════════════════════════════════════════════════════
    //  Navigation — Next / Back
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private Task NextAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!ValidateCurrentStep())
            return;

        if (IsLastStep)
        {
            await settingsService.MarkSetupCompletedAsync(ct);
            RequestClose?.Invoke(true);
            return;
        }

        CurrentStep++;
        ClearMessages();

        // Pre-load tax dropdown for product step
        if (CurrentStep == 4)
        {
            var taxes = await taxService.GetActiveAsync(ct);
            ProductTaxes = new ObservableCollection<TaxMaster>(taxes);
        }
    });

    [RelayCommand]
    private void Back()
    {
        if (!CanGoBack) return;
        CurrentStep--;
        ClearMessages();
    }

    private bool ValidateCurrentStep()
    {
        return CurrentStep switch
        {
            // Firm name is mandatory
            1 => Validate(v => v
                .Rule(FirmSaved, "Save firm details before proceeding.")),
            // At least one tax slab
            2 => Validate(v => v
                .Rule(Taxes.Count > 0, "Add at least one tax slab before proceeding.")),
            // Vendors — optional, always pass
            3 => true,
            // Products — optional, always pass
            4 => true,
            _ => true
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Step 1 — Save Firm
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private Task SaveFirmAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(FirmName), "Firm name is required.")))
            return;

        var dto = new FirmUpdateDto(
            FirmName.Trim(), FirmAddress.Trim(), string.Empty, string.Empty,
            FirmPhone.Trim(), FirmEmail.Trim(),
            string.IsNullOrWhiteSpace(FirmGSTIN) ? null : FirmGSTIN.Trim(),
            null, 4, 3, "₹", "dd/MM/yyyy", "Indian");

        await firmService.UpdateFirmAsync(dto, ct);
        FirmSaved = true;
        SuccessMessage = "Firm details saved.";
    });

    // ═══════════════════════════════════════════════════════════════
    //  Step 2 — Add Tax
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private Task AddTaxAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v
            .Rule(!string.IsNullOrWhiteSpace(TaxName), "Tax name is required.")
            .Rule(decimal.TryParse(SlabPercent, out var pct) && pct >= 0 && pct <= 100,
                  "Slab % must be a number between 0 and 100.")))
            return;

        await taxService.CreateAsync(new TaxDto(TaxName.Trim(), decimal.Parse(SlabPercent)), ct);
        SuccessMessage = $"Tax '{TaxName.Trim()}' added.";
        TaxName = string.Empty;
        SlabPercent = string.Empty;
        await ReloadTaxesAsync(ct);
    });

    // ═══════════════════════════════════════════════════════════════
    //  Step 3 — Add Vendor
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private Task AddVendorAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(VendorName), "Vendor name is required.")))
            return;

        var dto = new VendorDto(
            VendorName.Trim(), null,
            string.IsNullOrWhiteSpace(VendorPhone) ? null : VendorPhone.Trim(),
            null, null, null,
            string.IsNullOrWhiteSpace(VendorCity) ? null : VendorCity.Trim(),
            null, null, null, null, null, null, 0m, 0m, null);

        await vendorService.CreateAsync(dto, ct);
        SuccessMessage = $"Vendor '{VendorName.Trim()}' added.";
        VendorName = string.Empty;
        VendorPhone = string.Empty;
        VendorCity = string.Empty;
        await ReloadVendorsAsync(ct);
    });

    // ═══════════════════════════════════════════════════════════════
    //  Step 4 — Add Product
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private Task AddProductAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(ProductName), "Product name is required.")))
            return;

        var dto = new ProductDto(
            ProductName.Trim(), ProductType.Readymade, ProductUnit.Piece,
            SelectedProductTax?.Id, null, null, true, false, true, false);

        await productService.CreateAsync(dto, ct);
        SuccessMessage = $"Product '{ProductName.Trim()}' added.";
        ProductName = string.Empty;
        SelectedProductTax = null;
        await ReloadProductsAsync(ct);
    });

    // ═══════════════════════════════════════════════════════════════
    //  Reload helpers
    // ═══════════════════════════════════════════════════════════════

    private async Task ReloadTaxesAsync(CancellationToken ct)
    {
        var list = await taxService.GetAllAsync(ct);
        Taxes = new ObservableCollection<TaxMaster>(list);
    }

    private async Task ReloadVendorsAsync(CancellationToken ct)
    {
        var list = await vendorService.GetAllAsync(ct);
        Vendors = new ObservableCollection<Vendor>(list);
    }

    private async Task ReloadProductsAsync(CancellationToken ct)
    {
        var list = await productService.GetAllAsync(ct);
        Products = new ObservableCollection<Product>(list);
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
