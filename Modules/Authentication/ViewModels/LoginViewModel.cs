using System.ComponentModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Users.Commands;

namespace StoreAssistantPro.Modules.Authentication.ViewModels;

/// <summary>
/// Combined user-selection + PIN-entry ViewModel for the POS login screen.
/// <para>
/// Composes <see cref="PinPadViewModel"/> for reusable PIN pad logic.
/// When the PIN reaches 4 digits and a user is selected, login executes
/// automatically (POS auto-login). Error is cleared on next digit input.
/// </para>
/// <para>
/// <b>State integrity:</b> All UI messages (<see cref="BaseViewModel.ErrorMessage"/>,
/// <see cref="ResetSuccessMessage"/>) are mutually exclusive. Every state
/// transition calls <see cref="ClearMessages"/> before setting the new message,
/// preventing contradictory text on screen.
/// </para>
/// </summary>
public partial class LoginViewModel : BaseViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly IAppStateService _appState;
    private readonly IRegionalSettingsService _regional;
    private readonly IConnectivityMonitorService _connectivity;
    private readonly DispatcherTimer _clockTimer;

    /// <summary>Reusable PIN pad — bind keypad buttons to <c>PinPad.AddDigitCommand</c> etc.</summary>
    public PinPadViewModel PinPad { get; } = new(maxLength: 4);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUserSelected))]
    [NotifyPropertyChangedFor(nameof(RoleHintText))]
    public partial UserType? SelectedUserType { get; set; }

    public bool IsUserSelected => SelectedUserType is not null;

    public string RoleHintText => SelectedUserType is { } role
        ? $"Enter {role} PIN"
        : "Select a role above";

    [ObservableProperty]
    public partial bool IsVerifying { get; set; }

    // ── L1: Firm name ──
    public string FirmName => _appState.FirmName;

    // ── L15: Connection status ──
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectionStatus))]
    public partial bool IsOffline { get; set; }

    public string ConnectionStatus => IsOffline ? "● Offline" : "● Connected";
    public string AppVersion { get; } =
        System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";

    // ── L9: Clock ──
    [ObservableProperty]
    public partial string CurrentTime { get; set; }

    /// <summary>
    /// Kept for XAML binding compatibility. Server <see cref="BaseViewModel.ErrorMessage"/>
    /// now carries all attempt/lockout info — this property is always empty.
    /// </summary>
    public string AttemptsMessage => string.Empty;

    public Action<bool?>? RequestClose { get; set; }

    /// <summary>Raised after a successful PIN reset so the view can clear PasswordBoxes.</summary>
    public event Action? ResetCompleted;

    public LoginViewModel(
        ICommandBus commandBus,
        IAppStateService appState,
        IRegionalSettingsService regional,
        IConnectivityMonitorService connectivity)
        : base()
    {
        _commandBus = commandBus;
        _appState = appState;
        _regional = regional;
        _connectivity = connectivity;

        CurrentTime = _regional.FormatTime(_regional.Now);

        // L9: Clock timer + connectivity refresh
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) =>
        {
            CurrentTime = _regional.FormatTime(_regional.Now);
            IsOffline = !_connectivity.IsConnected;
        };
    }

    public void Initialize()
    {
        PinPad.PinCompleted += OnPinCompleted;
        PinPad.PropertyChanged += OnPinPadPropertyChanged;
        _clockTimer.Start();

        // L2: Pre-select last user if a previous login has occurred
        if (_appState.LastLoggedInUserType is { } lastUser)
        {
            SelectedUserType = lastUser;
        }
    }

    /// <summary>
    /// Single point of message cleanup. Enforces mutual exclusion:
    /// only one message type can be visible at any time.
    /// </summary>
    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        ResetSuccessMessage = string.Empty;
    }

    /// <summary>Clear error as soon as user starts typing a new PIN.</summary>
    private void OnPinPadPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PinPadViewModel.PinLength) && PinPad.PinLength > 0)
            ClearMessages();
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
        ClearMessages();
    }

    /// <summary>Clears selected role (layered ESC).</summary>
    public void DeselectRole()
    {
        SelectedUserType = null;
        PinPad.Reset();
        ClearMessages();
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

        ClearMessages();
        PinPad.Lock();
        IsVerifying = true;
        try
        {
            var result = await _commandBus.SendAsync(
                new LoginUserCommand(SelectedUserType.Value, PinPad.Pin));

            if (result.Succeeded)
            {
                _clockTimer.Stop();
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
            IsVerifying = false;
            PinPad.Unlock();
        }
    }

    // ── Forgot PIN recovery ──
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNormalMode))]
    public partial bool IsForgotPinMode { get; set; }

    public bool IsNormalMode => !IsForgotPinMode;

    public string MasterPassword { get; set; } = string.Empty;
    public string NewPin { get; set; } = string.Empty;
    public string NewPinConfirm { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResetSuccessMessage { get; set; } = string.Empty;

    [RelayCommand]
    private void ForgotPin()
    {
        if (SelectedUserType is null)
        {
            ErrorMessage = "Select a role first.";
            return;
        }

        IsForgotPinMode = true;
        ClearMessages();
        MasterPassword = string.Empty;
        NewPin = string.Empty;
        NewPinConfirm = string.Empty;
        PinPad.Reset();
    }

    [RelayCommand]
    private void CancelForgotPin()
    {
        IsForgotPinMode = false;
        ClearMessages();
    }

    [RelayCommand]
    private async Task ResetPinAsync()
    {
        if (SelectedUserType is null) return;

        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(MasterPassword), "Enter your Master PIN.")
            .Rule(InputValidator.IsRequired(NewPin), "Enter a new PIN.")
            .Rule(InputValidator.IsValidUserPin(NewPin), "PIN must be exactly 4 digits.")
            .Rule(InputValidator.AreEqual(NewPin, NewPinConfirm), "PINs do not match.")))
            return;

        ClearMessages();
        IsVerifying = true;
        try
        {
            var result = await _commandBus.SendAsync(
                new ChangePinCommand(SelectedUserType.Value, NewPin, MasterPassword));

            if (result.Succeeded)
            {
                IsForgotPinMode = false;
                ResetSuccessMessage = $"{SelectedUserType} PIN has been reset. Please login.";
                PinPad.Reset();
                MasterPassword = string.Empty;
                NewPin = string.Empty;
                NewPinConfirm = string.Empty;
                ResetCompleted?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "PIN reset failed.";
            }
        }
        finally
        {
            IsVerifying = false;
        }
    }

    // ── Cleanup ──

    public override void Dispose()
    {
        _clockTimer.Stop();
        PinPad.PinCompleted -= OnPinCompleted;
        PinPad.PropertyChanged -= OnPinPadPropertyChanged;
        base.Dispose();
    }
}
