using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Modules.Tax.ViewModels;

public partial class TaxManagementViewModel(
    ITaxService taxService,
    ITaxGroupService taxGroupService) : BaseViewModel
{
    // ═══════════════════════════════════════════════════════════════
    //  Tab Navigation
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTabRates))]
    [NotifyPropertyChangedFor(nameof(IsTabGroups))]
    [NotifyPropertyChangedFor(nameof(IsTabHSN))]
    public partial int ActiveTab { get; set; }

    public bool IsTabRates => ActiveTab == 0;
    public bool IsTabGroups => ActiveTab == 1;
    public bool IsTabHSN => ActiveTab == 2;

    [RelayCommand]
    private void SwitchTab(string tab)
    {
        ActiveTab = int.TryParse(tab, out var t) ? t : 0;
        ClearMessages();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Tab 0 — Tax Rates (TaxMaster — simple quick-setup rates)
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<TaxMaster> Taxes { get; set; } = [];

    [ObservableProperty]
    public partial TaxMaster? SelectedTax { get; set; }

    [ObservableProperty]
    public partial string TaxName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SlabPercent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    partial void OnSelectedTaxChanged(TaxMaster? value)
    {
        if (value is null) return;
        TaxName = value.TaxName;
        SlabPercent = value.SlabPercent.ToString("G");
        IsEditing = true;
        ClearMessages();
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedTax = null;
        TaxName = string.Empty;
        SlabPercent = string.Empty;
        IsEditing = false;
        ClearMessages();
    }

    [RelayCommand]
    private Task SaveRateAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v
            .Rule(!string.IsNullOrWhiteSpace(TaxName), "Tax name is required.")
            .Rule(decimal.TryParse(SlabPercent, out var pct) && pct >= 0 && pct <= 100,
                  "Slab % must be a number between 0 and 100.")))
            return;

        var dto = new TaxDto(TaxName.Trim(), decimal.Parse(SlabPercent));

        if (IsEditing && SelectedTax is not null)
        {
            await taxService.UpdateAsync(SelectedTax.Id, dto, ct);
            SuccessMessage = "Tax rate updated.";
        }
        else
        {
            await taxService.CreateAsync(dto, ct);
            SuccessMessage = "Tax rate created.";
        }

        await ReloadRatesAsync(ct);
        ClearForm();
    });

    [RelayCommand]
    private Task DeleteRateAsync() => RunAsync(async ct =>
    {
        if (SelectedTax is null) return;
        await taxService.DeleteAsync(SelectedTax.Id, ct);
        SuccessMessage = "Tax rate deleted.";
        await ReloadRatesAsync(ct);
        ClearForm();
    });

    // ═══════════════════════════════════════════════════════════════
    //  Tab 1 — Tax Groups & Slabs
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<TaxGroup> TaxGroups { get; set; } = [];

    [ObservableProperty]
    public partial TaxGroup? SelectedGroup { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<TaxSlab> GroupSlabs { get; set; } = [];

    [ObservableProperty]
    public partial TaxSlab? SelectedSlab { get; set; }

    // Group form fields
    [ObservableProperty]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GroupDescription { get; set; } = string.Empty;

    // Slab form fields
    [ObservableProperty]
    public partial string SlabGST { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SlabPriceFrom { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SlabPriceTo { get; set; } = string.Empty;

    partial void OnSelectedGroupChanged(TaxGroup? value)
    {
        ClearMessages();
        if (value is null)
        {
            GroupSlabs = [];
            return;
        }

        GroupName = value.Name;
        GroupDescription = value.Description ?? string.Empty;
        GroupSlabs = new ObservableCollection<TaxSlab>(value.Slabs);
    }

    [RelayCommand]
    private void ClearGroupForm()
    {
        SelectedGroup = null;
        GroupName = string.Empty;
        GroupDescription = string.Empty;
        GroupSlabs = [];
        ClearSlabForm();
        ClearMessages();
    }

    [RelayCommand]
    private Task SaveGroupAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(GroupName), "Group name is required.")))
            return;

        var dto = new TaxGroupDto(GroupName.Trim(),
            string.IsNullOrWhiteSpace(GroupDescription) ? null : GroupDescription.Trim());

        if (SelectedGroup is not null)
        {
            await taxGroupService.UpdateGroupAsync(SelectedGroup.Id, dto, ct);
            SuccessMessage = "Tax group updated.";
        }
        else
        {
            await taxGroupService.CreateGroupAsync(dto, ct);
            SuccessMessage = "Tax group created.";
        }

        await ReloadGroupsAsync(ct);
        ClearGroupForm();
    });

    [RelayCommand]
    private Task ToggleGroupActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedGroup is null) return;
        await taxGroupService.ToggleGroupActiveAsync(SelectedGroup.Id, ct);
        SuccessMessage = $"Group '{SelectedGroup.Name}' {(SelectedGroup.IsActive ? "deactivated" : "activated")}.";
        await ReloadGroupsAsync(ct);
    });

    // ── Slab management ──

    private void ClearSlabForm()
    {
        SelectedSlab = null;
        SlabGST = string.Empty;
        SlabPriceFrom = string.Empty;
        SlabPriceTo = string.Empty;
    }

    [RelayCommand]
    private Task AddSlabAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (SelectedGroup is null)
        {
            ErrorMessage = "Select a tax group first.";
            return;
        }

        if (!Validate(v => v
            .Rule(decimal.TryParse(SlabGST, out var g) && g >= 0 && g <= 100, "GST % must be 0–100.")
            .Rule(decimal.TryParse(SlabPriceFrom, out _), "Price From is required.")
            .Rule(string.IsNullOrWhiteSpace(SlabPriceTo) || decimal.TryParse(SlabPriceTo, out _), "Price To must be a number.")))
            return;

        var priceTo = string.IsNullOrWhiteSpace(SlabPriceTo)
            ? TaxSlab.MaxPrice
            : decimal.Parse(SlabPriceTo);

        var dto = new TaxSlabDto(
            SelectedGroup.Id,
            decimal.Parse(SlabGST),
            decimal.Parse(SlabPriceFrom),
            priceTo,
            DateTime.UtcNow, null);

        await taxGroupService.CreateSlabAsync(dto, ct);
        SuccessMessage = "Slab added.";

        // Reload group to pick up new slab
        await ReloadGroupsAsync(ct);
        ReselectGroup(SelectedGroup.Id);
        ClearSlabForm();
    });

    [RelayCommand]
    private Task DeleteSlabAsync() => RunAsync(async ct =>
    {
        if (SelectedSlab is null) return;
        await taxGroupService.DeleteSlabAsync(SelectedSlab.Id, ct);
        SuccessMessage = "Slab deleted.";
        await ReloadGroupsAsync(ct);
        if (SelectedGroup is not null)
            ReselectGroup(SelectedGroup.Id);
        ClearSlabForm();
    });

    // ═══════════════════════════════════════════════════════════════
    //  Tab 2 — HSN Codes
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<HSNCode> HSNCodes { get; set; } = [];

    [ObservableProperty]
    public partial HSNCode? SelectedHSN { get; set; }

    [ObservableProperty]
    public partial string HSNCodeValue { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HSNDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial HSNCategory HSNCategory { get; set; } = HSNCategory.Garments;

    public IReadOnlyList<HSNCategory> HSNCategories { get; } =
        Enum.GetValues<HSNCategory>();

    partial void OnSelectedHSNChanged(HSNCode? value)
    {
        ClearMessages();
        if (value is null) return;
        HSNCodeValue = value.Code;
        HSNDescription = value.Description;
        HSNCategory = value.Category;
    }

    [RelayCommand]
    private void ClearHSNForm()
    {
        SelectedHSN = null;
        HSNCodeValue = string.Empty;
        HSNDescription = string.Empty;
        HSNCategory = HSNCategory.Garments;
        ClearMessages();
    }

    [RelayCommand]
    private Task SaveHSNAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v
            .Rule(!string.IsNullOrWhiteSpace(HSNCodeValue), "HSN code is required.")
            .Rule(HSNCodeValue.Trim().Length is >= 4 and <= 8, "HSN code must be 4–8 digits.")
            .Rule(!string.IsNullOrWhiteSpace(HSNDescription), "Description is required.")))
            return;

        var dto = new HSNCodeDto(HSNCodeValue.Trim(), HSNDescription.Trim(), HSNCategory);

        if (SelectedHSN is not null)
        {
            await taxGroupService.UpdateHSNCodeAsync(SelectedHSN.Id, dto, ct);
            SuccessMessage = "HSN code updated.";
        }
        else
        {
            await taxGroupService.CreateHSNCodeAsync(dto, ct);
            SuccessMessage = "HSN code created.";
        }

        await ReloadHSNAsync(ct);
        ClearHSNForm();
    });

    [RelayCommand]
    private Task ToggleHSNActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedHSN is null) return;
        await taxGroupService.ToggleHSNActiveAsync(SelectedHSN.Id, ct);
        SuccessMessage = $"HSN '{SelectedHSN.Code}' {(SelectedHSN.IsActive ? "deactivated" : "activated")}.";
        await ReloadHSNAsync(ct);
    });

    // ═══════════════════════════════════════════════════════════════
    //  Load
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await ReloadRatesAsync(ct);
        await ReloadGroupsAsync(ct);
        await ReloadHSNAsync(ct);
    });

    // ═══════════════════════════════════════════════════════════════
    //  Reload helpers
    // ═══════════════════════════════════════════════════════════════

    private async Task ReloadRatesAsync(CancellationToken ct)
    {
        var taxes = await taxService.GetAllAsync(ct);
        Taxes = new ObservableCollection<TaxMaster>(taxes);
    }

    private async Task ReloadGroupsAsync(CancellationToken ct)
    {
        var groups = await taxGroupService.GetAllGroupsAsync(ct);
        TaxGroups = new ObservableCollection<TaxGroup>(groups);
    }

    private async Task ReloadHSNAsync(CancellationToken ct)
    {
        var codes = await taxGroupService.GetAllHSNCodesAsync(ct);
        HSNCodes = new ObservableCollection<HSNCode>(codes);
    }

    private void ReselectGroup(int groupId)
    {
        var group = TaxGroups.FirstOrDefault(g => g.Id == groupId);
        SelectedGroup = group;
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
