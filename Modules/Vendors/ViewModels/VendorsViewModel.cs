using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Vendors.Services;

namespace StoreAssistantPro.Modules.Vendors.ViewModels;

public partial class VendorsViewModel(
    IVendorService vendorService) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<Vendor> Vendors { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedVendor))]
    public partial Vendor? SelectedVendor { get; set; }

    public bool HasSelectedVendor => SelectedVendor is not null;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedActiveFilter { get; set; } = "All";

    public string[] ActiveFilterOptions { get; } = ["All", "Active", "Inactive"];

    [ObservableProperty]
    public partial string VendorCountDisplay { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SuccessMessage { get; set; } = string.Empty;

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
    public partial string NewCity { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewState { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewPinCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewGSTIN { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewPaymentTerms { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewCreditLimit { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewOpeningBalance { get; set; } = string.Empty;

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
    public partial string EditCity { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditState { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditPinCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditGSTIN { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditPaymentTerms { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditCreditLimit { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditOpeningBalance { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditNotes { get; set; } = string.Empty;

    public string[] PaymentTermsOptions { get; } =
        ["", "COD", "Advance", "Net 7", "Net 15", "Net 30", "Net 45", "Net 60", "Net 90"];

    private IReadOnlyList<Vendor> _allVendors = [];

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedActiveFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    private Task LoadVendorsAsync() => RunLoadAsync(async ct =>
    {
        _allVendors = await vendorService.GetAllAsync(ct);
        ApplyFilter();
        var total = _allVendors.Count;
        var active = _allVendors.Count(v => v.IsActive);
        VendorCountDisplay = $"{active} active / {total} total";
    });

    private void ApplyFilter()
    {
        var filtered = _allVendors.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            filtered = filtered.Where(v =>
                v.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (v.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.GSTIN?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.ContactPerson?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.City?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        filtered = SelectedActiveFilter switch
        {
            "Active" => filtered.Where(v => v.IsActive),
            "Inactive" => filtered.Where(v => !v.IsActive),
            _ => filtered
        };

        Vendors = new ObservableCollection<Vendor>(filtered);
    }

    // ── Add ──

    [RelayCommand]
    private void ShowAddForm()
    {
        IsEditFormVisible = false;
        NewName = string.Empty;
        NewContactPerson = string.Empty;
        NewPhone = string.Empty;
        NewEmail = string.Empty;
        NewAddress = string.Empty;
        NewCity = string.Empty;
        NewState = string.Empty;
        NewPinCode = string.Empty;
        NewGSTIN = string.Empty;
        NewPaymentTerms = string.Empty;
        NewCreditLimit = string.Empty;
        NewOpeningBalance = string.Empty;
        NewNotes = string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsAddFormVisible = true;
    }

    [RelayCommand]
    private void CancelAdd()
    {
        IsAddFormVisible = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private Task SaveVendorAsync() => RunAsync(async ct =>
    {
        var gstinError = GstinValidator.GetValidationError(NewGSTIN);
        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(NewName), "Vendor name is required.")
            .Rule(gstinError is null, gstinError ?? string.Empty)))
            return;

        var vendor = new Vendor
        {
            Name = NewName.Trim(),
            ContactPerson = NullIfEmpty(NewContactPerson),
            Phone = NullIfEmpty(NewPhone),
            Email = NullIfEmpty(NewEmail),
            Address = NullIfEmpty(NewAddress),
            City = NullIfEmpty(NewCity),
            State = NullIfEmpty(NewState),
            PinCode = NullIfEmpty(NewPinCode),
            GSTIN = NullIfEmpty(NewGSTIN),
            PaymentTerms = NullIfEmpty(NewPaymentTerms),
            CreditLimit = ParseDecimal(NewCreditLimit),
            OpeningBalance = ParseDecimal(NewOpeningBalance),
            Notes = NullIfEmpty(NewNotes)
        };

        await vendorService.AddAsync(vendor, ct);
        IsAddFormVisible = false;
        await LoadVendorsAsync();
    });

    // ── Edit ──

    [RelayCommand]
    private void ShowEditForm()
    {
        if (SelectedVendor is null) return;
        IsAddFormVisible = false;
        EditName = SelectedVendor.Name;
        EditContactPerson = SelectedVendor.ContactPerson ?? string.Empty;
        EditPhone = SelectedVendor.Phone ?? string.Empty;
        EditEmail = SelectedVendor.Email ?? string.Empty;
        EditAddress = SelectedVendor.Address ?? string.Empty;
        EditCity = SelectedVendor.City ?? string.Empty;
        EditState = SelectedVendor.State ?? string.Empty;
        EditPinCode = SelectedVendor.PinCode ?? string.Empty;
        EditGSTIN = SelectedVendor.GSTIN ?? string.Empty;
        EditPaymentTerms = SelectedVendor.PaymentTerms ?? string.Empty;
        EditCreditLimit = SelectedVendor.CreditLimit > 0 ? SelectedVendor.CreditLimit.ToString("N2") : string.Empty;
        EditOpeningBalance = SelectedVendor.OpeningBalance != 0 ? SelectedVendor.OpeningBalance.ToString("N2") : string.Empty;
        EditNotes = SelectedVendor.Notes ?? string.Empty;
        ErrorMessage = string.Empty;
        IsEditFormVisible = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditFormVisible = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private Task SaveEditAsync() => RunAsync(async ct =>
    {
        if (SelectedVendor is null) return;
        var editGstinError = GstinValidator.GetValidationError(EditGSTIN);
        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(EditName), "Vendor name is required.")
            .Rule(editGstinError is null, editGstinError ?? string.Empty)))
            return;

        var vendor = await vendorService.GetByIdAsync(SelectedVendor.Id, ct);
        if (vendor is null) { ErrorMessage = "Vendor not found."; return; }

        vendor.Name = EditName.Trim();
        vendor.ContactPerson = NullIfEmpty(EditContactPerson);
        vendor.Phone = NullIfEmpty(EditPhone);
        vendor.Email = NullIfEmpty(EditEmail);
        vendor.Address = NullIfEmpty(EditAddress);
        vendor.City = NullIfEmpty(EditCity);
        vendor.State = NullIfEmpty(EditState);
        vendor.PinCode = NullIfEmpty(EditPinCode);
        vendor.GSTIN = NullIfEmpty(EditGSTIN);
        vendor.PaymentTerms = NullIfEmpty(EditPaymentTerms);
        vendor.CreditLimit = ParseDecimal(EditCreditLimit);
        vendor.OpeningBalance = ParseDecimal(EditOpeningBalance);
        vendor.Notes = NullIfEmpty(EditNotes);

        await vendorService.UpdateAsync(vendor, ct);
        IsEditFormVisible = false;
        await LoadVendorsAsync();
    });

    // ── Toggle active ──

    [RelayCommand]
    private Task ToggleActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedVendor is null) return;
        await vendorService.ToggleActiveAsync(SelectedVendor.Id, SelectedVendor.RowVersion, ct);
        await LoadVendorsAsync();
    });

    // ── Delete ──

    [RelayCommand]
    private Task DeleteVendorAsync() => RunAsync(async ct =>
    {
        if (SelectedVendor is null) return;
        await vendorService.DeleteAsync(SelectedVendor.Id, SelectedVendor.RowVersion, ct);
        await LoadVendorsAsync();
    });

    // ── Export ──

    [RelayCommand]
    private async Task ExportVendorsAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            Title = "Export Vendors to CSV",
            FileName = "Vendors_Export.csv"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id,Name,Contact Person,Phone,Email,Address,City,State,PIN,GSTIN,Payment Terms,Credit Limit,Opening Balance,Active,Notes");

            foreach (var v in Vendors)
            {
                sb.AppendLine(string.Join(",",
                    v.Id,
                    Esc(v.Name),
                    Esc(v.ContactPerson ?? ""),
                    Esc(v.Phone ?? ""),
                    Esc(v.Email ?? ""),
                    Esc(v.Address ?? ""),
                    Esc(v.City ?? ""),
                    Esc(v.State ?? ""),
                    Esc(v.PinCode ?? ""),
                    Esc(v.GSTIN ?? ""),
                    Esc(v.PaymentTerms ?? ""),
                    v.CreditLimit.ToString("N2"),
                    v.OpeningBalance.ToString("N2"),
                    v.IsActive ? "Yes" : "No",
                    Esc(v.Notes ?? "")));
            }

            await System.IO.File.WriteAllTextAsync(dialog.FileName, sb.ToString());
            SuccessMessage = $"Exported {Vendors.Count} vendors to {System.IO.Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal ParseDecimal(string value) =>
        decimal.TryParse(value?.Trim(), out var result) ? result : 0;

    private static string Esc(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
