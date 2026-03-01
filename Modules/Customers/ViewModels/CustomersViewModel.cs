using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Customers.Commands;
using StoreAssistantPro.Modules.Customers.Services;

namespace StoreAssistantPro.Modules.Customers.ViewModels;

public partial class CustomersViewModel(
    ICustomerService customerService,
    ICommandBus commandBus) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<Customer> Customers { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedCustomer))]
    public partial Customer? SelectedCustomer { get; set; }

    public bool HasSelectedCustomer => SelectedCustomer is not null;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CountDisplay { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SuccessMessage { get; set; } = string.Empty;

    // ── Add form ──

    [ObservableProperty]
    public partial bool IsAddFormVisible { get; set; }

    [ObservableProperty]
    public partial string NewName { get; set; } = string.Empty;

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

    private List<Customer> _allCustomers = [];

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private Task LoadCustomersAsync() => RunLoadAsync(async _ =>
    {
        _allCustomers = await customerService.GetAllAsync();
        ApplyFilter();
        CountDisplay = $"{_allCustomers.Count(c => c.IsActive)} active / {_allCustomers.Count} total";
    });

    private void ApplyFilter()
    {
        var filtered = _allCustomers.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            filtered = filtered.Where(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (c.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.GSTIN?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        Customers = new ObservableCollection<Customer>(filtered);
    }

    [RelayCommand]
    private void ShowAddForm()
    {
        IsAddFormVisible = true;
        ClearAddForm();
    }

    [RelayCommand]
    private void CancelAdd() => IsAddFormVisible = false;

    [RelayCommand]
    private async Task SaveCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName))
        {
            ErrorMessage = "Name is required.";
            return;
        }

        var customer = new Customer
        {
            Name = NewName.Trim(),
            Phone = string.IsNullOrWhiteSpace(NewPhone) ? null : NewPhone.Trim(),
            Email = string.IsNullOrWhiteSpace(NewEmail) ? null : NewEmail.Trim(),
            Address = string.IsNullOrWhiteSpace(NewAddress) ? null : NewAddress.Trim(),
            GSTIN = string.IsNullOrWhiteSpace(NewGSTIN) ? null : NewGSTIN.Trim(),
            Notes = string.IsNullOrWhiteSpace(NewNotes) ? null : NewNotes.Trim()
        };

        var result = await commandBus.SendAsync(new SaveCustomerCommand(customer));
        if (result.Succeeded)
        {
            IsAddFormVisible = false;
            SuccessMessage = $"Customer '{customer.Name}' added.";
            await LoadCustomersAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to save customer.";
        }
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync()
    {
        if (SelectedCustomer is null) return;
        var result = await commandBus.SendAsync(new DeleteCustomerCommand(SelectedCustomer.Id));
        if (result.Succeeded)
        {
            SuccessMessage = $"Customer '{SelectedCustomer.Name}' deleted.";
            await LoadCustomersAsync();
        }
    }

    private void ClearAddForm()
    {
        NewName = NewPhone = NewEmail = NewAddress = NewGSTIN = NewNotes = string.Empty;
        ErrorMessage = string.Empty;
    }
}
