using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Commands;

namespace StoreAssistantPro.Modules.Authentication.ViewModels;

/// <summary>
/// Combined user-selection + PIN-entry ViewModel for the POS login screen.
/// Replaces the separate <c>UserSelectionViewModel</c> and <c>PinLoginViewModel</c>.
/// <para>
/// Composes <see cref="PinPadViewModel"/> for reusable PIN pad logic.
/// When the PIN reaches 4 digits and a user is selected, login executes
/// automatically (POS auto-login).
/// </para>
/// </summary>
public partial class UnifiedLoginViewModel(ICommandBus commandBus) : BaseViewModel
{
    /// <summary>Reusable PIN pad — bind keypad buttons to <c>PinPad.AddDigitCommand</c> etc.</summary>
    public PinPadViewModel PinPad { get; } = new(maxLength: 4);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUserSelected))]
    public partial UserType? SelectedUserType { get; set; }

    public bool IsUserSelected => SelectedUserType is not null;

    public Action<bool?>? RequestClose { get; set; }

    public void Initialize()
    {
        PinPad.PinCompleted += OnPinCompleted;
    }

    private void OnPinCompleted()
    {
        if (SelectedUserType is not null)
            LoginCommand.Execute(null);
    }

    [RelayCommand]
    private void SelectUser(UserType userType)
    {
        SelectedUserType = userType;
        PinPad.Reset();
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (SelectedUserType is null)
        {
            ErrorMessage = "Please select a user.";
            return;
        }

        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(PinPad.Pin), "Please enter your PIN.")))
            return;

        PinPad.Lock();
        try
        {
            var result = await commandBus.SendAsync(
                new LoginUserCommand(SelectedUserType.Value, PinPad.Pin));

            if (result.Succeeded)
            {
                RequestClose?.Invoke(true);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Login failed.";
                PinPad.Reset();
            }
        }
        finally
        {
            PinPad.Unlock();
        }
    }
}
