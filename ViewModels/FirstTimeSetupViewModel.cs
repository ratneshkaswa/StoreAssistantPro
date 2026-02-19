using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Services;

namespace StoreAssistantPro.ViewModels;

public partial class FirstTimeSetupViewModel(ISetupService setupService) : ObservableObject
{
    [ObservableProperty]
    public partial string FirmName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AdminPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ManagerPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MasterPin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public Action<bool?>? RequestClose { get; set; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FirmName))
        {
            ErrorMessage = "Firm name is required.";
            return;
        }

        if (!IsValidPin(AdminPin, 4))
        {
            ErrorMessage = "Admin PIN must be exactly 4 digits.";
            return;
        }

        if (!IsValidPin(ManagerPin, 4))
        {
            ErrorMessage = "Manager PIN must be exactly 4 digits.";
            return;
        }

        if (!IsValidPin(UserPin, 4))
        {
            ErrorMessage = "User PIN must be exactly 4 digits.";
            return;
        }

        if (!IsValidPin(MasterPin, 6))
        {
            ErrorMessage = "Master Password must be exactly 6 digits.";
            return;
        }

        await setupService.InitializeAppAsync(
            FirmName.Trim(), AdminPin, ManagerPin, UserPin, MasterPin);

        RequestClose?.Invoke(true);
    }

    private static bool IsValidPin(string pin, int length) =>
        pin.Length == length && pin.All(char.IsDigit);
}
