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
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var taxes = await taxService.GetAllAsync(ct);
        Taxes = new ObservableCollection<TaxMaster>(taxes);
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        if (!Validate(v => v
            .Rule(!string.IsNullOrWhiteSpace(TaxName), "Tax name is required.")
            .Rule(decimal.TryParse(SlabPercent, out var pct) && pct >= 0 && pct <= 100,
                  "Slab % must be a number between 0 and 100.")))
            return;

        var dto = new TaxDto(TaxName.Trim(), decimal.Parse(SlabPercent));

        if (IsEditing && SelectedTax is not null)
        {
            await taxService.UpdateAsync(SelectedTax.Id, dto, ct);
            SuccessMessage = "Tax updated.";
        }
        else
        {
            await taxService.CreateAsync(dto, ct);
            SuccessMessage = "Tax created.";
        }

        await ReloadAsync(ct);
        ClearForm();
    });

    [RelayCommand]
    private Task DeleteAsync() => RunAsync(async ct =>
    {
        if (SelectedTax is null) return;

        await taxService.DeleteAsync(SelectedTax.Id, ct);
        SuccessMessage = "Tax deleted.";
        await ReloadAsync(ct);
        ClearForm();
    });

    private async Task ReloadAsync(CancellationToken ct)
    {
        var taxes = await taxService.GetAllAsync(ct);
        Taxes = new ObservableCollection<TaxMaster>(taxes);
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
