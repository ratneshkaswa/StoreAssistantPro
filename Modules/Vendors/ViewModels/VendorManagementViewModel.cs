using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Vendors.Services;

namespace StoreAssistantPro.Modules.Vendors.ViewModels;

public partial class VendorManagementViewModel(IVendorService vendorService) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<Vendor> Vendors { get; set; } = [];

    [ObservableProperty]
    public partial Vendor? SelectedVendor { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

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
        if (value is null) return;
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

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var vendors = await vendorService.GetAllAsync(ct);
        Vendors = new ObservableCollection<Vendor>(vendors);
    });

    [RelayCommand]
    private Task SearchAsync() => RunAsync(async ct =>
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadAsync();
            return;
        }
        var results = await vendorService.SearchAsync(SearchText, ct);
        Vendors = new ObservableCollection<Vendor>(results);
    });

    [RelayCommand]
    private void NewVendor()
    {
        SelectedVendor = null;
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
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(VendorName), "Vendor name is required.")))
            return;

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

        await LoadAsync();
        NewVendor();
    });

    [RelayCommand]
    private Task ToggleActiveAsync() => RunAsync(async ct =>
    {
        if (SelectedVendor is null) return;
        await vendorService.ToggleActiveAsync(SelectedVendor.Id, ct);
        await LoadAsync();
        SuccessMessage = "Status toggled.";
    });
}
