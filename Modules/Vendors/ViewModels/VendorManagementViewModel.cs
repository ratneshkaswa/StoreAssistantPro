using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Vendors.Services;

namespace StoreAssistantPro.Modules.Vendors.ViewModels;

public partial class VendorManagementViewModel(
    IVendorService vendorService,
    IVendorLedgerService ledgerService,
    IRegionalSettingsService regional,
    IAppStateService appState) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<Vendor> Vendors { get; set; } = [];

    [ObservableProperty]
    public partial Vendor? SelectedVendor { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    // ── Paging ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial string PagingInfo { get; set; } = string.Empty;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    private const int PageSize = 25;

    // ── Form fields ──

    [ObservableProperty]
    public partial string VendorName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ContactPerson { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Address { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AddressLine2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string City { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string State { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PinCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GSTIN { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PAN { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TransportPreference { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    partial void OnSelectedVendorChanged(Vendor? value)
    {
        if (value is null)
        {
            ResetForm(clearMessages: false);
            return;
        }

        PopulateForm(value);
    }

    private void PopulateForm(Vendor value)
    {
        VendorName = value.Name;
        ContactPerson = value.ContactPerson ?? string.Empty;
        Phone = value.Phone ?? string.Empty;
        Email = value.Email ?? string.Empty;
        Address = value.Address ?? string.Empty;
        AddressLine2 = value.AddressLine2 ?? string.Empty;
        City = value.City ?? string.Empty;
        State = value.State ?? string.Empty;
        PinCode = value.PinCode ?? string.Empty;
        GSTIN = value.GSTIN ?? string.Empty;
        PAN = value.PAN ?? string.Empty;
        TransportPreference = value.TransportPreference ?? string.Empty;
        IsEditing = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private void ResetForm(bool clearMessages)
    {
        VendorName = string.Empty;
        ContactPerson = string.Empty;
        Phone = string.Empty;
        Email = string.Empty;
        Address = string.Empty;
        AddressLine2 = string.Empty;
        City = string.Empty;
        State = string.Empty;
        PinCode = string.Empty;
        GSTIN = string.Empty;
        PAN = string.Empty;
        TransportPreference = string.Empty;
        IsEditing = false;

        if (clearMessages)
            ClearMessages();
    }

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await ReloadVendorsAsync(ct);
    });

    private async Task ReloadVendorsAsync(CancellationToken ct, int? selectedVendorId = null)
    {
        var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
        var result = await vendorService.GetPagedAsync(new PagedQuery(CurrentPage, PageSize), search, ct);
        Vendors = new ObservableCollection<Vendor>(result.Items);
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages == 0 ? 1 : result.TotalPages;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        PagingInfo = TotalCount > 0
            ? $"Page {CurrentPage} of {TotalPages} ({TotalCount} total)"
            : string.Empty;
        SelectedVendor = selectedVendorId.HasValue
            ? Vendors.FirstOrDefault(v => v.Id == selectedVendorId.Value)
            : null;
    }

    [RelayCommand]
    private Task SearchAsync() => RunAsync(async ct =>
    {
        CurrentPage = 1;
        await ReloadVendorsAsync(ct);
    });

    [RelayCommand]
    private void NewVendor()
    {
        SelectedVendor = null;
        ResetForm(clearMessages: true);
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(VendorName), "Vendor name is required.")))
            return;

        var gstinError = GstinValidator.Validate(GSTIN);
        if (gstinError is not null) { ErrorMessage = gstinError; return; }

        var panError = GstinValidator.ValidatePan(PAN);
        if (panError is not null) { ErrorMessage = panError; return; }

        var dto = new VendorDto(
            VendorName, ContactPerson, Phone, Email,
            Address, AddressLine2, City, State, PinCode,
            GSTIN, PAN, TransportPreference, null, 0, 0, null);

        if (IsEditing && SelectedVendor is not null)
        {
            await vendorService.UpdateAsync(SelectedVendor.Id, dto, ct);
            SuccessMessage = "Vendor updated.";
        }
        else
        {
            await vendorService.CreateAsync(dto, ct);
            SuccessMessage = "Vendor created.";
        }

        await ReloadVendorsAsync(ct);
        SelectedVendor = null;
        ResetForm(clearMessages: false);
    });

    [RelayCommand]
    private Task ToggleActiveAsync(Vendor? vendor) => RunAsync(async ct =>
    {
        if (vendor is null) return;
        var selectedVendorId = SelectedVendor?.Id == vendor.Id ? vendor.Id : SelectedVendor?.Id;
        await vendorService.ToggleActiveAsync(vendor.Id, ct);
        await ReloadVendorsAsync(ct, selectedVendorId ?? vendor.Id);
        SuccessMessage = "Status toggled.";
    });

    [RelayCommand]
    private void ExportCsv()
    {
        if (Vendors.Count == 0) return;
        if (CsvExporter.Export(Vendors, "Vendors.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    [RelayCommand]
    private Task ImportCsvAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        var rows = CsvImporter.Import();
        if (rows is null) return;
        if (rows.Count == 0) { ErrorMessage = "CSV file is empty."; return; }

        var imported = await vendorService.ImportBulkAsync(rows, ct);
        SuccessMessage = $"Imported {imported} vendor(s).";
        await ReloadVendorsAsync(ct);
    });

    // ═══════════════════════════════════════════════════════════════
    //  Supplier Ledger (#87) & Payment Tracking (#90)
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial bool ShowLedger { get; set; }

    [ObservableProperty]
    public partial VendorLedgerSummary? LedgerSummary { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<VendorLedgerEntry> LedgerEntries { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<VendorPayment> VendorPayments { get; set; } = [];

    // ── Payment form fields ──

    [ObservableProperty]
    public partial string PaymentAmount { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PaymentMethod { get; set; } = "Cash";

    [ObservableProperty]
    public partial string PaymentReference { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PaymentNotes { get; set; } = string.Empty;

    public ObservableCollection<string> PaymentMethods { get; } =
    [
        "Cash",
        "UPI",
        "Bank Transfer",
        "Cheque"
    ];

    public string FormatCurrency(decimal amount) => regional.FormatCurrency(amount);

    [RelayCommand]
    private Task LoadLedgerAsync() => RunAsync(async ct =>
    {
        if (SelectedVendor is null) return;
        ClearMessages();

        var summaryTask = ledgerService.GetLedgerSummaryAsync(SelectedVendor.Id, ct);
        var entriesTask = ledgerService.GetLedgerEntriesAsync(SelectedVendor.Id, ct);
        var paymentsTask = ledgerService.GetPaymentsAsync(SelectedVendor.Id, ct);

        await Task.WhenAll(summaryTask, entriesTask, paymentsTask);

        LedgerSummary = summaryTask.Result;
        LedgerEntries = new ObservableCollection<VendorLedgerEntry>(entriesTask.Result);
        VendorPayments = new ObservableCollection<VendorPayment>(paymentsTask.Result);
        ShowLedger = true;
    });

    [RelayCommand]
    private Task RecordPaymentAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (SelectedVendor is null)
        {
            ErrorMessage = "Select a vendor first.";
            return;
        }

        if (!Validate(v => v
            .Rule(decimal.TryParse(PaymentAmount, out var amt) && amt > 0, "Enter a valid payment amount.")))
            return;

        var dto = new VendorPaymentDto(
            SelectedVendor.Id,
            decimal.Parse(PaymentAmount),
            PaymentMethod,
            string.IsNullOrWhiteSpace(PaymentReference) ? null : PaymentReference.Trim(),
            string.IsNullOrWhiteSpace(PaymentNotes) ? null : PaymentNotes.Trim(),
            (int)appState.CurrentUserType);

        await ledgerService.RecordPaymentAsync(dto, ct);

        PaymentAmount = string.Empty;
        PaymentReference = string.Empty;
        PaymentNotes = string.Empty;
        PaymentMethod = "Cash";

        SuccessMessage = $"Payment of {regional.FormatCurrency(dto.Amount)} recorded.";

        await LoadLedgerAsync();
    });

    [RelayCommand]
    private void CloseLedger()
    {
        ShowLedger = false;
        LedgerSummary = null;
        LedgerEntries = [];
        VendorPayments = [];
    }

    [RelayCommand]
    private Task PreviousPageAsync() => RunAsync(async ct =>
    {
        if (!HasPreviousPage) return;
        CurrentPage--;
        await ReloadVendorsAsync(ct);
    });

    [RelayCommand]
    private Task NextPageAsync() => RunAsync(async ct =>
    {
        if (!HasNextPage) return;
        CurrentPage++;
        await ReloadVendorsAsync(ct);
    });

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
