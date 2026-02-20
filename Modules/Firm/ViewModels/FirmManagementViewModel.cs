using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Modules.Firm.Events;
using StoreAssistantPro.Modules.Firm.Services;

namespace StoreAssistantPro.Modules.Firm.ViewModels;

public partial class FirmManagementViewModel(
    IFirmService firmService,
    IEventBus eventBus) : BaseViewModel
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Firm name is required.")]
    [MaxLength(200, ErrorMessage = "Firm name cannot exceed 200 characters.")]
    public partial string FirmName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Address { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SuccessMessage { get; set; } = string.Empty;

    [RelayCommand]
    private async Task LoadFirmAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            var config = await firmService.GetFirmAsync();
            if (config is null) return;

            FirmName = config.FirmName;
            Address = config.Address;
            Phone = config.Phone;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveFirmAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        ValidateAllProperties();
        if (HasErrors)
            return;

        try
        {
            var trimmedName = FirmName.Trim();
            await firmService.UpdateFirmAsync(trimmedName, Address.Trim(), Phone.Trim());
            await eventBus.PublishAsync(new FirmUpdatedEvent(trimmedName));
            SuccessMessage = "Firm information saved.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
