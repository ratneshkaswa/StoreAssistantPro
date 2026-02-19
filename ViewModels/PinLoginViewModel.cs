using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Models;
using StoreAssistantPro.Services;

namespace StoreAssistantPro.ViewModels;

public partial class PinLoginViewModel(ILoginService loginService) : ObservableObject
{
    [ObservableProperty]
    public partial UserType UserType { get; set; }

    [ObservableProperty]
    public partial string Pin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public string Title => $"{UserType} Login";

    public Action<bool?>? RequestClose { get; set; }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Pin))
        {
            ErrorMessage = "Please enter your PIN.";
            return;
        }

        var isValid = await loginService.ValidatePinAsync(UserType, Pin);
        if (isValid)
        {
            RequestClose?.Invoke(true);
        }
        else
        {
            ErrorMessage = "Invalid PIN. Try again.";
            Pin = string.Empty;
        }
    }
}
