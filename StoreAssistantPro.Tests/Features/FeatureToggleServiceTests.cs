using System.ComponentModel;
using NSubstitute;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Features;

public class FeatureToggleServiceTests
{
    private readonly IFocusLockService _focusLock = Substitute.For<IFocusLockService>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();

    private FeatureToggleService CreateSut() => new(_focusLock, _appState);

    [Fact]
    public void IsEnabled_UnknownFeature_ReturnsTrue()
    {
        var sut = CreateSut();

        Assert.True(sut.IsEnabled("UnknownFeature"));
    }

    [Fact]
    public void IsEnabled_ExplicitlyEnabled_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.Load(new Dictionary<string, bool> { ["Products"] = true });

        Assert.True(sut.IsEnabled("Products"));
    }

    [Fact]
    public void IsEnabled_ExplicitlyDisabled_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.Load(new Dictionary<string, bool> { ["Billing"] = false });

        Assert.False(sut.IsEnabled("Billing"));
    }

    [Fact]
    public void IsEnabled_CaseInsensitive()
    {
        var sut = CreateSut();
        sut.Load(new Dictionary<string, bool> { ["billing"] = false });

        Assert.False(sut.IsEnabled("Billing"));
        Assert.False(sut.IsEnabled("BILLING"));
        Assert.False(sut.IsEnabled("billing"));
    }

    [Fact]
    public void Load_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        var raised = false;
        sut.PropertyChanged += (_, _) => raised = true;

        sut.Load(new Dictionary<string, bool> { ["Products"] = true });

        Assert.True(raised);
    }

    [Fact]
    public void AllFlags_ReturnsLoadedDictionary()
    {
        var sut = CreateSut();
        var flags = new Dictionary<string, bool>
        {
            ["Products"] = true,
            ["Billing"] = false,
            ["Reports"] = false
        };

        sut.Load(flags);

        Assert.Equal(3, sut.AllFlags.Count);
        Assert.True(sut.AllFlags["Products"]);
        Assert.False(sut.AllFlags["Billing"]);
    }

    [Fact]
    public void Load_OverwritesPreviousFlags()
    {
        var sut = CreateSut();
        sut.Load(new Dictionary<string, bool> { [FeatureFlags.Products] = false });

        Assert.False(sut.IsEnabled(FeatureFlags.Products));

        sut.Load(new Dictionary<string, bool> { [FeatureFlags.Products] = true });

        Assert.True(sut.IsEnabled(FeatureFlags.Products));
    }

    // ── Billing mode: management features disabled ─────────────────

    [Theory]
    [InlineData(FeatureFlags.UserManagement)]
    [InlineData(FeatureFlags.FirmManagement)]
    [InlineData(FeatureFlags.TaxManagement)]
    [InlineData(FeatureFlags.SystemSettings)]
    public void BillingMode_DisablesManagementFeatures(string feature)
    {
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Billing);

        Assert.False(sut.IsEnabled(feature));
    }

    [Theory]
    [InlineData(FeatureFlags.Billing)]
    [InlineData(FeatureFlags.AdvancedBilling)]
    [InlineData(FeatureFlags.Products)]
    [InlineData(FeatureFlags.Sales)]
    public void BillingMode_EnablesBillingAndNeutralFeatures(string feature)
    {
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Billing);

        Assert.True(sut.IsEnabled(feature));
    }

    // ── Management mode: billing entry stays visible, advanced POS stays hidden ──

    [Fact]
    public void ManagementMode_DisablesAdvancedBillingFeature()
    {
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Management);

        Assert.False(sut.IsEnabled(FeatureFlags.AdvancedBilling));
    }

    [Fact]
    public void ManagementMode_KeepsBillingEntryEnabled()
    {
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Management);

        Assert.True(sut.IsEnabled(FeatureFlags.Billing));
    }

    [Theory]
    [InlineData(FeatureFlags.UserManagement)]
    [InlineData(FeatureFlags.FirmManagement)]
    [InlineData(FeatureFlags.TaxManagement)]
    [InlineData(FeatureFlags.SystemSettings)]
    [InlineData(FeatureFlags.Products)]
    [InlineData(FeatureFlags.Sales)]
    [InlineData(FeatureFlags.Reports)]
    public void ManagementMode_EnablesManagementAndNeutralFeatures(string feature)
    {
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Management);

        Assert.True(sut.IsEnabled(feature));
    }

    // ── Config flag overrides mode ─────────────────────────────────

    [Fact]
    public void ConfigDisabled_OverridesMode()
    {
        var sut = CreateSut();
        sut.Load(new Dictionary<string, bool> { [FeatureFlags.Products] = false });
        sut.SetMode(OperationalMode.Billing);

        Assert.False(sut.IsEnabled(FeatureFlags.Products));
    }

    // ── SetMode notifications ──────────────────────────────────────

    [Fact]
    public void SetMode_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        var raised = false;
        sut.PropertyChanged += (_, _) => raised = true;

        sut.SetMode(OperationalMode.Billing);

        Assert.True(raised);
    }

    [Fact]
    public void SetMode_SameMode_DoesNotNotify()
    {
        var sut = CreateSut();
        // Default is Management (0)
        var raised = false;
        sut.PropertyChanged += (_, _) => raised = true;

        sut.SetMode(OperationalMode.Management);

        Assert.False(raised);
    }

    [Fact]
    public void CurrentMode_DefaultsToManagement()
    {
        var sut = CreateSut();

        Assert.Equal(OperationalMode.Management, sut.CurrentMode);
    }

    [Fact]
    public void SetMode_UpdatesAppState()
    {
        var sut = CreateSut();

        sut.SetMode(OperationalMode.Billing);

        _appState.Received(1).SetMode(OperationalMode.Billing);
    }

    // ── Focus lock: non-billing features disabled ──────────────────

    [Theory]
    [InlineData(FeatureFlags.Products)]
    [InlineData(FeatureFlags.Reports)]
    [InlineData(FeatureFlags.UserManagement)]
    [InlineData(FeatureFlags.FirmManagement)]
    [InlineData(FeatureFlags.TaxManagement)]
    [InlineData(FeatureFlags.SystemSettings)]
    public void FocusLocked_DisablesNonBillingFeatures(string feature)
    {
        _focusLock.IsFocusLocked.Returns(true);
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Billing);

        Assert.False(sut.IsEnabled(feature));
    }

    [Theory]
    [InlineData(FeatureFlags.Billing)]
    [InlineData(FeatureFlags.AdvancedBilling)]
    [InlineData(FeatureFlags.Sales)]
    public void FocusLocked_KeepsBillingFeaturesEnabled(string feature)
    {
        _focusLock.IsFocusLocked.Returns(true);
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Billing);

        Assert.True(sut.IsEnabled(feature));
    }

    [Fact]
    public void FocusNotLocked_DoesNotFilter()
    {
        _focusLock.IsFocusLocked.Returns(false);
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Billing);

        // Products is a neutral feature — should be enabled in billing
        // mode when focus lock is NOT active.
        Assert.True(sut.IsEnabled(FeatureFlags.Products));
    }

    [Fact]
    public void FocusLocked_ConfigDisabled_StillDisabled()
    {
        _focusLock.IsFocusLocked.Returns(true);
        var sut = CreateSut();
        sut.Load(new Dictionary<string, bool> { [FeatureFlags.Sales] = false });
        sut.SetMode(OperationalMode.Billing);

        // Config flag takes priority even though focus lock allows Sales
        Assert.False(sut.IsEnabled(FeatureFlags.Sales));
    }

    // ── Focus lock PropertyChanged triggers re-evaluation ──────────

    [Fact]
    public void FocusLockChanged_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        var raised = false;
        sut.PropertyChanged += (_, _) => raised = true;

        // Simulate IsFocusLocked changing
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        Assert.True(raised);
    }

    // ── Offline mode: DB-dependent features disabled ───────────────

    [Theory]
    [InlineData(FeatureFlags.Products)]
    [InlineData(FeatureFlags.UserManagement)]
    [InlineData(FeatureFlags.FirmManagement)]
    [InlineData(FeatureFlags.TaxManagement)]
    [InlineData(FeatureFlags.SystemSettings)]
    [InlineData(FeatureFlags.Reports)]
    public void OfflineMode_DisablesDbDependentFeatures(string feature)
    {
        _appState.IsOfflineMode.Returns(true);
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Management);

        Assert.False(sut.IsEnabled(feature));
    }

    [Theory]
    [InlineData(FeatureFlags.Sales)]
    [InlineData(FeatureFlags.Billing)]
    [InlineData(FeatureFlags.AdvancedBilling)]
    public void OfflineMode_AllowsBillingAndSalesFeatures(string feature)
    {
        _appState.IsOfflineMode.Returns(true);
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Billing);

        Assert.True(sut.IsEnabled(feature));
    }

    [Fact]
    public void OfflineMode_ConfigDisabled_StillDisabled()
    {
        _appState.IsOfflineMode.Returns(true);
        var sut = CreateSut();
        sut.Load(new Dictionary<string, bool> { [FeatureFlags.Sales] = false });

        Assert.False(sut.IsEnabled(FeatureFlags.Sales));
    }

    [Theory]
    [InlineData(FeatureFlags.Products)]
    [InlineData(FeatureFlags.UserManagement)]
    [InlineData(FeatureFlags.FirmManagement)]
    [InlineData(FeatureFlags.TaxManagement)]
    [InlineData(FeatureFlags.SystemSettings)]
    [InlineData(FeatureFlags.Reports)]
    public void OnlineMode_DoesNotRestrictFeatures(string feature)
    {
        _appState.IsOfflineMode.Returns(false);
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Management);

        Assert.True(sut.IsEnabled(feature));
    }

    [Fact]
    public void OfflineModeChanged_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        var raised = false;
        sut.PropertyChanged += (_, _) => raised = true;

        _appState.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _appState,
            new PropertyChangedEventArgs(nameof(IAppStateService.IsOfflineMode)));

        Assert.True(raised);
    }

    [Fact]
    public void OfflineModeChanged_OtherProperty_DoesNotRaise()
    {
        var sut = CreateSut();
        var raised = false;
        sut.PropertyChanged += (_, _) => raised = true;

        _appState.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _appState,
            new PropertyChangedEventArgs(nameof(IAppStateService.CurrentTime)));

        Assert.False(raised);
    }

    [Fact]
    public void OfflineMode_FocusLockCombined_StillDisablesProducts()
    {
        _appState.IsOfflineMode.Returns(true);
        _focusLock.IsFocusLocked.Returns(true);
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Billing);

        // Products blocked by both focus lock AND offline mode
        Assert.False(sut.IsEnabled(FeatureFlags.Products));
        // Sales allowed by both focus lock AND offline mode
        Assert.True(sut.IsEnabled(FeatureFlags.Sales));
    }

    [Fact]
    public void OfflineMode_BillingMode_SalesStillAllowed()
    {
        _appState.IsOfflineMode.Returns(true);
        var sut = CreateSut();
        sut.SetMode(OperationalMode.Billing);

        Assert.True(sut.IsEnabled(FeatureFlags.Sales));
        Assert.True(sut.IsEnabled(FeatureFlags.Billing));
    }

    [Fact]
    public void CurrentModeChanged_FromAppState_SyncsServiceMode_AndRaisesPropertyChanged()
    {
        var sut = CreateSut();
        var raised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FeatureToggleService.CurrentMode) || string.IsNullOrEmpty(e.PropertyName))
                raised = true;
        };

        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _appState,
            new PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));

        Assert.Equal(OperationalMode.Billing, sut.CurrentMode);
        Assert.True(raised);
    }

    [Fact]
    public void Dispose_UnsubscribesFromPropertySources()
    {
        var sut = CreateSut();
        var raised = false;
        sut.PropertyChanged += (_, _) => raised = true;

        sut.Dispose();

        _focusLock.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _focusLock,
            new PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));
        _appState.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _appState,
            new PropertyChangedEventArgs(nameof(IAppStateService.IsOfflineMode)));

        Assert.False(raised);
    }

    [Fact]
    public void Dispose_CanBeCalledTwice()
    {
        var sut = CreateSut();

        sut.Dispose();
        sut.Dispose();

        _focusLock.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _focusLock,
            new PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));
    }
}
