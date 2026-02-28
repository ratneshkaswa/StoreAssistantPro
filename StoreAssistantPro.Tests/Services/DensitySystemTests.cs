using System.Xml.Linq;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

/// <summary>
/// Tests for the density system: <see cref="DensityMode"/>,
/// <see cref="DensityChangedEvent"/>, compact token coverage,
/// and the ViewModel toggle wiring.
/// </summary>
public class DensitySystemTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

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

    // ══════════════════════════════════════════════════════════════════
    //  DensityMode enum
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void DensityMode_HasNormalAndCompact()
    {
        var values = Enum.GetValues<DensityMode>();

        Assert.Contains(DensityMode.Normal, values);
        Assert.Contains(DensityMode.Compact, values);
        Assert.Equal(2, values.Length);
    }

    // ══════════════════════════════════════════════════════════════════
    //  DensityChangedEvent
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void DensityChangedEvent_CarriesNewMode()
    {
        var evt = new DensityChangedEvent(DensityMode.Compact);

        Assert.Equal(DensityMode.Compact, evt.NewMode);
    }

    [Fact]
    public void DensityChangedEvent_ImplementsIEvent()
    {
        var evt = new DensityChangedEvent(DensityMode.Normal);

        Assert.IsAssignableFrom<IEvent>(evt);
    }

    [Fact]
    public void DensityChangedEvent_RecordEquality()
    {
        var a = new DensityChangedEvent(DensityMode.Compact);
        var b = new DensityChangedEvent(DensityMode.Compact);

        Assert.Equal(a, b);
    }

    // ══════════════════════════════════════════════════════════════════
    //  DensityCompact.xaml — token coverage
    //
    //  Every key in DensityCompact.xaml must exist in DesignSystem.xaml.
    //  This prevents typos that would silently add a new token instead
    //  of overriding the intended one.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void CompactDictionary_AllKeysExistInDesignSystem()
    {
        var compactPath = Path.Combine(SolutionRoot, "Core", "Styles", "DensityCompact.xaml");
        var designPath = Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml");

        Assert.True(File.Exists(compactPath), "DensityCompact.xaml not found");
        Assert.True(File.Exists(designPath), "DesignSystem.xaml not found");

        var compactKeys = ExtractXamlKeys(compactPath);
        var designKeys = ExtractXamlKeys(designPath);

        var missing = compactKeys.Except(designKeys).ToList();

        Assert.True(missing.Count == 0,
            $"DensityCompact.xaml defines keys not in DesignSystem.xaml: {string.Join(", ", missing)}. " +
            "This means the compact override won't actually replace the normal value.");
    }

    [Fact]
    public void CompactDictionary_HasMinimumTokenCount()
    {
        var compactPath = Path.Combine(SolutionRoot, "Core", "Styles", "DensityCompact.xaml");
        var keys = ExtractXamlKeys(compactPath);

        // At least 35 tokens overridden (currently 38)
        Assert.True(keys.Count >= 35,
            $"DensityCompact.xaml has only {keys.Count} tokens — expected at least 35.");
    }

    [Fact]
    public void NormalDictionary_IsEmpty()
    {
        var normalPath = Path.Combine(SolutionRoot, "Core", "Styles", "DensityNormal.xaml");
        var keys = ExtractXamlKeys(normalPath);

        Assert.Empty(keys);
    }

    // ══════════════════════════════════════════════════════════════════
    //  DensityCompact.xaml — value sanity
    //
    //  Compact control heights must be strictly smaller than normal.
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("ControlHeight")]
    [InlineData("ButtonHeight")]
    [InlineData("ButtonHeightLarge")]
    [InlineData("DataGridRowHeight")]
    public void CompactControlHeight_IsSmallerThanNormal(string key)
    {
        var compactPath = Path.Combine(SolutionRoot, "Core", "Styles", "DensityCompact.xaml");
        var designPath = Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml");

        var compactValue = ExtractDoubleValue(compactPath, key);
        var normalValue = ExtractDoubleValue(designPath, key);

        Assert.True(compactValue < normalValue,
            $"Compact {key} ({compactValue}) should be less than Normal ({normalValue})");
    }

    // ══════════════════════════════════════════════════════════════════
    //  DensityService — source validation
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void DensityService_FileExists()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Services", "DensityService.cs");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void DensityService_ReferencesCompactUri()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Services", "DensityService.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("DensityNormal.xaml", content, StringComparison.Ordinal);
        Assert.Contains("DensityCompact.xaml", content, StringComparison.Ordinal);
    }

    [Fact]
    public void DensityService_PublishesDensityChangedEvent()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Services", "DensityService.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("DensityChangedEvent", content, StringComparison.Ordinal);
        Assert.Contains("PublishAsync", content, StringComparison.Ordinal);
    }

    [Fact]
    public void DensityService_IsRegisteredAsSingleton()
    {
        var path = Path.Combine(SolutionRoot, "HostingExtensions.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("IDensityService", content, StringComparison.Ordinal);
        Assert.Contains("DensityService", content, StringComparison.Ordinal);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Shell re-navigation wiring
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void MainViewModel_SubscribesToDensityChangedEvent()
    {
        var path = Path.Combine(SolutionRoot, "Modules", "MainShell",
            "ViewModels", "MainViewModel.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("DensityChangedEvent", content, StringComparison.Ordinal);
        Assert.Contains("NavigateTo", content, StringComparison.Ordinal);
    }

    // ══════════════════════════════════════════════════════════════════
    //  GeneralSettingsViewModel — density toggle
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void GeneralSettingsViewModel_ExposesAvailableDensityModes()
    {
        var path = Path.Combine(SolutionRoot, "Modules", "SystemSettings",
            "ViewModels", "GeneralSettingsViewModel.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("AvailableDensityModes", content, StringComparison.Ordinal);
        Assert.Contains("DensityMode.Normal", content, StringComparison.Ordinal);
        Assert.Contains("DensityMode.Compact", content, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneralSettingsViewModel_CallsApplyDensityOnChange()
    {
        var path = Path.Combine(SolutionRoot, "Modules", "SystemSettings",
            "ViewModels", "GeneralSettingsViewModel.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("ApplyDensity", content, StringComparison.Ordinal);
        Assert.Contains("OnSelectedDensityModeChanged", content, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneralSettingsView_HasDensityComboBox()
    {
        var path = Path.Combine(SolutionRoot, "Modules", "SystemSettings",
            "Views", "GeneralSettingsView.xaml");
        var content = File.ReadAllText(path);

        Assert.Contains("AvailableDensityModes", content, StringComparison.Ordinal);
        Assert.Contains("SelectedDensityMode", content, StringComparison.Ordinal);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Token type consistency
    //
    //  Compact tokens must use the same XAML type as DesignSystem.
    //  e.g., Thickness→Thickness, Double→Double, GridLength→GridLength.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void CompactTokens_UseMatchingXamlTypes()
    {
        var compactPath = Path.Combine(SolutionRoot, "Core", "Styles", "DensityCompact.xaml");
        var designPath = Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml");

        var compactTypes = ExtractKeyTypeMap(compactPath);
        var designTypes = ExtractKeyTypeMap(designPath);

        var mismatches = new List<string>();
        foreach (var kvp in compactTypes)
        {
            if (designTypes.TryGetValue(kvp.Key, out var designType) &&
                designType != kvp.Value)
            {
                mismatches.Add($"{kvp.Key}: Compact={kvp.Value}, Design={designType}");
            }
        }

        Assert.True(mismatches.Count == 0,
            "Type mismatches between DensityCompact.xaml and DesignSystem.xaml:\n" +
            string.Join("\n", mismatches));
    }

    // ══════════════════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════════════════

    private static HashSet<string> ExtractXamlKeys(string path)
    {
        var doc = XDocument.Load(path);
        var ns = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");
        var keys = new HashSet<string>();

        foreach (var el in doc.Descendants())
        {
            var keyAttr = el.Attribute(ns + "Key");
            if (keyAttr is not null)
                keys.Add(keyAttr.Value);
        }

        return keys;
    }

    private static double ExtractDoubleValue(string path, string key)
    {
        var doc = XDocument.Load(path);
        var ns = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");

        foreach (var el in doc.Descendants())
        {
            var keyAttr = el.Attribute(ns + "Key");
            if (keyAttr?.Value == key && double.TryParse(el.Value.Trim(), out var v))
                return v;
        }

        throw new InvalidOperationException($"Key '{key}' not found or not a double in {path}");
    }

    private static Dictionary<string, string> ExtractKeyTypeMap(string path)
    {
        var doc = XDocument.Load(path);
        var ns = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");
        var map = new Dictionary<string, string>();

        foreach (var el in doc.Descendants())
        {
            var keyAttr = el.Attribute(ns + "Key");
            if (keyAttr is not null)
                map[keyAttr.Value] = el.Name.LocalName;
        }

        return map;
    }
}
