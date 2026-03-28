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
using StoreAssistantPro.Modules.Users.Services;
using StoreAssistantPro.Modules.Users.Commands;

namespace StoreAssistantPro.Modules.Authentication.ViewModels;

/// <summary>
/// Combined user-selection + PIN-entry ViewModel for the POS login screen.
/// <para>
/// Admin requires a 4-digit PIN (entered via <see cref="PinPadViewModel"/>).
/// User logs in immediately without a PIN.
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
    private readonly IUserService _userService;
    private readonly DispatcherTimer _clockTimer;
    private Action? _resetCompleted;
    private bool _isInitialized;
    private bool _isDisposed;

    /// <summary>Reusable PIN pad — bind keypad buttons to <c>PinPad.AddDigitCommand</c> etc.</summary>
    public PinPadViewModel PinPad { get; } = new(maxLength: 4);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUserSelected))]
    [NotifyPropertyChangedFor(nameof(RoleHintText))]
    [NotifyPropertyChangedFor(nameof(IsPinRequired))]
    public partial UserType? SelectedUserType { get; set; }

    public bool IsUserSelected => SelectedUserType is not null;

    /// <summary>PIN pad is only shown for Admin role.</summary>
    public bool IsPinRequired => SelectedUserType == UserType.Admin;

    /// <summary>
    /// Controls whether the User role button is visible on the login screen.
    /// Set to false when switching from User mode so only Admin is offered.
    /// </summary>
    [ObservableProperty]
    public partial bool IsUserRoleVisible { get; set; } = true;

    public string RoleHintText => SelectedUserType switch
    {
        UserType.Admin => "Enter Admin PIN",
        UserType.User => "Logging in...",
        _ => "Select a role above"
    };

    // ── L1: Firm name ──
    public string FirmName => _appState.FirmName;

    /// <summary>True when admin PIN is still the factory default.</summary>
    public bool IsDefaultAdminPin => _appState.IsDefaultAdminPin;

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
    /// Raised after successful login with the authenticated user type.
    /// MainViewModel subscribes to transition from login to workspace.
    /// </summary>
    public Func<UserType, Task>? LoginSucceeded { get; set; }

    /// <summary>Raised after a successful PIN reset so the view can clear PasswordBoxes.</summary>
    public event Action? ResetCompleted
    {
        add => _resetCompleted += value;
        remove => _resetCompleted -= value;
    }

    public LoginViewModel(
        ICommandBus commandBus,
        IAppStateService appState,
        IRegionalSettingsService regional,
        IConnectivityMonitorService connectivity,
        IUserService userService)
        : base()
    {
        _commandBus = commandBus;
        _appState = appState;
        _regional = regional;
        _connectivity = connectivity;
        _userService = userService;

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
        if (_isInitialized || _isDisposed)
            return;

        _isInitialized = true;
        PinPad.PinCompleted += OnPinCompleted;
        PinPad.PropertyChanged += OnPinPadPropertyChanged;
        _clockTimer.Start();

        // L2: Pre-select last user if a previous login has occurred
        if (_appState.LastLoggedInUserType == UserType.Admin)
        {
            SelectedUserType = UserType.Admin;
            // Don't auto-login on pre-select — user must click or press F1/F2
        }

        _ = LoadRoleVisibilityAsync();
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
        if (!IsBusy && SelectedUserType is not null)
            LoginCommand.Execute(null);
    }

    [RelayCommand]
    private async Task SelectUserAsync(UserType userType)
    {
        if (userType == UserType.User && !IsUserRoleVisible)
            return;

        SelectedUserType = userType;
        PinPad.Reset();
        ClearMessages();

        // User role logs in immediately without PIN
        if (userType == UserType.User)
        {
            await LoginUserDirectAsync();
        }
    }

    /// <summary>Clears selected role (layered ESC).</summary>
    public void DeselectRole()
    {
        SelectedUserType = null;
        PinPad.Reset();
        ClearMessages();
    }

    private async Task LoadRoleVisibilityAsync()
    {
        try
        {
            var hasUserRole = !_appState.IsInitialSetupPending
                && await _userService.HasUserRoleAsync().ConfigureAwait(true);
            if (_isDisposed)
                return;

            IsUserRoleVisible = hasUserRole;

            if (!hasUserRole)
            {
                if (SelectedUserType == UserType.User)
                    SelectedUserType = null;

                SelectedUserType ??= UserType.Admin;
                return;
            }

            if (SelectedUserType is null && _appState.LastLoggedInUserType == UserType.User)
                SelectedUserType = UserType.User;
        }
        catch
        {
            if (_isDisposed)
                return;

            IsUserRoleVisible = false;
            SelectedUserType ??= UserType.Admin;
        }
    }

    /// <summary>Direct login for User role (no PIN required).</summary>
    private async Task LoginUserDirectAsync()
    {
        if (IsBusy)
            return;

        ClearMessages();
        IsBusy = true;
        try
        {
            var result = await _commandBus.SendAsync(
                new LoginUserCommand(UserType.User, string.Empty));

            if (result.Succeeded)
            {
                _clockTimer.Stop();
                if (LoginSucceeded is not null)
                    await LoginSucceeded(UserType.User);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Login failed.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        if (SelectedUserType is null)
        {
            ErrorMessage = "Please select a user.";
            return;
        }

        // User role uses direct login (no PIN)
        if (SelectedUserType == UserType.User)
        {
            await LoginUserDirectAsync();
            return;
        }

        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(PinPad.Pin), "Please enter your PIN.")))
            return;

        ClearMessages();
        PinPad.Lock();
        IsBusy = true;
        try
        {
            var result = await _commandBus.SendAsync(
                new LoginUserCommand(SelectedUserType.Value, PinPad.Pin));

            if (result.Succeeded)
            {
                _clockTimer.Stop();
                if (LoginSucceeded is not null)
                    await LoginSucceeded(SelectedUserType.Value);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Login failed.";
                PinPad.Reset();
            }
        }
        finally
        {
            IsBusy = false;
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
    [NotifyPropertyChangedFor(nameof(HasResetSuccessMessage))]
    public partial string ResetSuccessMessage { get; set; } = string.Empty;

    public bool HasResetSuccessMessage => !string.IsNullOrEmpty(ResetSuccessMessage);

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
        if (IsBusy)
            return;

        if (SelectedUserType is null) return;

        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(MasterPassword), "Enter your Master PIN.")
            .Rule(InputValidator.IsValidMasterPin(MasterPassword), "Master PIN must be exactly 6 digits.")
            .Rule(InputValidator.IsRequired(NewPin), "Enter a new PIN.")
            .Rule(InputValidator.IsValidUserPin(NewPin), "PIN must be exactly 4 digits.")
            .Rule(!InputValidator.IsWeakPin(NewPin), "PIN is too weak (e.g. 0000, 1234). Choose a stronger PIN.")
            .Rule(InputValidator.AreEqual(NewPin, NewPinConfirm), "PINs do not match.")))
            return;

        ClearMessages();
        IsBusy = true;
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
                _resetCompleted?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "PIN reset failed.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Cleanup ──

    public override void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _clockTimer.Stop();
        if (_isInitialized)
        {
            PinPad.PinCompleted -= OnPinCompleted;
            PinPad.PropertyChanged -= OnPinPadPropertyChanged;
            _isInitialized = false;
        }

        LoginSucceeded = null;
        _resetCompleted = null;
        base.Dispose();
    }
}
