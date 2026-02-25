using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Suppliers.Services;

namespace StoreAssistantPro.Modules.Suppliers.ViewModels;

public partial class SuppliersViewModel(ISupplierService supplierService) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<Supplier> Suppliers { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedSupplier))]
    public partial Supplier? SelectedSupplier { get; set; }

    public bool HasSelectedSupplier => SelectedSupplier is not null;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedActiveFilter { get; set; } = "All";

    public string[] ActiveFilterOptions { get; } = ["All", "Active", "Inactive"];

    // ── Add form fields ──

    [ObservableProperty]
    public partial bool IsAddFormVisible { get; set; }

    [ObservableProperty]
    public partial string NewName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewContactPerson { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewPhone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewAddress { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewGSTIN { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewNotes { get; set; } = string.Empty;

    // ── Edit form fields ──

    [ObservableProperty]
    public partial bool IsEditFormVisible { get; set; }

    [ObservableProperty]
    public partial string EditName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditContactPerson { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditPhone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditAddress { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditGSTIN { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditNotes { get; set; } = string.Empty;

    private IReadOnlyList<Supplier> _allSuppliers = [];

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedActiveFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task LoadSuppliersAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            _allSuppliers = await supplierService.GetAllAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load suppliers: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allSuppliers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            filtered = filtered.Where(s =>
                s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (s.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.GSTIN?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.ContactPerson?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        filtered = SelectedActiveFilter switch
        {
            "Active" => filtered.Where(s => s.IsActive),
            "Inactive" => filtered.Where(s => !s.IsActive),
            _ => filtered
        };

        Suppliers = new ObservableCollection<Supplier>(filtered);
    }

    [RelayCommand]
    private void ShowAddForm()
    {
        IsEditFormVisible = false;
        NewName = string.Empty;
        NewContactPerson = string.Empty;
        NewPhone = string.Empty;
        NewEmail = string.Empty;
        NewAddress = string.Empty;
        NewGSTIN = string.Empty;
        NewNotes = string.Empty;
        IsAddFormVisible = true;
    }

    [RelayCommand]
    private void CancelAdd() => IsAddFormVisible = false;

    [RelayCommand]
    private async Task SaveSupplierAsync()
    {
        if (!Validate(v => v.Rule(InputValidator.IsRequired(NewName), "Supplier name is required.")))
            return;

        var supplier = new Supplier
        {
            Name = NewName.Trim(),
            ContactPerson = string.IsNullOrWhiteSpace(NewContactPerson) ? null : NewContactPerson.Trim(),
            Phone = string.IsNullOrWhiteSpace(NewPhone) ? null : NewPhone.Trim(),
            Email = string.IsNullOrWhiteSpace(NewEmail) ? null : NewEmail.Trim(),
            Address = string.IsNullOrWhiteSpace(NewAddress) ? null : NewAddress.Trim(),
            GSTIN = string.IsNullOrWhiteSpace(NewGSTIN) ? null : NewGSTIN.Trim(),
            Notes = string.IsNullOrWhiteSpace(NewNotes) ? null : NewNotes.Trim()
        };

        await supplierService.AddAsync(supplier);
        IsAddFormVisible = false;
        await LoadSuppliersAsync();
    }

    [RelayCommand]
    private void ShowEditForm()
    {
        if (SelectedSupplier is null) return;
        IsAddFormVisible = false;
        EditName = SelectedSupplier.Name;
        EditContactPerson = SelectedSupplier.ContactPerson ?? string.Empty;
        EditPhone = SelectedSupplier.Phone ?? string.Empty;
        EditEmail = SelectedSupplier.Email ?? string.Empty;
        EditAddress = SelectedSupplier.Address ?? string.Empty;
        EditGSTIN = SelectedSupplier.GSTIN ?? string.Empty;
        EditNotes = SelectedSupplier.Notes ?? string.Empty;
        IsEditFormVisible = true;
    }

    [RelayCommand]
    private void CancelEdit() => IsEditFormVisible = false;

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (SelectedSupplier is null) return;
        if (!Validate(v => v.Rule(InputValidator.IsRequired(EditName), "Supplier name is required.")))
            return;

        var supplier = await supplierService.GetByIdAsync(SelectedSupplier.Id);
        if (supplier is null) { ErrorMessage = "Supplier not found."; return; }

        supplier.Name = EditName.Trim();
        supplier.ContactPerson = string.IsNullOrWhiteSpace(EditContactPerson) ? null : EditContactPerson.Trim();
        supplier.Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim();
        supplier.Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail.Trim();
        supplier.Address = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress.Trim();
        supplier.GSTIN = string.IsNullOrWhiteSpace(EditGSTIN) ? null : EditGSTIN.Trim();
        supplier.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();

        await supplierService.UpdateAsync(supplier);
        IsEditFormVisible = false;
        await LoadSuppliersAsync();
    }

    [RelayCommand]
    private async Task DeleteSupplierAsync()
    {
        if (SelectedSupplier is null) return;
        await supplierService.DeleteAsync(SelectedSupplier.Id, SelectedSupplier.RowVersion);
        await LoadSuppliersAsync();
    }

    [RelayCommand]
    private async Task ExportSuppliersAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            Title = "Export Suppliers to CSV",
            FileName = "Suppliers_Export.csv"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id,Name,Contact Person,Phone,Email,Address,GSTIN,Active,Notes");

            foreach (var s in Suppliers)
            {
                sb.AppendLine(string.Join(",",
                    s.Id,
                    Esc(s.Name),
                    Esc(s.ContactPerson ?? ""),
                    Esc(s.Phone ?? ""),
                    Esc(s.Email ?? ""),
                    Esc(s.Address ?? ""),
                    Esc(s.GSTIN ?? ""),
                    s.IsActive ? "Yes" : "No",
                    Esc(s.Notes ?? "")));
            }

            await System.IO.File.WriteAllTextAsync(dialog.FileName, sb.ToString());
            ErrorMessage = $"Exported {Suppliers.Count} suppliers to {System.IO.Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
    }

    private static string Esc(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
