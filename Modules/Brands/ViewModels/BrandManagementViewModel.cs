using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
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
    public partial string SearchText { get; set; } = string.Empty;

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
    private Task ToggleActiveAsync(Brand? brand) => RunAsync(async ct =>
    {
        if (brand is null) return;
        await brandService.ToggleActiveAsync(brand.Id, ct);
        SuccessMessage = $"Brand '{brand.Name}' {(brand.IsActive ? "deactivated" : "activated")}.";
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task SearchAsync() => RunAsync(async ct =>
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await ReloadAsync(ct);
            return;
        }
        var results = await brandService.SearchAsync(SearchText, ct);
        Brands = new ObservableCollection<Brand>(results);
    });

    [RelayCommand]
    private void ExportCsv()
    {
        if (Brands.Count == 0) return;
        if (CsvExporter.Export(Brands, "Brands.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    [RelayCommand]
    private Task ImportCsvAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        var rows = CsvImporter.Import();
        if (rows is null) return;
        if (rows.Count == 0) { ErrorMessage = "CSV file is empty."; return; }

        var names = rows
            .Select(r => r.GetValueOrDefault("Name") ?? r.GetValueOrDefault("Brand") ?? "")
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        if (names.Count == 0) { ErrorMessage = "No valid brand names found in CSV. Expected column: Name."; return; }

        var imported = await brandService.ImportBulkAsync(names, ct);
        SuccessMessage = $"Imported {imported} brand(s). {names.Count - imported} skipped (duplicates).";
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
