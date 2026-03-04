using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Brands.Services;

namespace StoreAssistantPro.Modules.Brands.ViewModels;

public partial class BrandManagementViewModel(IBrandService brandService) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<Brand> Brands { get; set; } = [];

    [ObservableProperty]
    public partial Brand? SelectedBrand { get; set; }

    [ObservableProperty]
    public partial string BrandName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    partial void OnSelectedBrandChanged(Brand? value)
    {
        if (value is null) return;
        BrandName = value.Name;
        IsEditing = true;
        ClearMessages();
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedBrand = null;
        BrandName = string.Empty;
        IsEditing = false;
        ClearMessages();
    }

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var brands = await brandService.GetAllAsync(ct);
        Brands = new ObservableCollection<Brand>(brands);
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(BrandName), "Brand name is required.")))
            return;

        if (IsEditing && SelectedBrand is not null)
        {
            await brandService.UpdateAsync(SelectedBrand.Id, BrandName.Trim(), ct);
            SuccessMessage = "Brand updated.";
        }
        else
        {
            await brandService.CreateAsync(BrandName.Trim(), ct);
            SuccessMessage = "Brand created.";
        }

        await ReloadAsync(ct);
        ClearForm();
    });

    [RelayCommand]
    private Task ToggleActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedBrand is null) return;
        await brandService.ToggleActiveAsync(SelectedBrand.Id, ct);
        SuccessMessage = $"Brand '{SelectedBrand.Name}' {(SelectedBrand.IsActive ? "deactivated" : "activated")}.";
        await ReloadAsync(ct);
    });

    private async Task ReloadAsync(CancellationToken ct)
    {
        var brands = await brandService.GetAllAsync(ct);
        Brands = new ObservableCollection<Brand>(brands);
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
