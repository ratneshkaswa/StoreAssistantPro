using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Inward.ViewModels;

/// <summary>
/// Bindable row model for a single parcel in the inward entry form.
/// Each row has auto-generated parcel number, category, and vendor selection.
/// </summary>
public class InwardParcelRow : ObservableObject
{
    private string _parcelNumber = string.Empty;
    public string ParcelNumber
    {
        get => _parcelNumber;
        set => SetProperty(ref _parcelNumber, value);
    }

    private Category? _selectedCategory;
    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    private Vendor? _selectedVendor;
    public Vendor? SelectedVendor
    {
        get => _selectedVendor;
        set => SetProperty(ref _selectedVendor, value);
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
}
