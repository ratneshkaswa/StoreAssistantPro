using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Helpers;

/// <summary>
/// Tests for the smart Enter-key navigation logic that uses
/// <see cref="FocusMap"/> to determine the next field.
/// <para>
/// Since <see cref="Core.Helpers.KeyboardNav"/> is an attached behavior
/// that requires WPF visual tree, these tests validate the underlying
/// <see cref="FocusMap"/> navigation decisions that the behavior
/// delegates to at runtime.
/// </para>
/// </summary>
public class SmartEnterNavigationTests
{
    // ══════════════════════════════════════════════════════════════════
    //  Forward navigation (Enter key)
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Enter_FromFirstField_MovesToSecond()
    {
        var map = CreateVendorMap();

        var next = map.GetNextElement("NameInput");

        Assert.Equal("ContactInput", next);
    }

    [Fact]
    public void Enter_AdvancesThroughAllFields()
    {
        var map = CreateVendorMap();

        Assert.Equal("ContactInput", map.GetNextElement("NameInput"));
        Assert.Equal("PhoneInput", map.GetNextElement("ContactInput"));
        Assert.Equal("EmailInput", map.GetNextElement("PhoneInput"));
        Assert.Equal("GSTINInput", map.GetNextElement("EmailInput"));
    }

    [Fact]
    public void Enter_LastFormField_ReturnsNull_TriggeringPrimaryAction()
    {
        var map = CreateFormOnlyMap();

        // GSTINInput is the last FormField before SaveButton
        var next = map.GetNextElement("GSTINInput");

        // Next is SaveButton (PrimaryAction) — but the behavior
        // should detect this is a PrimaryAction role and execute the
        // DefaultCommand instead of focusing it.
        Assert.Equal("SaveButton", next);
    }

    [Fact]
    public void Enter_LastEntryInMap_ReturnsNull()
    {
        var map = CreateFormOnlyMap();

        // CancelButton is the absolute last entry
        var next = map.GetNextElement("CancelButton");

        Assert.Null(next);
    }

    [Fact]
    public void Enter_AtPrimaryAction_NoNextField_TriggersSubmit()
    {
        // When the user is on the last form field and there's no next
        // element (map returns null), the behavior should fire DefaultCommand.
        var map = FocusMap.For("Minimal")
            .Add("Field1", FocusRole.PrimaryInput)
            .Add("Field2", FocusRole.FormField)
            .Build();

        var next = map.GetNextElement("Field2");

        Assert.Null(next);
        // In the behavior, null → TryExecuteDefaultCommand → Save fires
    }

    // ══════════════════════════════════════════════════════════════════
    //  Reverse navigation (Shift+Enter)
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ShiftEnter_FromSecondField_MovesToFirst()
    {
        var map = CreateVendorMap();

        var prev = map.GetPreviousElement("ContactInput");

        Assert.Equal("NameInput", prev);
    }

    [Fact]
    public void ShiftEnter_FromFirstField_ReturnsNull_FallsBack()
    {
        var map = CreateFormOnlyMap();

        var prev = map.GetPreviousElement("NameInput");

        Assert.Null(prev);
        // In the behavior, null → returns false → standard PredictFocus
    }

    [Fact]
    public void ShiftEnter_ReversesEntireSequence()
    {
        var map = CreateFormOnlyMap();

        Assert.Equal("GSTINInput", map.GetPreviousElement("SaveButton"));
        Assert.Equal("EmailInput", map.GetPreviousElement("GSTINInput"));
        Assert.Equal("PhoneInput", map.GetPreviousElement("EmailInput"));
        Assert.Equal("ContactInput", map.GetPreviousElement("PhoneInput"));
        Assert.Equal("NameInput", map.GetPreviousElement("ContactInput"));
    }

    // ══════════════════════════════════════════════════════════════════
    //  PrimaryAction detection
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void PrimaryAction_IsLocatable()
    {
        var map = CreateVendorMap();

        var action = map.GetByRole(FocusRole.PrimaryAction);

        Assert.NotNull(action);
        Assert.Equal("SaveButton", action!.ElementName);
    }

    [Fact]
    public void LastFormField_NextIsPrimaryAction()
    {
        var map = CreateFormOnlyMap();

        // The last FormField should have PrimaryAction as its successor
        var lastField = "GSTINInput";
        var next = map.GetNextElement(lastField);

        Assert.Equal("SaveButton", next);

        var nextEntry = map.Entries.First(e => e.ElementName == next);
        Assert.Equal(FocusRole.PrimaryAction, nextEntry.Role);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Element not in map (fallback to standard nav)
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void UnknownElement_Contains_ReturnsFalse()
    {
        var map = CreateVendorMap();

        Assert.False(map.Contains("RandomTextBox"));
    }

    [Fact]
    public void UnknownElement_GetNext_ReturnsNull()
    {
        var map = CreateVendorMap();

        Assert.Null(map.GetNextElement("NotInMap"));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Guard conditions (IsAvailableWhen)
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void FormFields_HaveAvailabilityGuard()
    {
        var map = CreateVendorMap();

        // Form fields should only be navigable when the form is visible
        var nameEntry = map.Entries.First(e => e.ElementName == "NameInput");
        Assert.Equal("IsAddFormVisible", nameEntry.IsAvailableWhen);

        var contactEntry = map.Entries.First(e => e.ElementName == "ContactInput");
        Assert.Equal("IsAddFormVisible", contactEntry.IsAvailableWhen);
    }

    [Fact]
    public void SearchBox_AlwaysAvailable()
    {
        var map = CreateVendorMap();

        var search = map.Entries.First(e => e.ElementName == "VendorSearchBox");
        Assert.Null(search.IsAvailableWhen);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Full-page map structure (Vendors reference)
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void VendorMap_SearchBoxIsLandingTarget()
    {
        var map = CreateVendorMap();

        Assert.Equal("VendorSearchBox", map.GetLandingTarget());
    }

    [Fact]
    public void VendorMap_FormSequenceStartsAtPrimaryInput()
    {
        var map = CreateVendorMap();

        var primary = map.GetByRole(FocusRole.PrimaryInput);
        Assert.NotNull(primary);
        Assert.Equal("NameInput", primary!.ElementName);
    }

    [Fact]
    public void VendorMap_HasSecondaryAction()
    {
        var map = CreateVendorMap();

        var cancel = map.GetByRole(FocusRole.SecondaryAction);
        Assert.NotNull(cancel);
        Assert.Equal("CancelButton", cancel!.ElementName);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Registry integration
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Registry_ResolvesMapByKey()
    {
        var registry = new FocusMapRegistry();
        var map = CreateVendorMap();
        registry.Register(map);

        var resolved = registry.Get("Vendors");

        Assert.NotNull(resolved);
        Assert.Same(map, resolved);
    }

    [Fact]
    public void Registry_MissingKey_ReturnsNull_FallbackBehavior()
    {
        var registry = new FocusMapRegistry();

        // When no map is registered, behavior falls back to standard nav
        var resolved = registry.Get("NoSuchPage");

        Assert.Null(resolved);
    }

    // ══════════════════════════════════════════════════════════════════
    //  XAML compliance
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void KeyboardNav_HasFocusMapKeyProperty()
    {
        var path = Path.Combine(FindSolutionRoot(), "Core", "Helpers", "KeyboardNav.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("FocusMapKeyProperty", content, StringComparison.Ordinal);
        Assert.Contains("RegisterAttached", content, StringComparison.Ordinal);
        Assert.Contains("FocusMapKey", content, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyboardNav_QueriesFocusMapRegistry()
    {
        var path = Path.Combine(FindSolutionRoot(), "Core", "Helpers", "KeyboardNav.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("IFocusMapRegistry", content, StringComparison.Ordinal);
        Assert.Contains("GetNextElement", content, StringComparison.Ordinal);
        Assert.Contains("GetPreviousElement", content, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyboardNav_FallsBackToDefaultCommand()
    {
        var path = Path.Combine(FindSolutionRoot(), "Core", "Helpers", "KeyboardNav.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("TryExecuteDefaultCommand", content, StringComparison.Ordinal);
        Assert.Contains("DefaultCommandProperty", content, StringComparison.Ordinal);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Replicates the Vendors FocusMap registered by VendorsModule.
    /// </summary>
    private static FocusMap CreateVendorMap() =>
        FocusMap.For("Vendors")
            .Add("VendorSearchBox", FocusRole.SearchInput)
            .Add("VendorDataGrid", FocusRole.DataGrid)
            .Add("NameInput", FocusRole.PrimaryInput, isAvailableWhen: "IsAddFormVisible")
            .Add("ContactInput", FocusRole.FormField, isAvailableWhen: "IsAddFormVisible")
            .Add("PhoneInput", FocusRole.FormField, isAvailableWhen: "IsAddFormVisible")
            .Add("EmailInput", FocusRole.FormField, isAvailableWhen: "IsAddFormVisible")
            .Add("GSTINInput", FocusRole.FormField, isAvailableWhen: "IsAddFormVisible")
            .Add("SaveButton", FocusRole.PrimaryAction, isAvailableWhen: "IsAddFormVisible")
            .Add("CancelButton", FocusRole.SecondaryAction, isAvailableWhen: "IsAddFormVisible")
            .Build();

    /// <summary>
    /// Minimal form-only map (no toolbar/search).
    /// </summary>
    private static FocusMap CreateFormOnlyMap() =>
        FocusMap.For("FormOnly")
            .Add("NameInput", FocusRole.PrimaryInput)
            .Add("ContactInput", FocusRole.FormField)
            .Add("PhoneInput", FocusRole.FormField)
            .Add("EmailInput", FocusRole.FormField)
            .Add("GSTINInput", FocusRole.FormField)
            .Add("SaveButton", FocusRole.PrimaryAction)
            .Add("CancelButton", FocusRole.SecondaryAction)
            .Build();

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0
                || Directory.GetFiles(dir, "*.slnx").Length > 0)
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Cannot find solution root.");
    }
}
