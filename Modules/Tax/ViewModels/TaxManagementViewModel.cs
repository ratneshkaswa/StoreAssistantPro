using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Modules.Tax.ViewModels;

public partial class TaxManagementViewModel(
    ITaxGroupService groupService,
    IProductService productService) : BaseViewModel
{
    /// <summary>Suppresses slab reload while populating form from a selected slab.</summary>
    private bool _suppressSlabReload;

    // ═══════════════════════════════════════════════════════════════
    //  Tab 1 — GST Groups
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<TaxGroup> Groups { get; set; } = [];

    [ObservableProperty]
    public partial TaxGroup? SelectedGroup { get; set; }

    [ObservableProperty]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GroupDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditingGroup { get; set; }

    partial void OnSelectedGroupChanged(TaxGroup? value)
    {
        if (value is null) return;
        GroupName = value.Name;
        GroupDescription = value.Description ?? string.Empty;
        IsEditingGroup = true;
        ClearMessages();
    }

    [RelayCommand]
    private void NewGroup()
    {
        SelectedGroup = null;
        GroupName = string.Empty;
        GroupDescription = string.Empty;
        IsEditingGroup = false;
        ClearMessages();
    }

    [RelayCommand]
    private Task SaveGroupAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;
        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(GroupName), "Group name is required.")))
            return;

        var dto = new TaxGroupDto(GroupName.Trim(), GroupDescription.Trim());

        if (IsEditingGroup && SelectedGroup is not null)
        {
            await groupService.UpdateGroupAsync(SelectedGroup.Id, dto, ct);
            SuccessMessage = "Group updated.";
        }
        else
        {
            await groupService.CreateGroupAsync(dto, ct);
            SuccessMessage = "Group created.";
        }

        await ReloadGroupsAsync(ct);
        NewGroup();
    });

    [RelayCommand]
    private Task ToggleGroupActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedGroup is null) return;
        await groupService.ToggleGroupActiveAsync(SelectedGroup.Id, ct);
        await ReloadGroupsAsync(ct);
        SuccessMessage = "Status toggled.";
    });

    // ═══════════════════════════════════════════════════════════════
    //  Tab 2 — GST Slabs
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<TaxSlab> Slabs { get; set; } = [];

    [ObservableProperty]
    public partial TaxSlab? SelectedSlab { get; set; }

    [ObservableProperty]
    public partial TaxGroup? SlabGroup { get; set; }

    [ObservableProperty]
    public partial string SlabGSTPercent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SlabPriceFrom { get; set; } = "0";

    [ObservableProperty]
    public partial string SlabPriceTo { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime SlabEffectiveFrom { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial DateTime? SlabEffectiveTo { get; set; }

    [ObservableProperty]
    public partial bool IsEditingSlab { get; set; }

    /// <summary>Auto-calculated display values.</summary>
    [ObservableProperty]
    public partial string SlabCGST { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SlabSGST { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SlabIGST { get; set; } = string.Empty;

    partial void OnSlabGSTPercentChanged(string value)
    {
        if (decimal.TryParse(value, out var rate) && rate >= 0 && rate <= 100)
        {
            SlabCGST = (rate / 2m).ToString("G");
            SlabSGST = (rate / 2m).ToString("G");
            SlabIGST = rate.ToString("G");
        }
        else
        {
            SlabCGST = string.Empty;
            SlabSGST = string.Empty;
            SlabIGST = string.Empty;
        }
    }

    partial void OnSelectedSlabChanged(TaxSlab? value)
    {
        if (value is null) return;
        _suppressSlabReload = true;
        try
        {
            SlabGroup = Groups.FirstOrDefault(g => g.Id == value.TaxGroupId);
            SlabGSTPercent = value.GSTPercent.ToString("G");
            SlabPriceFrom = value.PriceFrom.ToString("G");
            SlabPriceTo = value.PriceTo >= TaxSlab.MaxPrice ? string.Empty : value.PriceTo.ToString("G");
            SlabEffectiveFrom = value.EffectiveFrom;
            SlabEffectiveTo = value.EffectiveTo;
            IsEditingSlab = true;
            ClearMessages();
        }
        finally { _suppressSlabReload = false; }
    }

    partial void OnSlabGroupChanged(TaxGroup? value)
    {
        if (!_suppressSlabReload)
            _ = SafeLoadSlabsAsync();
    }

    private async Task SafeLoadSlabsAsync()
    {
        try { await LoadSlabsForGroupAsync(); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void NewSlab()
    {
        SelectedSlab = null;
        SlabGSTPercent = string.Empty;
        SlabPriceFrom = "0";
        SlabPriceTo = string.Empty;
        SlabEffectiveFrom = DateTime.Today;
        SlabEffectiveTo = null;
        IsEditingSlab = false;
        ClearMessages();
    }

    [RelayCommand]
    private Task SaveSlabAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        if (!Validate(v => v
            .Rule(SlabGroup is not null, "Select a tax group.")
            .Rule(decimal.TryParse(SlabGSTPercent, out var gstVal) && gstVal >= 0 && gstVal <= 100,
                  "GST% must be a number 0–100.")
            .Rule(decimal.TryParse(SlabPriceFrom, out var fromVal) && fromVal >= 0,
                  "Price From must be a non-negative number.")
            .Rule(string.IsNullOrWhiteSpace(SlabPriceTo) || decimal.TryParse(SlabPriceTo, out _),
                  "Price To must be a number or blank for no limit.")))
            return;

        var gst = decimal.Parse(SlabGSTPercent);
        var from = decimal.Parse(SlabPriceFrom);
        var to = string.IsNullOrWhiteSpace(SlabPriceTo) ? TaxSlab.MaxPrice : decimal.Parse(SlabPriceTo);

        var dto = new TaxSlabDto(SlabGroup!.Id, gst, from, to, SlabEffectiveFrom, SlabEffectiveTo);

        if (IsEditingSlab && SelectedSlab is not null)
        {
            await groupService.UpdateSlabAsync(SelectedSlab.Id, dto, ct);
            SuccessMessage = "Slab updated.";
        }
        else
        {
            await groupService.CreateSlabAsync(dto, ct);
            SuccessMessage = "Slab created.";
        }

        await LoadSlabsForGroupAsync();
        NewSlab();
    });

    [RelayCommand]
    private Task DeleteSlabAsync() => RunAsync(async ct =>
    {
        if (SelectedSlab is null) return;
        await groupService.DeleteSlabAsync(SelectedSlab.Id, ct);
        await LoadSlabsForGroupAsync();
        NewSlab();
        SuccessMessage = "Slab deleted.";
    });

    private async Task LoadSlabsForGroupAsync()
    {
        if (SlabGroup is null) { Slabs = []; return; }
        var slabs = await groupService.GetSlabsByGroupAsync(SlabGroup.Id);
        Slabs = new ObservableCollection<TaxSlab>(slabs);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Tab 3 — HSN Codes
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
    public partial HSNCategory HSNSelectedCategory { get; set; } = HSNCategory.Garments;

    [ObservableProperty]
    public partial bool IsEditingHSN { get; set; }

    public ObservableCollection<HSNCategory> HSNCategories { get; } =
        [HSNCategory.Garments, HSNCategory.Fabric, HSNCategory.Both];

    partial void OnSelectedHSNChanged(HSNCode? value)
    {
        if (value is null) return;
        HSNCodeValue = value.Code;
        HSNDescription = value.Description;
        HSNSelectedCategory = value.Category;
        IsEditingHSN = true;
        ClearMessages();
    }

    [RelayCommand]
    private void NewHSN()
    {
        SelectedHSN = null;
        HSNCodeValue = string.Empty;
        HSNDescription = string.Empty;
        HSNSelectedCategory = HSNCategory.Garments;
        IsEditingHSN = false;
        ClearMessages();
    }

    [RelayCommand]
    private Task SaveHSNAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        if (!Validate(v => v
            .Rule(!string.IsNullOrWhiteSpace(HSNCodeValue), "HSN code is required.")
            .Rule(HSNCodeValue.Trim().Length is >= 4 and <= 8, "HSN code must be 4–8 characters.")
            .Rule(!string.IsNullOrWhiteSpace(HSNDescription), "Description is required.")))
            return;

        var dto = new HSNCodeDto(HSNCodeValue.Trim(), HSNDescription.Trim(), HSNSelectedCategory);

        if (IsEditingHSN && SelectedHSN is not null)
        {
            await groupService.UpdateHSNCodeAsync(SelectedHSN.Id, dto, ct);
            SuccessMessage = "HSN code updated.";
        }
        else
        {
            await groupService.CreateHSNCodeAsync(dto, ct);
            SuccessMessage = "HSN code created.";
        }

        await ReloadHSNCodesAsync(ct);
        NewHSN();
    });

    [RelayCommand]
    private Task ToggleHSNActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedHSN is null) return;
        await groupService.ToggleHSNActiveAsync(SelectedHSN.Id, ct);
        await ReloadHSNCodesAsync(ct);
        SuccessMessage = "Status toggled.";
    });

    // ═══════════════════════════════════════════════════════════════
    //  Tab 4 — Tax Rules (Product → TaxGroup + HSN mapping)
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    [ObservableProperty]
    public partial Product? RuleProduct { get; set; }

    [ObservableProperty]
    public partial TaxGroup? RuleGroup { get; set; }

    [ObservableProperty]
    public partial HSNCode? RuleHSN { get; set; }

    [ObservableProperty]
    public partial bool RuleOverrideAllowed { get; set; }

    [ObservableProperty]
    public partial string RuleCurrentMapping { get; set; } = "No product selected";

    partial void OnRuleProductChanged(Product? value) => _ = SafeLoadMappingAsync();

    private async Task SafeLoadMappingAsync()
    {
        try { await LoadCurrentMappingAsync(); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    private async Task LoadCurrentMappingAsync()
    {
        if (RuleProduct is null) { RuleCurrentMapping = "No product selected"; return; }
        var mapping = await groupService.GetMappingByProductAsync(RuleProduct.Id);
        if (mapping is null)
        {
            RuleCurrentMapping = "No tax rule assigned";
            RuleGroup = null;
            RuleHSN = null;
            RuleOverrideAllowed = false;
        }
        else
        {
            RuleCurrentMapping = $"{mapping.TaxGroup?.Name} / {mapping.HSNCode?.Code}";
            RuleGroup = Groups.FirstOrDefault(g => g.Id == mapping.TaxGroupId);
            RuleHSN = HSNCodes.FirstOrDefault(h => h.Id == mapping.HSNCodeId);
            RuleOverrideAllowed = mapping.OverrideAllowed;
        }
    }

    [RelayCommand]
    private Task SaveRuleAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        if (!Validate(v => v
            .Rule(RuleProduct is not null, "Select a product.")
            .Rule(RuleGroup is not null, "Select a tax group.")
            .Rule(RuleHSN is not null, "Select an HSN code.")))
            return;

        var dto = new ProductTaxMappingDto(
            RuleProduct!.Id, RuleGroup!.Id, RuleHSN!.Id, RuleOverrideAllowed);

        await groupService.SetProductMappingAsync(dto, ct);
        SuccessMessage = $"Tax rule saved for {RuleProduct.Name}.";
        await LoadCurrentMappingAsync();
    });

    [RelayCommand]
    private Task RemoveRuleAsync() => RunAsync(async ct =>
    {
        if (RuleProduct is null) return;
        await groupService.RemoveProductMappingAsync(RuleProduct.Id, ct);
        SuccessMessage = "Tax rule removed.";
        await LoadCurrentMappingAsync();
    });

    // ═══════════════════════════════════════════════════════════════
    //  Load all
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await ReloadGroupsAsync(ct);
        await ReloadHSNCodesAsync(ct);

        var products = await productService.GetActiveAsync(ct);
        Products = new ObservableCollection<Product>(products);
    });

    private async Task ReloadGroupsAsync(CancellationToken ct = default)
    {
        var groups = await groupService.GetAllGroupsAsync(ct);
        Groups = new ObservableCollection<TaxGroup>(groups);
    }

    private async Task ReloadHSNCodesAsync(CancellationToken ct = default)
    {
        var codes = await groupService.GetAllHSNCodesAsync(ct);
        HSNCodes = new ObservableCollection<HSNCode>(codes);
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
