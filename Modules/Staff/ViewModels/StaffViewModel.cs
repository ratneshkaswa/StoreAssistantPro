using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Staff.Commands;
using StoreAssistantPro.Modules.Staff.Services;

namespace StoreAssistantPro.Modules.Staff.ViewModels;

public partial class StaffViewModel(
    IStaffService staffService,
    ICommandBus commandBus) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<Models.Staff> StaffMembers { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedStaff))]
    public partial Models.Staff? SelectedStaff { get; set; }

    public bool HasSelectedStaff => SelectedStaff is not null;

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
    public partial string NewStaffCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewPhone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewRole { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewDailyTarget { get; set; } = string.Empty;

    private List<Models.Staff> _allStaff = [];

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private Task LoadStaffAsync() => RunLoadAsync(async _ =>
    {
        _allStaff = await staffService.GetAllAsync();
        ApplyFilter();
        CountDisplay = $"{_allStaff.Count(s => s.IsActive)} active / {_allStaff.Count} total";
    });

    private void ApplyFilter()
    {
        var filtered = _allStaff.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            filtered = filtered.Where(s =>
                s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (s.StaffCode?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        StaffMembers = new ObservableCollection<Models.Staff>(filtered);
    }

    [RelayCommand]
    private void ShowAddForm()
    {
        IsAddFormVisible = true;
        NewName = NewStaffCode = NewPhone = NewRole = NewDailyTarget = string.Empty;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelAdd() => IsAddFormVisible = false;

    [RelayCommand]
    private async Task SaveStaffAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName))
        {
            ErrorMessage = "Name is required.";
            return;
        }

        var staff = new Models.Staff
        {
            Name = NewName.Trim(),
            StaffCode = string.IsNullOrWhiteSpace(NewStaffCode) ? null : NewStaffCode.Trim(),
            Phone = string.IsNullOrWhiteSpace(NewPhone) ? null : NewPhone.Trim(),
            Role = string.IsNullOrWhiteSpace(NewRole) ? null : NewRole.Trim(),
            DailyTarget = decimal.TryParse(NewDailyTarget, out var target) ? target : 0m
        };

        var result = await commandBus.SendAsync(new SaveStaffCommand(staff));
        if (result.Succeeded)
        {
            IsAddFormVisible = false;
            SuccessMessage = $"Staff '{staff.Name}' added.";
            await LoadStaffAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to save staff.";
        }
    }

    [RelayCommand]
    private async Task DeleteStaffAsync()
    {
        if (SelectedStaff is null) return;
        var result = await commandBus.SendAsync(new DeleteStaffCommand(SelectedStaff.Id));
        if (result.Succeeded)
        {
            SuccessMessage = $"Staff '{SelectedStaff.Name}' deleted.";
            await LoadStaffAsync();
        }
    }
}
