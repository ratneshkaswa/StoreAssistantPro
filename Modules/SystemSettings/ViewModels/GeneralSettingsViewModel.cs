using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Firm.Events;
using StoreAssistantPro.Modules.Firm.Services;

namespace StoreAssistantPro.Modules.SystemSettings.ViewModels;

public partial class GeneralSettingsViewModel(
    IFirmService firmService,
    IAppStateService appState,
    IEventBus eventBus) : BaseViewModel
{
    [ObservableProperty]
    public partial string FirmName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DefaultCurrency { get; set; } = "INR";

    [ObservableProperty]
    public partial string SuccessMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool SmartTooltipsEnabled { get; set; }

    public string[] AvailableCurrencies { get; } = ["USD", "EUR", "GBP", "INR", "CAD", "AUD"];

    partial void OnSmartTooltipsEnabledChanged(bool value) =>
        appState.SetSmartTooltipsEnabled(value);

    [RelayCommand]
    private async Task LoadAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        SmartTooltipsEnabled = appState.SmartTooltipsEnabled;

        try
        {
            var config = await firmService.GetFirmAsync();
            if (config is not null)
            {
                FirmName = config.FirmName;
                DefaultCurrency = config.CurrencyCode;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FirmName))
        {
            ErrorMessage = "Firm name is required.";
            return;
        }

        try
        {
            var trimmedName = FirmName.Trim();
            var config = await firmService.GetFirmAsync();
            await firmService.UpdateFirmAsync(
                trimmedName,
                config?.Address ?? string.Empty,
                config?.Phone ?? string.Empty,
                config?.GSTNumber,
                DefaultCurrency);

            await eventBus.PublishAsync(new FirmUpdatedEvent(trimmedName));
            SuccessMessage = "General settings saved.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
