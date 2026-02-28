using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

/// <summary>
/// Tests for <see cref="FocusRuleEngine"/> — the context-aware rule
/// engine that evaluates <see cref="FocusContext"/> against the
/// <see cref="IFocusMapRegistry"/> to produce <see cref="FocusHint"/>.
/// </summary>
public class FocusRuleEngineTests
{
    private readonly FocusMapRegistry _registry = new();

    private FocusRuleEngine CreateSut() => new(_registry);

    // ══════════════════════════════════════════════════════════════════
    //  Rule 1: Billing + Page → always BillingSearchBox
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void BillingPage_AlwaysReturnsBillingSearchBox()
    {
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Billing, "Billing", FocusContextType.Page);

        var hint = sut.Evaluate(ctx, "test");

        Assert.Equal(FocusStrategy.Named, hint.Strategy);
        Assert.Equal("BillingSearchBox", hint.ElementName);
    }

    [Fact]
    public void BillingPage_IgnoresFocusMap()
    {
        // Even if a FocusMap is registered with different entries,
        // billing page always lands on BillingSearchBox.
        _registry.Register(FocusMap.For("Billing")
            .Add("SomeOtherInput", FocusRole.PrimaryInput)
            .Build());
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Billing, "Billing", FocusContextType.Page);

        var hint = sut.Evaluate(ctx, "test");

        Assert.Equal("BillingSearchBox", hint.ElementName);
    }

    [Fact]
    public void BillingPage_HasHighPriority()
    {
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Billing, "Billing", FocusContextType.Page);

        var hint = sut.Evaluate(ctx, "test", basePriority: 5);

        Assert.Equal(20, hint.Priority); // basePriority + 15
    }

    // ══════════════════════════════════════════════════════════════════
    //  Rule 2: Dialog → PrimaryInput from map
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Dialog_WithMap_ReturnsPrimaryInput()
    {
        _registry.Register(FocusMap.For("FirmManagement")
            .Add("FirmNameInput", FocusRole.PrimaryInput)
            .Add("SaveButton", FocusRole.PrimaryAction)
            .Build());
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "FirmManagement", FocusContextType.Dialog);

        var hint = sut.Evaluate(ctx, "DialogOpened");

        Assert.Equal(FocusStrategy.Named, hint.Strategy);
        Assert.Equal("FirmNameInput", hint.ElementName);
    }

    [Fact]
    public void Dialog_WithMap_NoPrimaryInput_FallsToLandingTarget()
    {
        _registry.Register(FocusMap.For("ConfirmDialog")
            .Add("OkButton", FocusRole.PrimaryAction)
            .Build());
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "ConfirmDialog", FocusContextType.Dialog);

        var hint = sut.Evaluate(ctx, "DialogOpened");

        Assert.Equal(FocusStrategy.Named, hint.Strategy);
        Assert.Equal("OkButton", hint.ElementName);
    }

    [Fact]
    public void Dialog_NoMap_ReturnsFirstInput()
    {
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "UnregisteredDialog", FocusContextType.Dialog);

        var hint = sut.Evaluate(ctx, "DialogOpened");

        Assert.Equal(FocusStrategy.FirstInput, hint.Strategy);
    }

    [Fact]
    public void Dialog_InBillingMode_StillUsesPrimaryInput()
    {
        // Dialogs override the billing rule — they land on PrimaryInput
        // regardless of operational mode.
        _registry.Register(FocusMap.For("PaymentDialog")
            .Add("AmountInput", FocusRole.PrimaryInput)
            .Build());
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Billing, "PaymentDialog", FocusContextType.Dialog);

        var hint = sut.Evaluate(ctx, "DialogOpened");

        Assert.Equal(FocusStrategy.Named, hint.Strategy);
        Assert.Equal("AmountInput", hint.ElementName);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Rule 3: Form → PrimaryInput from map
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Form_WithMap_ReturnsPrimaryInput()
    {
        _registry.Register(FocusMap.For("VendorsAddForm")
            .Add("NameInput", FocusRole.PrimaryInput)
            .Add("ContactInput", FocusRole.FormField)
            .Build());
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "VendorsAddForm", FocusContextType.Form);

        var hint = sut.Evaluate(ctx, "FormRevealed");

        Assert.Equal(FocusStrategy.Named, hint.Strategy);
        Assert.Equal("NameInput", hint.ElementName);
    }

    [Fact]
    public void Form_NoMap_ReturnsFirstInput()
    {
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "UnregisteredForm", FocusContextType.Form);

        var hint = sut.Evaluate(ctx, "FormRevealed");

        Assert.Equal(FocusStrategy.FirstInput, hint.Strategy);
    }

    [Fact]
    public void Form_HasElevatedPriority()
    {
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "SomeForm", FocusContextType.Form);

        var hint = sut.Evaluate(ctx, "FormRevealed", basePriority: 5);

        Assert.Equal(15, hint.Priority); // basePriority + 10
    }

    // ══════════════════════════════════════════════════════════════════
    //  Rule 4: Management + Page → SearchInput preferred
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ManagementPage_WithSearchInput_ReturnsSearchInput()
    {
        _registry.Register(FocusMap.For("Vendors")
            .Add("VendorSearchBox", FocusRole.SearchInput)
            .Add("NameInput", FocusRole.PrimaryInput)
            .Build());
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "Vendors", FocusContextType.Page);

        var hint = sut.Evaluate(ctx, "PageNavigated");

        Assert.Equal(FocusStrategy.Named, hint.Strategy);
        Assert.Equal("VendorSearchBox", hint.ElementName);
    }

    [Fact]
    public void ManagementPage_NoSearchInput_FallsToPrimaryInput()
    {
        _registry.Register(FocusMap.For("Settings")
            .Add("ThemeSelector", FocusRole.PrimaryInput)
            .Add("SaveButton", FocusRole.PrimaryAction)
            .Build());
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "Settings", FocusContextType.Page);

        var hint = sut.Evaluate(ctx, "PageNavigated");

        Assert.Equal(FocusStrategy.Named, hint.Strategy);
        Assert.Equal("ThemeSelector", hint.ElementName);
    }

    [Fact]
    public void ManagementPage_NoSearchOrPrimary_FallsToLandingTarget()
    {
        _registry.Register(FocusMap.For("Dashboard")
            .Add("SummaryGrid", FocusRole.DataGrid)
            .Build());
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "Dashboard", FocusContextType.Page);

        var hint = sut.Evaluate(ctx, "PageNavigated");

        Assert.Equal(FocusStrategy.Named, hint.Strategy);
        Assert.Equal("SummaryGrid", hint.ElementName);
    }

    [Fact]
    public void ManagementPage_NoMap_ReturnsFirstInput()
    {
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "UnregisteredPage", FocusContextType.Page);

        var hint = sut.Evaluate(ctx, "PageNavigated");

        Assert.Equal(FocusStrategy.FirstInput, hint.Strategy);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Rule 5: Empty/null context key → FirstInput (fallback)
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void EmptyContextKey_ReturnsFirstInput()
    {
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "", FocusContextType.Page);

        var hint = sut.Evaluate(ctx, "NoContext");

        Assert.Equal(FocusStrategy.FirstInput, hint.Strategy);
    }

    [Fact]
    public void EmptyContextKey_BillingMode_StillReturnsBillingSearchBox()
    {
        // Billing + Page always returns BillingSearchBox, even without a key
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Billing, "", FocusContextType.Page);

        var hint = sut.Evaluate(ctx, "NoContext");

        Assert.Equal(FocusStrategy.Named, hint.Strategy);
        Assert.Equal("BillingSearchBox", hint.ElementName);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Reason and priority propagation
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Reason_PropagatedToHint()
    {
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "", FocusContextType.Page);

        var hint = sut.Evaluate(ctx, "MyCustomReason");

        Assert.Equal("MyCustomReason", hint.Reason);
    }

    [Fact]
    public void BasePriority_PropagatedForManagementPage()
    {
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "", FocusContextType.Page);

        var hint = sut.Evaluate(ctx, "test", basePriority: 7);

        Assert.Equal(7, hint.Priority);
    }

    [Fact]
    public void BasePriority_ElevatedForDialog()
    {
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Management, "SomeDialog", FocusContextType.Dialog);

        var hint = sut.Evaluate(ctx, "test", basePriority: 5);

        Assert.Equal(15, hint.Priority); // basePriority + 10
    }

    [Fact]
    public void BasePriority_MaximumForBillingPage()
    {
        var sut = CreateSut();
        var ctx = new FocusContext(OperationalMode.Billing, "Billing", FocusContextType.Page);

        var hint = sut.Evaluate(ctx, "test", basePriority: 5);

        Assert.Equal(20, hint.Priority); // basePriority + 15
    }

    // ══════════════════════════════════════════════════════════════════
    //  Null context guard
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void NullContext_Throws()
    {
        var sut = CreateSut();

        Assert.Throws<ArgumentNullException>(() => sut.Evaluate(null!, "test"));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Real-world Vendors page scenario
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void VendorsPage_ManagementMode_LandsOnSearchBox()
    {
        _registry.Register(FocusMap.For("Vendors")
            .Add("VendorSearchBox", FocusRole.SearchInput)
            .Add("VendorDataGrid", FocusRole.DataGrid)
            .Add("NameInput", FocusRole.PrimaryInput, isAvailableWhen: "IsAddFormVisible")
            .Add("SaveButton", FocusRole.PrimaryAction, isAvailableWhen: "IsAddFormVisible")
            .Build());
        var sut = CreateSut();

        var hint = sut.Evaluate(
            new FocusContext(OperationalMode.Management, "Vendors", FocusContextType.Page),
            "PageNavigated");

        Assert.Equal(FocusStrategy.Named, hint.Strategy);
        Assert.Equal("VendorSearchBox", hint.ElementName);
    }

    [Fact]
    public void VendorsPage_BillingMode_LandsOnBillingSearchBox()
    {
        // Even if navigated to Vendors in billing mode (unlikely but safe)
        _registry.Register(FocusMap.For("Vendors")
            .Add("VendorSearchBox", FocusRole.SearchInput)
            .Build());
        var sut = CreateSut();

        var hint = sut.Evaluate(
            new FocusContext(OperationalMode.Billing, "Vendors", FocusContextType.Page),
            "PageNavigated");

        // Billing mode always overrides to BillingSearchBox
        Assert.Equal("BillingSearchBox", hint.ElementName);
    }
}
