using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Modules.Tax.ViewModels;

public partial class TaxManagementViewModel(
    ITaxService taxService,
    ITaxGroupService taxGroupService,
    IRegionalSettingsService regional) : BaseViewModel
{
    public string CurrencySymbol => regional.CurrencySymbol;

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Tab Navigation
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Tab 0 â€” Tax Rates (TaxMaster â€” simple quick-setup rates)
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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
        DeleteRateCommand.NotifyCanExecuteChanged();

        if (value is null)
        {
            ResetRateForm(clearMessages: false);
            return;
        }

        TaxName = value.TaxName;
        SlabPercent = value.SlabPercent.ToString("G");
        IsEditing = true;
        ClearMessages();
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedTax = null;
        ResetRateForm(clearMessages: true);
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
        var successMessage = IsEditing && SelectedTax is not null
            ? "Tax rate updated."
            : "Tax rate created.";

        if (IsEditing && SelectedTax is not null)
        {
            await taxService.UpdateAsync(SelectedTax.Id, dto, ct);
        }
        else
        {
            await taxService.CreateAsync(dto, ct);
        }

        await ReloadRatesAsync(ct);
        SelectedTax = null;
        ResetRateForm(clearMessages: false);
        SuccessMessage = successMessage;
    });

    [RelayCommand(CanExecute = nameof(CanDeleteRate))]
    private Task DeleteRateAsync() => RunAsync(async ct =>
    {
        if (SelectedTax is null) return;
        await taxService.DeleteAsync(SelectedTax.Id, ct);
        await ReloadRatesAsync(ct);
        SelectedTax = null;
        ResetRateForm(clearMessages: false);
        SuccessMessage = "Tax rate deleted.";
    });

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Tab 1 â€” Tax Groups & Slabs
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [ObservableProperty]
    public partial ObservableCollection<TaxGroup> TaxGroups { get; set; } = [];

    [ObservableProperty]
    public partial TaxGroup? SelectedGroup { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<TaxSlab> GroupSlabs { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SlabActionText))]
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
        AddSlabCommand.NotifyCanExecuteChanged();

        if (value is null)
        {
            ResetGroupForm(clearMessages: false);
            return;
        }

        GroupName = value.Name;
        GroupDescription = value.Description ?? string.Empty;
        GroupSlabs = new ObservableCollection<TaxSlab>(value.Slabs.OrderBy(s => s.PriceFrom));
        SelectedSlab = null;
        ResetSlabForm(clearMessages: false);
        ClearMessages();
    }

    partial void OnSelectedSlabChanged(TaxSlab? value)
    {
        DeleteSlabCommand.NotifyCanExecuteChanged();

        if (value is null)
        {
            ResetSlabForm(clearMessages: false);
            return;
        }

        SlabGST = value.GSTPercent.ToString("G");
        SlabPriceFrom = value.PriceFrom.ToString("G");
        SlabPriceTo = value.PriceTo >= TaxSlab.MaxPrice
            ? string.Empty
            : value.PriceTo.ToString("G");
        ClearMessages();
    }

    public string SlabActionText => SelectedSlab is null ? "Add Slab" : "Update Slab";

    [RelayCommand]
    private void ClearGroupForm()
    {
        SelectedGroup = null;
        ResetGroupForm(clearMessages: true);
    }

    [RelayCommand]
    private Task SaveGroupAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(GroupName), "Group name is required.")))
            return;

        var dto = new TaxGroupDto(GroupName.Trim(),
            string.IsNullOrWhiteSpace(GroupDescription) ? null : GroupDescription.Trim());
        var successMessage = SelectedGroup is not null
            ? "Tax group updated."
            : "Tax group created.";

        if (SelectedGroup is not null)
        {
            await taxGroupService.UpdateGroupAsync(SelectedGroup.Id, dto, ct);
        }
        else
        {
            await taxGroupService.CreateGroupAsync(dto, ct);
        }

        await ReloadGroupsAsync(ct);
        SelectedGroup = null;
        ResetGroupForm(clearMessages: false);
        SuccessMessage = successMessage;
    });

    [RelayCommand]
    private Task ToggleGroupActiveAsync(TaxGroup? group) => RunAsync(async ct =>
    {
        if (group is null) return;
        var selectedGroupId = SelectedGroup?.Id == group.Id ? group.Id : SelectedGroup?.Id;
        var statusMessage = $"Group '{group.Name}' {(group.IsActive ? "deactivated" : "activated")}.";
        await taxGroupService.ToggleGroupActiveAsync(group.Id, ct);
        await ReloadGroupsAsync(ct, selectedGroupId ?? group.Id);
        SuccessMessage = statusMessage;
    });

    // â”€â”€ Slab management â”€â”€

    [RelayCommand]
    private void ClearSlabForm()
    {
        SelectedSlab = null;
        ResetSlabForm(clearMessages: true);
    }

    [RelayCommand(CanExecute = nameof(CanAddSlab))]
    private Task AddSlabAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (SelectedGroup is null)
        {
            ErrorMessage = "Select a tax group first.";
            return;
        }

        if (!Validate(v => v
            .Rule(decimal.TryParse(SlabGST, out var g) && g >= 0 && g <= 100, "GST % must be 0â€“100.")
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
            regional.Now, null);
        var successMessage = SelectedSlab is not null ? "Slab updated." : "Slab added.";
        var selectedGroupId = SelectedGroup.Id;

        if (SelectedSlab is not null)
        {
            await taxGroupService.UpdateSlabAsync(SelectedSlab.Id, dto, ct);
        }
        else
        {
            await taxGroupService.CreateSlabAsync(dto, ct);
        }

        await ReloadGroupsAsync(ct, selectedGroupId);
        SelectedSlab = null;
        ResetSlabForm(clearMessages: false);
        SuccessMessage = successMessage;
    });

    [RelayCommand(CanExecute = nameof(CanDeleteSlab))]
    private Task DeleteSlabAsync() => RunAsync(async ct =>
    {
        if (SelectedSlab is null) return;
        var selectedGroupId = SelectedGroup?.Id ?? SelectedSlab.TaxGroupId;
        await taxGroupService.DeleteSlabAsync(SelectedSlab.Id, ct);
        await ReloadGroupsAsync(ct, selectedGroupId);
        SelectedSlab = null;
        ResetSlabForm(clearMessages: false);
        SuccessMessage = "Slab deleted.";
    });

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Tab 2 â€” HSN Codes
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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
        if (value is null)
        {
            ResetHSNForm(clearMessages: false);
            return;
        }

        HSNCodeValue = value.Code;
        HSNDescription = value.Description;
        HSNCategory = value.Category;
        ClearMessages();
    }

    partial void OnHSNCodeValueChanged(string value)
    {
        var normalizedValue = NormalizeHsnCode(value);
        if (!string.Equals(value, normalizedValue, StringComparison.Ordinal))
            HSNCodeValue = normalizedValue;
    }

    [RelayCommand]
    private void ClearHSNForm()
    {
        SelectedHSN = null;
        ResetHSNForm(clearMessages: true);
    }

    [RelayCommand]
    private Task SaveHSNAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        var normalizedCode = NormalizeHsnCode(HSNCodeValue);

        if (!Validate(v => v
            .Rule(!string.IsNullOrWhiteSpace(normalizedCode), "HSN code is required.")
            .Rule(normalizedCode.Length is >= 4 and <= 8, "HSN code must be 4-8 digits.")
            .Rule(!string.IsNullOrWhiteSpace(HSNDescription), "Description is required.")))
            return;

        HSNCodeValue = normalizedCode;
        var dto = new HSNCodeDto(normalizedCode, HSNDescription.Trim(), HSNCategory);
        var successMessage = SelectedHSN is not null
            ? "HSN code updated."
            : "HSN code created.";

        if (SelectedHSN is not null)
        {
            await taxGroupService.UpdateHSNCodeAsync(SelectedHSN.Id, dto, ct);
        }
        else
        {
            await taxGroupService.CreateHSNCodeAsync(dto, ct);
        }

        await ReloadHSNAsync(ct);
        SelectedHSN = null;
        ResetHSNForm(clearMessages: false);
        SuccessMessage = successMessage;
    });

    [RelayCommand]
    private Task ToggleHSNActiveAsync(HSNCode? hsnCode) => RunAsync(async ct =>
    {
        if (hsnCode is null) return;
        var selectedHsnId = SelectedHSN?.Id == hsnCode.Id ? hsnCode.Id : SelectedHSN?.Id;
        var statusMessage = $"HSN '{hsnCode.Code}' {(hsnCode.IsActive ? "deactivated" : "activated")}.";
        await taxGroupService.ToggleHSNActiveAsync(hsnCode.Id, ct);
        await ReloadHSNAsync(ct, selectedHsnId ?? hsnCode.Id);
        SuccessMessage = statusMessage;
    });

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Load
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await Task.WhenAll(
            ReloadRatesAsync(ct),
            ReloadGroupsAsync(ct),
            ReloadHSNAsync(ct));
    });

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Reload helpers
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    private async Task ReloadRatesAsync(CancellationToken ct, int? selectedTaxId = null)
    {
        var taxes = await taxService.GetAllAsync(ct);
        Taxes = new ObservableCollection<TaxMaster>(taxes);
        SelectedTax = selectedTaxId.HasValue
            ? Taxes.FirstOrDefault(t => t.Id == selectedTaxId.Value)
            : null;
    }

    private async Task ReloadGroupsAsync(CancellationToken ct, int? selectedGroupId = null)
    {
        var groups = await taxGroupService.GetAllGroupsAsync(ct);
        TaxGroups = new ObservableCollection<TaxGroup>(groups);
        SelectedGroup = selectedGroupId.HasValue
            ? TaxGroups.FirstOrDefault(g => g.Id == selectedGroupId.Value)
            : null;
    }

    private async Task ReloadHSNAsync(CancellationToken ct, int? selectedHsnId = null)
    {
        var codes = await taxGroupService.GetAllHSNCodesAsync(ct);
        HSNCodes = new ObservableCollection<HSNCode>(codes);
        SelectedHSN = selectedHsnId.HasValue
            ? HSNCodes.FirstOrDefault(code => code.Id == selectedHsnId.Value)
            : null;
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private void ResetRateForm(bool clearMessages)
    {
        TaxName = string.Empty;
        SlabPercent = string.Empty;
        IsEditing = false;

        if (clearMessages)
            ClearMessages();
    }

    private void ResetGroupForm(bool clearMessages)
    {
        GroupName = string.Empty;
        GroupDescription = string.Empty;
        GroupSlabs = [];
        SelectedSlab = null;
        ResetSlabForm(clearMessages: false);

        if (clearMessages)
            ClearMessages();
    }

    private void ResetSlabForm(bool clearMessages)
    {
        SlabGST = string.Empty;
        SlabPriceFrom = string.Empty;
        SlabPriceTo = string.Empty;

        if (clearMessages)
            ClearMessages();
    }

    private void ResetHSNForm(bool clearMessages)
    {
        HSNCodeValue = string.Empty;
        HSNDescription = string.Empty;
        HSNCategory = HSNCategory.Garments;

        if (clearMessages)
            ClearMessages();
    }

    private static string NormalizeHsnCode(string? value) =>
        new string((value ?? string.Empty)
            .Where(char.IsDigit)
            .Take(8)
            .ToArray());

    private bool CanDeleteRate() => SelectedTax is not null;

    private bool CanAddSlab() => SelectedGroup is not null;

    private bool CanDeleteSlab() => SelectedSlab is not null;
}

