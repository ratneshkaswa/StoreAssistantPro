using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Customers.Services;

namespace StoreAssistantPro.Modules.Customers.ViewModels;

public partial class CustomerManagementViewModel(
    ICustomerService customerService) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<Customer> Customers { get; set; } = [];

    [ObservableProperty]
    public partial Customer? SelectedCustomer { get; set; }

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

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
    public partial string CustomerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Address { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GSTIN { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    // ── Purchase History (#159) ──

    [ObservableProperty]
    public partial ObservableCollection<CustomerPurchaseSummary> PurchaseHistory { get; set; } = [];

    [ObservableProperty]
    public partial bool HasPurchaseHistory { get; set; }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value is null)
        {
            PurchaseHistory = [];
            HasPurchaseHistory = false;
            return;
        }

        CustomerName = value.Name;
        Phone = value.Phone ?? string.Empty;
        Email = value.Email ?? string.Empty;
        Address = value.Address ?? string.Empty;
        GSTIN = value.GSTIN ?? string.Empty;
        Notes = value.Notes ?? string.Empty;

        LoadPurchaseHistoryCommand.Execute(value);
    }

    [RelayCommand]
    private Task LoadPurchaseHistoryAsync(Customer? customer) => RunAsync(async ct =>
    {
        if (customer is null) return;
        var history = await customerService.GetPurchaseHistoryAsync(customer.Id, ct);
        PurchaseHistory = new ObservableCollection<CustomerPurchaseSummary>(history);
        HasPurchaseHistory = history.Count > 0;
    });

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task SearchAsync() => RunAsync(async ct =>
    {
        CurrentPage = 1;
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void NewCustomer()
    {
        SelectedCustomer = null;
        CustomerName = string.Empty;
        Phone = string.Empty;
        Email = string.Empty;
        Address = string.Empty;
        GSTIN = string.Empty;
        Notes = string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (!Validate(v => v
            .Rule(!string.IsNullOrWhiteSpace(CustomerName), "Customer name is required.")))
            return;

        var gstinError = GstinValidator.Validate(GSTIN);
        if (gstinError is not null) { ErrorMessage = gstinError; return; }

        var dto = new CustomerDto(
            CustomerName,
            string.IsNullOrWhiteSpace(Phone) ? null : Phone,
            string.IsNullOrWhiteSpace(Email) ? null : Email,
            string.IsNullOrWhiteSpace(Address) ? null : Address,
            string.IsNullOrWhiteSpace(GSTIN) ? null : GSTIN,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes);

        if (SelectedCustomer is null)
        {
            await customerService.CreateAsync(dto, ct);
            SuccessMessage = $"Customer '{CustomerName}' created.";
        }
        else
        {
            await customerService.UpdateAsync(SelectedCustomer.Id, dto, ct);
            SuccessMessage = $"Customer '{CustomerName}' updated.";
        }

        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task ToggleActiveAsync(Customer? customer) => RunAsync(async ct =>
    {
        if (customer is null) return;
        await customerService.ToggleActiveAsync(customer.Id, ct);
        await ReloadAsync(ct);
        SuccessMessage = "Active status toggled.";
    });

    [RelayCommand]
    private void ExportCsv()
    {
        if (Customers.Count == 0) return;
        if (CsvExporter.Export(Customers, "Customers.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    [RelayCommand]
    private Task ImportCsvAsync() => RunAsync(async ct =>
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        var rows = CsvImporter.Import();
        if (rows is null) return;
        if (rows.Count == 0) { ErrorMessage = "CSV file is empty."; return; }

        var imported = await customerService.ImportBulkAsync(rows, ct);
        SuccessMessage = $"Imported {imported} customer(s).";
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task PreviousPageAsync() => RunAsync(async ct =>
    {
        if (!HasPreviousPage) return;
        CurrentPage--;
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task NextPageAsync() => RunAsync(async ct =>
    {
        if (!HasNextPage) return;
        CurrentPage++;
        await ReloadAsync(ct);
    });

    private async Task ReloadAsync(CancellationToken ct)
    {
        var search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery;
        var result = await customerService.GetPagedAsync(new PagedQuery(CurrentPage, PageSize), search, ct);
        Customers = new ObservableCollection<Customer>(result.Items);
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages == 0 ? 1 : result.TotalPages;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        PagingInfo = TotalCount > 0
            ? $"Page {CurrentPage} of {TotalPages} ({TotalCount} total)"
            : string.Empty;
    }
}
