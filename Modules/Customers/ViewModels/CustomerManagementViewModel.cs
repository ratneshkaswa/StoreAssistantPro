using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
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

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value is null) return;
        CustomerName = value.Name;
        Phone = value.Phone ?? string.Empty;
        Email = value.Email ?? string.Empty;
        Address = value.Address ?? string.Empty;
        GSTIN = value.GSTIN ?? string.Empty;
        Notes = value.Notes ?? string.Empty;
    }

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var list = await customerService.GetAllAsync(ct);
        Customers = new ObservableCollection<Customer>(list);
    });

    [RelayCommand]
    private Task SearchAsync() => RunAsync(async ct =>
    {
        var results = string.IsNullOrWhiteSpace(SearchQuery)
            ? await customerService.GetAllAsync(ct)
            : await customerService.SearchAsync(SearchQuery, ct);
        Customers = new ObservableCollection<Customer>(results);
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

        var list = await customerService.GetAllAsync(ct);
        Customers = new ObservableCollection<Customer>(list);
    });

    [RelayCommand]
    private Task ToggleActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedCustomer is null) return;
        await customerService.ToggleActiveAsync(SelectedCustomer.Id, ct);
        var list = await customerService.GetAllAsync(ct);
        Customers = new ObservableCollection<Customer>(list);
        SuccessMessage = "Active status toggled.";
    });
}
