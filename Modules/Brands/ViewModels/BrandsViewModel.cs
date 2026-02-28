using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Brands.Commands;
using StoreAssistantPro.Modules.Brands.Services;

namespace StoreAssistantPro.Modules.Brands.ViewModels;

public partial class BrandsViewModel(
    IBrandService brandService,
    ISessionService sessionService,
    IDialogService dialogService,
    IMasterPinValidator masterPinValidator,
    ICommandBus commandBus) : BaseViewModel
{
    // ── Role-based access ──

    public bool CanManageBrands =>
        sessionService.CurrentUserType is UserType.Admin or UserType.Manager;

    public bool CanDeleteBrands =>
        sessionService.CurrentUserType is UserType.Admin;

    [ObservableProperty]
    public partial ObservableCollection<Brand> Brands { get; set; } = [];

    [ObservableProperty]
    public partial Brand? SelectedBrand { get; set; }

    // ── Search ──

    private IReadOnlyList<Brand> _allBrands = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allBrands
            : _allBrands.Where(b => b.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        Brands = new ObservableCollection<Brand>(filtered);
    }

    // ── Add form ──

    [ObservableProperty]
    public partial string NewBrandName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool NewBrandIsActive { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAddFormVisible { get; set; }

    // ── Edit form ──

    [ObservableProperty]
    public partial string EditBrandName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool EditBrandIsActive { get; set; } = true;

    [ObservableProperty]
    public partial bool IsEditFormVisible { get; set; }

    // ── Data loading ──

    [RelayCommand]
    private Task LoadBrandsAsync() => RunLoadAsync(async ct =>
    {
        _allBrands = await brandService.GetAllWithProductCountAsync(ct);
        ApplyFilter();
    });

    // ── Add ──

    [RelayCommand]
    private void ShowAddForm()
    {
        if (!CanManageBrands)
        {
            ErrorMessage = "Only administrators and managers can add brands.";
            return;
        }

        ErrorMessage = string.Empty;
        NewBrandName = string.Empty;
        NewBrandIsActive = true;
        IsEditFormVisible = false;
        IsAddFormVisible = true;
    }

    [RelayCommand]
    private void CancelAdd()
    {
        IsAddFormVisible = false;
    }

    [RelayCommand]
    private async Task SaveBrandAsync()
    {
        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(NewBrandName), "Brand name is required.")))
            return;

        var result = await commandBus.SendAsync(
            new SaveBrandCommand(NewBrandName.Trim(), NewBrandIsActive));

        if (result.Succeeded)
        {
            IsAddFormVisible = false;
            await LoadBrandsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Save failed.";
        }
    }

    // ── Edit ──

    [RelayCommand]
    private void ShowEditForm()
    {
        if (SelectedBrand is null) return;

        if (!CanManageBrands)
        {
            ErrorMessage = "Only administrators and managers can edit brands.";
            return;
        }

        ErrorMessage = string.Empty;
        EditBrandName = SelectedBrand.Name;
        EditBrandIsActive = SelectedBrand.IsActive;
        IsAddFormVisible = false;
        IsEditFormVisible = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditFormVisible = false;
    }

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (!Validate(v => v
            .Rule(SelectedBrand is not null, "No brand selected.")
            .Rule(InputValidator.IsRequired(EditBrandName), "Brand name is required.")))
            return;

        var brand = SelectedBrand!;
        var result = await commandBus.SendAsync(
            new UpdateBrandCommand(brand.Id, EditBrandName.Trim(), EditBrandIsActive, brand.RowVersion));

        if (result.Succeeded)
        {
            IsEditFormVisible = false;
            await LoadBrandsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Update failed.";
            await LoadBrandsAsync();
        }
    }

    // ── Delete ──

    [RelayCommand]
    private async Task DeleteBrandAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedBrand is null) return;

        if (!CanDeleteBrands)
        {
            ErrorMessage = "Only administrators can delete brands.";
            return;
        }

        if (await brandService.HasProductsAsync(SelectedBrand.Id))
        {
            ErrorMessage = $"Cannot delete '{SelectedBrand.Name}' — it has products assigned. Remove or reassign products first.";
            return;
        }

        if (!dialogService.Confirm(
            $"Delete brand '{SelectedBrand.Name}'?\n\nThis action cannot be undone.",
            "Delete Brand"))
            return;

        if (!await masterPinValidator.ValidateAsync("Enter Master PIN to delete this brand."))
        {
            ErrorMessage = "Master PIN validation failed. Delete cancelled.";
            return;
        }

        var result = await commandBus.SendAsync(
            new DeleteBrandCommand(SelectedBrand.Id, SelectedBrand.RowVersion));

        if (result.Succeeded)
        {
            SelectedBrand = null;
            await LoadBrandsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Delete failed.";
            await LoadBrandsAsync();
        }
    }

    // ── Export ──

    [RelayCommand]
    private async Task ExportBrandsAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            Title = "Export Brands to CSV",
            FileName = "Brands_Export.csv"
        };

        if (dialog.ShowDialog() != true) return;

        ErrorMessage = string.Empty;

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Name,ProductCount,IsActive");

            foreach (var b in _allBrands)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(b.Name),
                    b.Products.Count,
                    b.IsActive));
            }

            await System.IO.File.WriteAllTextAsync(dialog.FileName, sb.ToString());
            ErrorMessage = $"Exported {_allBrands.Count} brands to {System.IO.Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
