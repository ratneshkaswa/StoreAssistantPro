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
/// </summary>
public partial class LoginViewModel : BaseViewModel
{
    private readonly ICommandBus _commandBus;
    private readonly IAppStateService _appState;
    private readonly IRegionalSettingsService _regional;
    private readonly IConnectivityMonitorService _connectivity;
    private readonly DispatcherTimer _clockTimer;

    // Failed attempt tracking per role
    private readonly Dictionary<UserType, int> _failedAttempts = new();
    private readonly Dictionary<UserType, DateTime?> _lockoutUntil = new();
    private const int MaxAttempts = 5;
    private const int LockoutSeconds = 30;

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

    public bool IsLastUsedAdmin => LastLoggedInUser == UserType.Admin;
    public bool IsLastUsedManager => LastLoggedInUser == UserType.Manager;
    public bool IsLastUsedUser => LastLoggedInUser == UserType.User;

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

    // ── L3: Failed attempts ──
    [ObservableProperty]
    public partial string AttemptsMessage { get; set; }

    [ObservableProperty]
    public partial bool IsRoleLocked { get; set; }

    // ── L2: Last user pre-selection ──
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLastUsedAdmin))]
    [NotifyPropertyChangedFor(nameof(IsLastUsedManager))]
    [NotifyPropertyChangedFor(nameof(IsLastUsedUser))]
    public partial UserType? LastLoggedInUser { get; set; }

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
        AttemptsMessage = string.Empty;

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

        // L2: Pre-select last user only if firm is set up (meaning prior logins happened)
        if (!string.IsNullOrEmpty(_appState.FirmName) && _appState.CurrentUserType is var lastUser
            && lastUser != default(UserType))
        {
            LastLoggedInUser = lastUser;
            SelectedUserType = lastUser;
        }
    }

    /// <summary>Clear error as soon as user starts typing a new PIN.</summary>
    private void OnPinPadPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PinPadViewModel.PinLength) && PinPad.PinLength > 0)
        {
            if (HasError) ErrorMessage = string.Empty;
            if (!string.IsNullOrEmpty(ResetSuccessMessage)) ResetSuccessMessage = string.Empty;
        }
    }

    private void OnPinCompleted()
    {
        if (SelectedUserType is not null)
            LoginCommand.Execute(null);
    }

    [RelayCommand]
    private void SelectUser(UserType userType)
    {
        // L3: Check lockout
        if (_lockoutUntil.TryGetValue(userType, out var until) && until > _regional.Now)
        {
            var remaining = (int)(until.Value - _regional.Now).TotalSeconds;
            ErrorMessage = $"{userType} is locked. Try again in {remaining}s.";
            return;
        }

        SelectedUserType = userType;
        PinPad.Reset();
        ErrorMessage = string.Empty;
        IsRoleLocked = false;
        UpdateAttemptsMessage();
    }

    /// <summary>Clears selected role (layered ESC).</summary>
    public void DeselectRole()
    {
        SelectedUserType = null;
        PinPad.Reset();
        ErrorMessage = string.Empty;
        AttemptsMessage = string.Empty;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (SelectedUserType is null)
        {
            ErrorMessage = "Please select a user.";
            return;
        }

        // L3: Check lockout
        if (_lockoutUntil.TryGetValue(SelectedUserType.Value, out var until) && until > _regional.Now)
        {
            var remaining = (int)(until.Value - _regional.Now).TotalSeconds;
            ErrorMessage = $"Too many attempts. Locked for {remaining}s.";
            PinPad.Reset();
            return;
        }

        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(PinPad.Pin), "Please enter your PIN.")))
            return;

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
                // L3: Track failed attempts
                var role = SelectedUserType.Value;
                _failedAttempts.TryGetValue(role, out var count);
                count++;
                _failedAttempts[role] = count;

                if (count >= MaxAttempts)
                {
                    _lockoutUntil[role] = _regional.Now.AddSeconds(LockoutSeconds);
                    _failedAttempts[role] = 0;
                    IsRoleLocked = true;
                    ErrorMessage = $"Too many failed attempts. {role} locked for {LockoutSeconds}s.";
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Login failed.";
                }

                UpdateAttemptsMessage();
                PinPad.Reset();
            }
        }
        finally
        {
            IsVerifying = false;
            PinPad.Unlock();
        }
    }

    private void UpdateAttemptsMessage()
    {
        if (SelectedUserType is null) { AttemptsMessage = string.Empty; return; }

        _failedAttempts.TryGetValue(SelectedUserType.Value, out var count);
        AttemptsMessage = count > 0
            ? $"{MaxAttempts - count} attempt(s) remaining"
            : string.Empty;
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
        ErrorMessage = string.Empty;
        ResetSuccessMessage = string.Empty;
        MasterPassword = string.Empty;
        NewPin = string.Empty;
        NewPinConfirm = string.Empty;
        PinPad.Reset();
    }

    [RelayCommand]
    private void CancelForgotPin()
    {
        IsForgotPinMode = false;
        ErrorMessage = string.Empty;
        ResetSuccessMessage = string.Empty;
    }

    [RelayCommand]
    private async Task ResetPinAsync()
    {
        if (SelectedUserType is null) return;

        if (!Validate(v => v
            .Rule(InputValidator.IsRequired(MasterPassword), "Enter your Master Password.")
            .Rule(InputValidator.IsRequired(NewPin), "Enter a new PIN.")
            .Rule(InputValidator.IsValidUserPin(NewPin), "PIN must be exactly 4 digits.")
            .Rule(InputValidator.AreEqual(NewPin, NewPinConfirm), "PINs do not match.")))
            return;

        IsVerifying = true;
        try
        {
            var result = await _commandBus.SendAsync(
                new ChangePinCommand(SelectedUserType.Value, NewPin, MasterPassword));

            if (result.Succeeded)
            {
                ResetSuccessMessage = $"{SelectedUserType} PIN has been reset. Please login.";
                ErrorMessage = string.Empty;
                IsForgotPinMode = false;
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
}
