using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

public class FocusMapTests
{
    // ══════════════════════════════════════════════════════════════════
    //  FocusMap model tests
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Build_CreatesImmutableMap()
    {
        var map = FocusMap.For("TestPage")
            .Add("Field1", FocusRole.PrimaryInput)
            .Add("Field2", FocusRole.FormField)
            .Build();

        Assert.Equal("TestPage", map.ContextKey);
        Assert.Equal(2, map.Entries.Count);
    }

    [Fact]
    public void Build_EmptyMap_IsValid()
    {
        var map = FocusMap.For("Empty").Build();

        Assert.Equal("Empty", map.ContextKey);
        Assert.Empty(map.Entries);
    }

    [Fact]
    public void Build_NullContextKey_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FocusMap.For(null!));
    }

    [Fact]
    public void Build_EmptyContextKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => FocusMap.For(""));
    }

    [Fact]
    public void Add_DuplicateElementName_Throws()
    {
        var builder = FocusMap.For("Test")
            .Add("Field1", FocusRole.PrimaryInput);

        Assert.Throws<InvalidOperationException>(
            () => builder.Add("Field1", FocusRole.FormField));
    }

    [Fact]
    public void Add_EmptyElementName_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => FocusMap.For("Test").Add("", FocusRole.PrimaryInput));
    }

    // ── GetLandingTarget ───────────────────────────────────────────

    [Fact]
    public void GetLandingTarget_ReturnsFirstEntry()
    {
        var map = FocusMap.For("Test")
            .Add("SearchBox", FocusRole.SearchInput)
            .Add("NameInput", FocusRole.PrimaryInput)
            .Build();

        Assert.Equal("SearchBox", map.GetLandingTarget());
    }

    [Fact]
    public void GetLandingTarget_EmptyMap_ReturnsNull()
    {
        var map = FocusMap.For("Empty").Build();

        Assert.Null(map.GetLandingTarget());
    }

    // ── GetByRole ──────────────────────────────────────────────────

    [Fact]
    public void GetByRole_ReturnsFirstMatch()
    {
        var map = FocusMap.For("Test")
            .Add("Search", FocusRole.SearchInput)
            .Add("Name", FocusRole.PrimaryInput)
            .Add("Save", FocusRole.PrimaryAction)
            .Build();

        var entry = map.GetByRole(FocusRole.PrimaryAction);

        Assert.NotNull(entry);
        Assert.Equal("Save", entry!.ElementName);
    }

    [Fact]
    public void GetByRole_NoMatch_ReturnsNull()
    {
        var map = FocusMap.For("Test")
            .Add("Name", FocusRole.PrimaryInput)
            .Build();

        Assert.Null(map.GetByRole(FocusRole.DataGrid));
    }

    // ── GetAllByRole ───────────────────────────────────────────────

    [Fact]
    public void GetAllByRole_ReturnsAllMatches()
    {
        var map = FocusMap.For("Test")
            .Add("Name", FocusRole.FormField)
            .Add("Email", FocusRole.FormField)
            .Add("Save", FocusRole.PrimaryAction)
            .Add("Phone", FocusRole.FormField)
            .Build();

        var fields = map.GetAllByRole(FocusRole.FormField).ToList();

        Assert.Equal(3, fields.Count);
        Assert.Equal("Name", fields[0].ElementName);
        Assert.Equal("Email", fields[1].ElementName);
        Assert.Equal("Phone", fields[2].ElementName);
    }

    // ── GetNextElement ─────────────────────────────────────────────

    [Fact]
    public void GetNextElement_ReturnsNextInOrder()
    {
        var map = FocusMap.For("Test")
            .Add("A", FocusRole.PrimaryInput)
            .Add("B", FocusRole.FormField)
            .Add("C", FocusRole.PrimaryAction)
            .Build();

        Assert.Equal("B", map.GetNextElement("A"));
        Assert.Equal("C", map.GetNextElement("B"));
    }

    [Fact]
    public void GetNextElement_LastEntry_ReturnsNull()
    {
        var map = FocusMap.For("Test")
            .Add("A", FocusRole.PrimaryInput)
            .Add("B", FocusRole.FormField)
            .Build();

        Assert.Null(map.GetNextElement("B"));
    }

    [Fact]
    public void GetNextElement_UnknownElement_ReturnsNull()
    {
        var map = FocusMap.For("Test")
            .Add("A", FocusRole.PrimaryInput)
            .Build();

        Assert.Null(map.GetNextElement("Z"));
    }

    // ── GetPreviousElement ─────────────────────────────────────────

    [Fact]
    public void GetPreviousElement_ReturnsPreviousInOrder()
    {
        var map = FocusMap.For("Test")
            .Add("A", FocusRole.PrimaryInput)
            .Add("B", FocusRole.FormField)
            .Add("C", FocusRole.PrimaryAction)
            .Build();

        Assert.Equal("B", map.GetPreviousElement("C"));
        Assert.Equal("A", map.GetPreviousElement("B"));
    }

    [Fact]
    public void GetPreviousElement_FirstEntry_ReturnsNull()
    {
        var map = FocusMap.For("Test")
            .Add("A", FocusRole.PrimaryInput)
            .Add("B", FocusRole.FormField)
            .Build();

        Assert.Null(map.GetPreviousElement("A"));
    }

    // ── Contains ───────────────────────────────────────────────────

    [Fact]
    public void Contains_ExistingElement_ReturnsTrue()
    {
        var map = FocusMap.For("Test")
            .Add("Name", FocusRole.PrimaryInput)
            .Build();

        Assert.True(map.Contains("Name"));
    }

    [Fact]
    public void Contains_MissingElement_ReturnsFalse()
    {
        var map = FocusMap.For("Test")
            .Add("Name", FocusRole.PrimaryInput)
            .Build();

        Assert.False(map.Contains("NotHere"));
    }

    // ── IsAvailableWhen guard ──────────────────────────────────────

    [Fact]
    public void Entry_WithGuard_StoresPropertyName()
    {
        var map = FocusMap.For("Test")
            .Add("Name", FocusRole.PrimaryInput, isAvailableWhen: "IsFormVisible")
            .Build();

        Assert.Equal("IsFormVisible", map.Entries[0].IsAvailableWhen);
    }

    [Fact]
    public void Entry_WithoutGuard_HasNullCondition()
    {
        var map = FocusMap.For("Test")
            .Add("Name", FocusRole.PrimaryInput)
            .Build();

        Assert.Null(map.Entries[0].IsAvailableWhen);
    }

    // ── Order preservation ─────────────────────────────────────────

    [Fact]
    public void Entries_PreserveInsertionOrder()
    {
        var map = FocusMap.For("Test")
            .Add("Search", FocusRole.SearchInput)
            .Add("Grid", FocusRole.DataGrid)
            .Add("Name", FocusRole.PrimaryInput)
            .Add("Save", FocusRole.PrimaryAction)
            .Add("Cancel", FocusRole.SecondaryAction)
            .Build();

        Assert.Equal(
            ["Search", "Grid", "Name", "Save", "Cancel"],
            map.Entries.Select(e => e.ElementName).ToArray());
    }

    // ══════════════════════════════════════════════════════════════════
    //  FocusMapRegistry tests
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Registry_RegisterAndGet()
    {
        var registry = new FocusMapRegistry();
        var map = FocusMap.For("Vendors")
            .Add("Name", FocusRole.PrimaryInput)
            .Build();

        registry.Register(map);

        Assert.Same(map, registry.Get("Vendors"));
    }

    [Fact]
    public void Registry_Get_UnknownKey_ReturnsNull()
    {
        var registry = new FocusMapRegistry();

        Assert.Null(registry.Get("NonExistent"));
    }

    [Fact]
    public void Registry_Contains_RegisteredKey_ReturnsTrue()
    {
        var registry = new FocusMapRegistry();
        registry.Register(FocusMap.For("Products").Build());

        Assert.True(registry.Contains("Products"));
    }

    [Fact]
    public void Registry_Contains_UnknownKey_ReturnsFalse()
    {
        var registry = new FocusMapRegistry();

        Assert.False(registry.Contains("Missing"));
    }

    [Fact]
    public void Registry_Register_ReplaceExisting()
    {
        var registry = new FocusMapRegistry();
        var map1 = FocusMap.For("Test").Add("A", FocusRole.PrimaryInput).Build();
        var map2 = FocusMap.For("Test").Add("B", FocusRole.PrimaryInput).Build();

        registry.Register(map1);
        registry.Register(map2);

        Assert.Same(map2, registry.Get("Test"));
        Assert.Equal("B", registry.Get("Test")!.GetLandingTarget());
    }

    [Fact]
    public void Registry_GetRegisteredKeys_ReturnsAllKeys()
    {
        var registry = new FocusMapRegistry();
        registry.Register(FocusMap.For("A").Build());
        registry.Register(FocusMap.For("B").Build());
        registry.Register(FocusMap.For("C").Build());

        var keys = registry.GetRegisteredKeys();

        Assert.Equal(3, keys.Count);
        Assert.Contains("A", keys);
        Assert.Contains("B", keys);
        Assert.Contains("C", keys);
    }

    [Fact]
    public void Registry_Register_NullMap_Throws()
    {
        var registry = new FocusMapRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Fact]
    public void Registry_Get_EmptyKey_Throws()
    {
        var registry = new FocusMapRegistry();

        Assert.Throws<ArgumentException>(() => registry.Get(""));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Vendors focus map integration test
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void VendorsFocusMap_HasExpectedStructure()
    {
        // Simulate what VendorsModule.AddVendorsModule registers
        var map = FocusMap.For("Vendors")
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

        // Landing target is the search box (page navigation)
        Assert.Equal("VendorSearchBox", map.GetLandingTarget());

        // Primary input is NameInput (form opened)
        Assert.Equal("NameInput", map.GetByRole(FocusRole.PrimaryInput)!.ElementName);

        // Save button is reachable
        Assert.Equal("SaveButton", map.GetByRole(FocusRole.PrimaryAction)!.ElementName);

        // Sequential traversal from NameInput
        Assert.Equal("ContactInput", map.GetNextElement("NameInput"));
        Assert.Equal("PhoneInput", map.GetNextElement("ContactInput"));
        Assert.Equal("EmailInput", map.GetNextElement("PhoneInput"));
        Assert.Equal("GSTINInput", map.GetNextElement("EmailInput"));
        Assert.Equal("SaveButton", map.GetNextElement("GSTINInput"));
        Assert.Equal("CancelButton", map.GetNextElement("SaveButton"));
        Assert.Null(map.GetNextElement("CancelButton"));

        // Form fields have availability guard
        var formEntries = map.Entries.Where(e => e.IsAvailableWhen == "IsAddFormVisible").ToList();
        Assert.Equal(7, formEntries.Count);

        // Grid and search are always available
        Assert.Null(map.Entries[0].IsAvailableWhen);
        Assert.Null(map.Entries[1].IsAvailableWhen);
    }
}
