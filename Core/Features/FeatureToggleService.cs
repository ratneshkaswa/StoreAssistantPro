using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Features;

/// <summary>
/// In-memory feature flag store with operational-mode filtering,
/// focus-lock awareness, and offline-mode restrictions.
/// Defaults to enabled for all unknown features (opt-out model).
/// <para>
/// Call <see cref="Load"/> once at startup with the flags read
/// from configuration. Call <see cref="SetMode"/> when the
/// application switches between Management and Billing modes.
/// Both raise <see cref="ObservableObject.PropertyChanged"/>
/// so bound ViewModel properties refresh automatically.
/// </para>
/// <para>
/// <b>Layering (evaluated top-to-bottom, first false wins):</b>
/// <list type="number">
///   <item>Config flag — explicitly disabled → <c>false</c>.</item>
///   <item>Focus lock — locked to a module and this feature is not
///         in the allowed set → <c>false</c>.</item>
///   <item>Offline mode — database unreachable and this feature
///         requires DB access → <c>false</c>.</item>
///   <item>Operational mode — feature not permitted in the current
///         mode → <c>false</c>.</item>
///   <item>Otherwise → <c>true</c>.</item>
/// </list>
/// </para>
/// </summary>
public partial class FeatureToggleService : ObservableObject, IFeatureToggleService, IDisposable
{
    private readonly IFocusLockService _focusLock;
    private readonly IAppStateService _appState;
    private Dictionary<string, bool> _flags = [];
    private bool _disposed;

    public FeatureToggleService(IFocusLockService focusLock, IAppStateService appState)
    {
        _focusLock = focusLock;
        _appState = appState;
        CurrentMode = _appState.CurrentMode;
        _focusLock.PropertyChanged += OnFocusLockPropertyChanged;
        _appState.PropertyChanged += OnAppStatePropertyChanged;
    }

    /// <summary>
    /// Features disabled when in <see cref="OperationalMode.Billing"/>.
    /// </summary>
    private static readonly HashSet<string> ManagementOnlyFeatures = new(StringComparer.OrdinalIgnoreCase)
    {
        FeatureFlags.UserManagement,
        FeatureFlags.FirmManagement,
        FeatureFlags.TaxManagement,
        FeatureFlags.SystemSettings
    };

    /// <summary>
    /// Features disabled when in <see cref="OperationalMode.Management"/>.
    /// </summary>
    private static readonly HashSet<string> BillingOnlyFeatures = new(StringComparer.OrdinalIgnoreCase)
    {
        FeatureFlags.AdvancedBilling
    };

    /// <summary>
    /// Features that remain enabled while the "Billing" focus lock
    /// is active. Everything else is disabled.
    /// </summary>
    private static readonly HashSet<string> FocusLockAllowedFeatures = new(StringComparer.OrdinalIgnoreCase)
    {
        FeatureFlags.Billing,
        FeatureFlags.AdvancedBilling,
        FeatureFlags.Sales
    };

    /// <summary>
    /// Features that remain enabled while the application is in
    /// offline mode (database unreachable). Everything else is
    /// disabled because it requires database access.
    /// <para>
    /// <b>Allowed:</b> Billing and Sales — bills are queued locally
    /// via <c>IOfflineBillingQueue</c> and synced when connectivity
    /// is restored.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> OfflineAllowedFeatures = new(StringComparer.OrdinalIgnoreCase)
    {
        FeatureFlags.Billing,
        FeatureFlags.AdvancedBilling,
        FeatureFlags.Sales
    };

    [ObservableProperty]
    public partial OperationalMode CurrentMode { get; set; }

    public IReadOnlyDictionary<string, bool> AllFlags => _flags;

    public bool IsEnabled(string featureName)
    {
        // Layer 1: Config flag check (unknown features default to enabled)
        if (_flags.TryGetValue(featureName, out var enabled) && !enabled)
            return false;

        // Layer 2: Focus lock — when locked, only allowed features pass
        if (_focusLock.IsFocusLocked && !FocusLockAllowedFeatures.Contains(featureName))
            return false;

        // Layer 3: Offline mode — only billing/sales features pass
        if (_appState.IsOfflineMode && !OfflineAllowedFeatures.Contains(featureName))
            return false;

        // Layer 4: Mode-based filtering
        return CurrentMode switch
        {
            OperationalMode.Billing => !ManagementOnlyFeatures.Contains(featureName),
            OperationalMode.Management => !BillingOnlyFeatures.Contains(featureName),
            _ => true
        };
    }

    public void Load(IReadOnlyDictionary<string, bool> flags)
    {
        _flags = new Dictionary<string, bool>(flags, StringComparer.OrdinalIgnoreCase);
        OnPropertyChanged(string.Empty);
    }

    public void SetMode(OperationalMode mode)
    {
        if (CurrentMode == mode)
            return;

        _appState.SetMode(mode);

        if (CurrentMode != mode)
            CurrentMode = mode;

        // Notify all bound properties so UI refreshes even when the
        // app-state implementation is substituted in tests.
        OnPropertyChanged(string.Empty);
    }

    private void OnFocusLockPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IFocusLockService.IsFocusLocked))
        {
            // Re-evaluate all feature flags when focus lock changes
            OnPropertyChanged(string.Empty);
        }
    }

    private void OnAppStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppStateService.CurrentMode))
        {
            if (CurrentMode != _appState.CurrentMode)
                CurrentMode = _appState.CurrentMode;

            OnPropertyChanged(string.Empty);
            return;
        }

        if (e.PropertyName == nameof(IAppStateService.IsOfflineMode))
        {
            // Re-evaluate all feature flags when offline mode changes
            OnPropertyChanged(string.Empty);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _focusLock.PropertyChanged -= OnFocusLockPropertyChanged;
        _appState.PropertyChanged -= OnAppStatePropertyChanged;
        GC.SuppressFinalize(this);
    }
}
