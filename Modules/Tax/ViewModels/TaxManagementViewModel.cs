using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Modules.Tax.ViewModels;

public partial class TaxManagementViewModel(ITaxService taxService) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<TaxMaster> TaxSlabs { get; set; } = [];

    [ObservableProperty]
    public partial TaxMaster? SelectedTaxSlab { get; set; }

    // ── Form fields ──

    [ObservableProperty]
    public partial string TaxName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TaxRate { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HSNCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TaxApplicableCategory SelectedCategory { get; set; } = TaxApplicableCategory.Both;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    public ObservableCollection<TaxApplicableCategory> Categories { get; } =
    [
        TaxApplicableCategory.Readymade,
        TaxApplicableCategory.GarmentCloth,
        TaxApplicableCategory.Both
    ];

    partial void OnSelectedTaxSlabChanged(TaxMaster? value)
    {
        if (value is null) return;
        TaxName = value.TaxName;
        TaxRate = value.TaxRate.ToString("G");
        HSNCode = value.HSNCode ?? string.Empty;
        SelectedCategory = value.ApplicableCategory;
        IsEditing = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var slabs = await taxService.GetAllAsync(ct);
        TaxSlabs = new ObservableCollection<TaxMaster>(slabs);
    });

    [RelayCommand]
    private void NewTaxSlab()
    {
        SelectedTaxSlab = null;
        TaxName = string.Empty;
        TaxRate = string.Empty;
        HSNCode = string.Empty;
        SelectedCategory = TaxApplicableCategory.Both;
        IsEditing = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        if (!Validate(v => v
            .Rule(!string.IsNullOrWhiteSpace(TaxName), "Tax name is required.")
            .Rule(decimal.TryParse(TaxRate, out var rate) && rate >= 0 && rate <= 100,
                  "GST percent must be 0–100.")))
            return;

        var dto = new TaxMasterDto(
            TaxName.Trim(),
            decimal.Parse(TaxRate),
            string.IsNullOrWhiteSpace(HSNCode) ? null : HSNCode.Trim(),
            SelectedCategory);

        if (IsEditing && SelectedTaxSlab is not null)
        {
            await taxService.UpdateAsync(SelectedTaxSlab.Id, dto, ct);
            SuccessMessage = "Tax slab updated.";
        }
        else
        {
            await taxService.CreateAsync(dto, ct);
            SuccessMessage = "Tax slab created.";
        }

        await LoadAsync();
        NewTaxSlab();
    });

    [RelayCommand]
    private Task ToggleActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedTaxSlab is null) return;
        await taxService.ToggleActiveAsync(SelectedTaxSlab.Id, ct);
        await LoadAsync();
        SuccessMessage = "Status toggled.";
    });
}
