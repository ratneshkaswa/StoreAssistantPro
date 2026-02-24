using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Tax.Commands;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Modules.Tax.ViewModels;

public partial class TaxManagementViewModel(
    ITaxService taxService,
    ICommandBus commandBus,
    IDialogService dialogService) : BaseViewModel
{
    // ── List ──

    [ObservableProperty]
    public partial ObservableCollection<TaxProfile> TaxProfiles { get; set; } = [];

    [ObservableProperty]
    public partial TaxProfile? SelectedProfile { get; set; }

    // ── Form fields ──

    [ObservableProperty]
    public partial string ProfileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TaxRate { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsProfileActive { get; set; } = true;

    [ObservableProperty]
    public partial string SuccessMessage { get; set; } = string.Empty;

    // ── Edit mode tracking ──

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    public string FormTitle => IsEditing ? "Edit Tax Profile" : "Add Tax Profile";

    partial void OnIsEditingChanged(bool value) => OnPropertyChanged(nameof(FormTitle));

    // ── Tax component breakdown (read-only) ──

    [ObservableProperty]
    public partial string TaxComponentBreakdown { get; set; } = string.Empty;

    // ── Selection → populate form ──

    partial void OnSelectedProfileChanged(TaxProfile? value)
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (value is null)
        {
            ClearForm();
            return;
        }

        ProfileName = value.ProfileName;
        TaxRate = value.Items.Sum(i => i.TaxMaster?.TaxRate ?? 0).ToString("0.##");
        IsProfileActive = value.IsActive;
        IsEditing = true;

        TaxComponentBreakdown = value.Items.Count > 0
            ? string.Join(" + ", value.Items
                .Where(i => i.TaxMaster is not null)
                .Select(i => $"{i.TaxMaster!.TaxName} ({i.TaxMaster.TaxRate}%)"))
            : string.Empty;
    }

    // ── Load ──

    [RelayCommand]
    private async Task LoadProfilesAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            var profiles = await taxService.GetAllProfilesAsync();
            TaxProfiles = new ObservableCollection<TaxProfile>(profiles);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // ── Save (Add or Update) ──

    [RelayCommand]
    private async Task SaveAsync()
    {
        SuccessMessage = string.Empty;

        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(ProfileName), "Profile name is required.")
            .Rule(decimal.TryParse(TaxRate, out var rate), "Tax rate must be a valid number.")
            .Rule(decimal.TryParse(TaxRate, out var r2) && r2 >= 0 && r2 <= 100,
                  "Tax rate must be between 0 and 100.")))
            return;

        var parsedRate = decimal.Parse(TaxRate);

        var result = await commandBus.SendAsync(new SaveTaxProfileCommand(
            IsEditing ? SelectedProfile?.Id : null,
            ProfileName.Trim(),
            parsedRate,
            IsProfileActive));

        if (result.Succeeded)
        {
            SuccessMessage = IsEditing
                ? $"'{ProfileName.Trim()}' updated."
                : $"'{ProfileName.Trim()}' added.";
            ClearForm();
            await LoadProfilesAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Save failed.";
        }
    }

    // ── Toggle active/inactive ──

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        SuccessMessage = string.Empty;

        if (SelectedProfile is null)
        {
            ErrorMessage = "Select a tax profile first.";
            return;
        }

        var newState = !SelectedProfile.IsActive;
        var verb = newState ? "activate" : "deactivate";

        if (!newState && !dialogService.Confirm(
                $"Deactivate '{SelectedProfile.ProfileName}'?\n\nThis will hide it from new product assignments.",
                "Confirm Deactivation"))
            return;

        var result = await commandBus.SendAsync(
            new ToggleTaxProfileCommand(SelectedProfile.Id, newState));

        if (result.Succeeded)
        {
            SuccessMessage = $"'{SelectedProfile.ProfileName}' {verb}d.";
            await LoadProfilesAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? $"Could not {verb}.";
        }
    }

    // ── New (clear form) ──

    [RelayCommand]
    private void NewProfile()
    {
        SelectedProfile = null;
        ClearForm();
    }

    // ── Set as default ──

    [RelayCommand]
    private async Task SetDefaultAsync()
    {
        SuccessMessage = string.Empty;

        if (SelectedProfile is null)
        {
            ErrorMessage = "Select a tax profile first.";
            return;
        }

        if (SelectedProfile.IsDefault)
        {
            ErrorMessage = "This profile is already the default.";
            return;
        }

        var result = await commandBus.SendAsync(
            new SetDefaultTaxProfileCommand(SelectedProfile.Id));

        if (result.Succeeded)
        {
            SuccessMessage = $"'{SelectedProfile.ProfileName}' is now the default.";
            await LoadProfilesAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Could not set default.";
        }
    }

    private void ClearForm()
    {
        ProfileName = string.Empty;
        TaxRate = string.Empty;
        IsProfileActive = true;
        TaxComponentBreakdown = string.Empty;
        IsEditing = false;
        ErrorMessage = string.Empty;
    }
}
