using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Commands;

namespace StoreAssistantPro.Modules.Authentication.ViewModels;

public partial class PinLoginViewModel(ICommandBus commandBus) : BaseViewModel
{
    [ObservableProperty]
    public partial UserType UserType { get; set; }

    [ObservableProperty]
    public partial string Pin { get; set; } = string.Empty;

    public override string Title => $"{UserType} Login";

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

        var result = await commandBus.SendAsync(new LoginUserCommand(UserType, Pin));

        if (result.Succeeded)
        {
            RequestClose?.Invoke(true);
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Login failed.";
            Pin = string.Empty;
        }
    }
}
