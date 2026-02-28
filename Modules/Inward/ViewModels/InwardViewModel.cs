using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Inward.Services;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Vendors.Services;

namespace StoreAssistantPro.Modules.Inward.ViewModels;

public partial class InwardViewModel(
    IInwardService inwardService,
    ICategoryService categoryService,
    IVendorService vendorService,
    IRegionalSettingsService regional) : BaseViewModel
{
    // ── Collections ──

    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<Vendor> Vendors { get; } = [];
    public ObservableCollection<InwardEntry> InwardEntries { get; } = [];
    public ObservableCollection<InwardParcelRow> ParcelRows { get; } = [];

    // ── Entry form fields (backing fields for source-gen reliability) ──

    private DateTime _inwardDate;
    public DateTime InwardDate
    {
        get => _inwardDate;
        set => SetProperty(ref _inwardDate, value);
    }

    private int _parcelCount;
    public int ParcelCount
    {
        get => _parcelCount;
        set
        {
            if (SetProperty(ref _parcelCount, value))
                RegenerateParcelRows(value);
        }
    }

    private string _transportCharges = string.Empty;
    public string TransportCharges
    {
        get => _transportCharges;
        set => SetProperty(ref _transportCharges, value);
    }

    private string _notes = string.Empty;
    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    private string _successMessage = string.Empty;
    public string SuccessMessage
    {
        get => _successMessage;
        set => SetProperty(ref _successMessage, value);
    }

    private bool _isEntryFormVisible;
    public bool IsEntryFormVisible
    {
        get => _isEntryFormVisible;
        set => SetProperty(ref _isEntryFormVisible, value);
    }

    private string _nextParcelPreview = string.Empty;
    public string NextParcelPreview
    {
        get => _nextParcelPreview;
        set => SetProperty(ref _nextParcelPreview, value);
    }

    private InwardEntry? _selectedEntry;
    public InwardEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
                OnPropertyChanged(nameof(HasSelectedEntry));
        }
    }

    public bool HasSelectedEntry => SelectedEntry is not null;

    // ── Month filter ──

    private int _filterMonth;
    public int FilterMonth
    {
        get => _filterMonth;
        set => SetProperty(ref _filterMonth, value);
    }

    private int _filterYear;
    public int FilterYear
    {
        get => _filterYear;
        set => SetProperty(ref _filterYear, value);
    }

    // ── Parcel count changed → regenerate rows ──

    private int _nextSequence = 1;

    private void RegenerateParcelRows(int count)
    {
        if (count < 0) count = 0;
        if (count > 50) count = 50;

        var existing = ParcelRows.ToList();
        ParcelRows.Clear();

        var month = InwardDate.Month;

        for (int i = 0; i < count; i++)
        {
            var seq = _nextSequence + i;
            var row = i < existing.Count ? existing[i] : new InwardParcelRow();
            row.ParcelNumber = inwardService.FormatParcelNumber(month, seq);
            ParcelRows.Add(row);
        }
    }

    // ── Load ──

    [RelayCommand]
    private Task LoadInwardAsync() => RunLoadAsync(async ct =>
    {
        SuccessMessage = string.Empty;
        var now = regional.Now;

        if (FilterMonth == 0)
        {
            FilterMonth = now.Month;
            FilterYear = now.Year;
        }

        Categories.Clear();
        foreach (var cat in await categoryService.GetAllAsync(ct))
            Categories.Add(cat);

        Vendors.Clear();
        foreach (var sup in await vendorService.GetActiveAsync(ct))
            Vendors.Add(sup);

        _nextSequence = await inwardService.GetNextSequenceAsync(now.Month, now.Year, ct);
        NextParcelPreview = inwardService.FormatParcelNumber(now.Month, _nextSequence);

        await RefreshEntriesAsync(ct);
    });

    [RelayCommand]
    private async Task RefreshEntriesAsync(CancellationToken ct = default)
    {
        InwardEntries.Clear();
        var entries = await inwardService.GetByMonthAsync(FilterMonth, FilterYear, ct);
        foreach (var entry in entries)
            InwardEntries.Add(entry);
    }

    // ── Show entry form ──

    [RelayCommand]
    private async Task ShowEntryFormAsync()
    {
        var now = regional.Now;
        InwardDate = now.Date;
        ParcelCount = 0;
        TransportCharges = string.Empty;
        Notes = string.Empty;
        ParcelRows.Clear();
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        _nextSequence = await inwardService.GetNextSequenceAsync(now.Month, now.Year);
        NextParcelPreview = inwardService.FormatParcelNumber(now.Month, _nextSequence);

        IsEntryFormVisible = true;
    }

    [RelayCommand]
    private void CancelEntry()
    {
        IsEntryFormVisible = false;
        ParcelRows.Clear();
        ErrorMessage = string.Empty;
    }

    // ── Save ──

    [RelayCommand]
    private Task SaveInwardAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        if (ParcelRows.Count == 0)
        {
            ErrorMessage = "Enter the number of parcels received.";
            return;
        }

        if (!Validate(v => v
            .Rule(ParcelRows.Count > 0, "At least one parcel is required.")
            .Rule(ParcelRows.All(r => r.SelectedCategory is not null), "Select a category for each parcel.")
            .Rule(ParcelRows.All(r => r.SelectedVendor is not null), "Select a vendor for each parcel.")))
            return;

        decimal transport = 0;
        if (!string.IsNullOrWhiteSpace(TransportCharges))
        {
            if (!decimal.TryParse(TransportCharges.Trim(), out transport) || transport < 0)
            {
                ErrorMessage = "Enter a valid transport charges amount.";
                return;
            }
        }

        var month = InwardDate.Month;
        var entry = new InwardEntry
        {
            InwardNumber = inwardService.FormatInwardNumber(month, _nextSequence),
            InwardDate = InwardDate,
            ParcelCount = ParcelRows.Count,
            TransportCharges = transport,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            Parcels = ParcelRows.Select(r => new InwardParcel
            {
                ParcelNumber = r.ParcelNumber,
                CategoryId = r.SelectedCategory?.Id,
                VendorId = r.SelectedVendor?.Id,
                Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim()
            }).ToList()
        };

        await inwardService.SaveInwardEntryAsync(entry, ct);

        SuccessMessage = $"Inward entry {entry.InwardNumber} saved with {entry.ParcelCount} parcel(s). Transport: ₹{transport:N2}";

        IsEntryFormVisible = false;
        ParcelRows.Clear();

        _nextSequence = await inwardService.GetNextSequenceAsync(InwardDate.Month, InwardDate.Year, ct);
        NextParcelPreview = inwardService.FormatParcelNumber(InwardDate.Month, _nextSequence);
        await RefreshEntriesAsync(ct);
    });

    // ── Delete ──

    [RelayCommand]
    private Task DeleteEntryAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;

        if (SelectedEntry is null)
        {
            ErrorMessage = "Select an inward entry to delete.";
            return;
        }

        await inwardService.DeleteAsync(SelectedEntry.Id, SelectedEntry.RowVersion, ct);
        SuccessMessage = $"Inward entry {SelectedEntry.InwardNumber} deleted.";
        SelectedEntry = null;

        _nextSequence = await inwardService.GetNextSequenceAsync(FilterMonth, FilterYear, ct);
        NextParcelPreview = inwardService.FormatParcelNumber(FilterMonth, _nextSequence);
        await RefreshEntriesAsync(ct);
    });
}
