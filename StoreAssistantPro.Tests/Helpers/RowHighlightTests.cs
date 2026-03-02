using System.Xml.Linq;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Helpers;

/// <summary>
/// Tests for the DataGrid row highlight system:
/// <see cref="RowHighlightLevel"/> enum, DesignSystem token coverage,
/// DensityCompact compatibility, and behavior wiring.
/// </summary>
public class RowHighlightTests
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
    //  RowHighlightLevel enum
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void RowHighlightLevel_HasFiveValues()
    {
        var values = Enum.GetValues<RowHighlightLevel>();

        Assert.Equal(5, values.Length);
        Assert.Contains(RowHighlightLevel.None, values);
        Assert.Contains(RowHighlightLevel.Success, values);
        Assert.Contains(RowHighlightLevel.Warning, values);
        Assert.Contains(RowHighlightLevel.Danger, values);
        Assert.Contains(RowHighlightLevel.Inactive, values);
    }

    [Fact]
    public void RowHighlightLevel_NoneIsDefault()
    {
        // Default enum value should be None (0)
        Assert.Equal(RowHighlightLevel.None, default(RowHighlightLevel));
    }

    // ══════════════════════════════════════════════════════════════════
    //  DesignSystem.xaml — token existence
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("RowHighlightSuccess")]
    [InlineData("RowHighlightWarning")]
    [InlineData("RowHighlightDanger")]
    [InlineData("RowHighlightInactive")]
    public void DesignSystem_HasRowHighlightToken(string tokenKey)
    {
        var designPath = Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml");
        var keys = ExtractXamlKeys(designPath);

        Assert.Contains(tokenKey, keys);
    }

    [Theory]
    [InlineData("RowHighlightSuccess")]
    [InlineData("RowHighlightWarning")]
    [InlineData("RowHighlightDanger")]
    [InlineData("RowHighlightInactive")]
    public void DesignSystem_RowHighlightTokensAreBrushes(string tokenKey)
    {
        var designPath = Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml");
        var typeMap = ExtractKeyTypeMap(designPath);

        Assert.True(typeMap.ContainsKey(tokenKey), $"Token {tokenKey} not found");
        Assert.Equal("SolidColorBrush", typeMap[tokenKey]);
    }

    [Theory]
    [InlineData("RowHighlightSuccess")]
    [InlineData("RowHighlightWarning")]
    [InlineData("RowHighlightDanger")]
    [InlineData("RowHighlightInactive")]
    public void DesignSystem_RowHighlightTokensHaveReducedOpacity(string tokenKey)
    {
        var designPath = Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml");
        var opacity = ExtractBrushOpacity(designPath, tokenKey);

        // Opacity should be < 1.0 for subtle tints
        Assert.True(opacity < 1.0,
            $"{tokenKey} Opacity={opacity} — should be < 1.0 for subtle tint");
        // And > 0 (visible)
        Assert.True(opacity > 0,
            $"{tokenKey} Opacity={opacity} — should be > 0");
    }

    // ══════════════════════════════════════════════════════════════════
    //  RowHighlight behavior — source validation
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void RowHighlight_BehaviorFileExists()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "RowHighlight.cs");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RowHighlight_HasLevelProperty()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "RowHighlight.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("LevelProperty", content, StringComparison.Ordinal);
        Assert.Contains("RegisterAttached", content, StringComparison.Ordinal);
    }

    [Fact]
    public void RowHighlight_ResolvesAllTokenKeys()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "RowHighlight.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("RowHighlightSuccess", content, StringComparison.Ordinal);
        Assert.Contains("RowHighlightWarning", content, StringComparison.Ordinal);
        Assert.Contains("RowHighlightDanger", content, StringComparison.Ordinal);
        Assert.Contains("RowHighlightInactive", content, StringComparison.Ordinal);
    }

    [Fact]
    public void RowHighlight_RestoresOriginalBackground()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "RowHighlight.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("OriginalBackground", content, StringComparison.Ordinal);
    }

    [Fact]
    public void RowHighlight_TargetsDataGridRow()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "RowHighlight.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("DataGridRow", content, StringComparison.Ordinal);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Token-to-level mapping consistency
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(RowHighlightLevel.Success, "RowHighlightSuccess")]
    [InlineData(RowHighlightLevel.Warning, "RowHighlightWarning")]
    [InlineData(RowHighlightLevel.Danger, "RowHighlightDanger")]
    [InlineData(RowHighlightLevel.Inactive, "RowHighlightInactive")]
    public void GetTokenKey_MapsCorrectly(RowHighlightLevel level, string expected)
    {
        var key = RowHighlight.GetTokenKey(level);
        Assert.Equal(expected, key);
    }

    [Fact]
    public void GetTokenKey_NoneReturnsNull()
    {
        var key = RowHighlight.GetTokenKey(RowHighlightLevel.None);
        Assert.Null(key);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Every token key maps to a real DesignSystem token
    //  (catches rename drift between C# and XAML)
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void AllMappedTokenKeys_ExistInDesignSystem()
    {
        var designPath = Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml");
        var keys = ExtractXamlKeys(designPath);

        var levels = new[]
        {
            RowHighlightLevel.Success,
            RowHighlightLevel.Warning,
            RowHighlightLevel.Danger,
            RowHighlightLevel.Inactive
        };

        foreach (var level in levels)
        {
            var tokenKey = RowHighlight.GetTokenKey(level);
            Assert.NotNull(tokenKey);
            Assert.Contains(tokenKey, keys);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  Product.HighlightLevel computed property
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Product_ActiveInStock_HighlightNone()
    {
        var product = new Product
        {
            IsActive = true,
            Quantity = 50,
            MinStockLevel = 10
        };

        Assert.Equal(RowHighlightLevel.None, product.HighlightLevel);
    }

    [Fact]
    public void Product_ActiveOutOfStock_HighlightDanger()
    {
        var product = new Product
        {
            IsActive = true,
            Quantity = 0,
            MinStockLevel = 10
        };

        Assert.Equal(RowHighlightLevel.Danger, product.HighlightLevel);
    }

    [Fact]
    public void Product_ActiveLowStock_HighlightWarning()
    {
        var product = new Product
        {
            IsActive = true,
            Quantity = 5,
            MinStockLevel = 10
        };

        Assert.Equal(RowHighlightLevel.Warning, product.HighlightLevel);
    }

    [Fact]
    public void Product_ActiveAtMinStock_HighlightWarning()
    {
        var product = new Product
        {
            IsActive = true,
            Quantity = 10,
            MinStockLevel = 10
        };

        Assert.Equal(RowHighlightLevel.Warning, product.HighlightLevel);
    }

    [Fact]
    public void Product_Inactive_HighlightInactive_RegardlessOfStock()
    {
        var product = new Product
        {
            IsActive = false,
            Quantity = 50,
            MinStockLevel = 10
        };

        Assert.Equal(RowHighlightLevel.Inactive, product.HighlightLevel);
    }

    [Fact]
    public void Product_InactiveOutOfStock_HighlightInactive()
    {
        // Inactive takes priority over out-of-stock
        var product = new Product
        {
            IsActive = false,
            Quantity = 0,
            MinStockLevel = 10
        };

        Assert.Equal(RowHighlightLevel.Inactive, product.HighlightLevel);
    }

    [Fact]
    public void Product_ActiveNoMinStockSet_HighlightNone()
    {
        var product = new Product
        {
            IsActive = true,
            Quantity = 3,
            MinStockLevel = 0  // Not configured
        };

        Assert.Equal(RowHighlightLevel.None, product.HighlightLevel);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Vendor.HighlightLevel
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Vendor_Active_HighlightNone()
    {
        var vendor = new Vendor { IsActive = true };
        Assert.Equal(RowHighlightLevel.None, vendor.HighlightLevel);
    }

    [Fact]
    public void Vendor_Inactive_HighlightInactive()
    {
        var vendor = new Vendor { IsActive = false };
        Assert.Equal(RowHighlightLevel.Inactive, vendor.HighlightLevel);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Brand.HighlightLevel
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Brand_Active_HighlightNone()
    {
        var brand = new Brand { IsActive = true };
        Assert.Equal(RowHighlightLevel.None, brand.HighlightLevel);
    }

    [Fact]
    public void Brand_Inactive_HighlightInactive()
    {
        var brand = new Brand { IsActive = false };
        Assert.Equal(RowHighlightLevel.Inactive, brand.HighlightLevel);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Category.HighlightLevel
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Category_Active_HighlightNone()
    {
        var cat = new Category { IsActive = true };
        Assert.Equal(RowHighlightLevel.None, cat.HighlightLevel);
    }

    [Fact]
    public void Category_Inactive_HighlightInactive()
    {
        var cat = new Category { IsActive = false };
        Assert.Equal(RowHighlightLevel.Inactive, cat.HighlightLevel);
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

    private static double ExtractBrushOpacity(string path, string key)
    {
        var doc = XDocument.Load(path);
        var ns = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");
        foreach (var el in doc.Descendants())
        {
            var keyAttr = el.Attribute(ns + "Key");
            if (keyAttr?.Value == key)
            {
                var opacity = el.Attribute("Opacity");
                if (opacity is not null && double.TryParse(opacity.Value, out var v))
                    return v;
                return 1.0; // default opacity
            }
        }
        throw new InvalidOperationException($"Key '{key}' not found in {path}");
    }
}
