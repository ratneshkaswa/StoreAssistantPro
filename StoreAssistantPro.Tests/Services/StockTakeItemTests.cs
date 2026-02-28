using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Tests.Services;

/// <summary>
/// Tests for <see cref="StockTakeItem"/> — Feature #69.
/// </summary>
public class StockTakeItemTests
{
    [Fact]
    public void Discrepancy_Null_WhenNotCounted()
    {
        var item = new StockTakeItem { SystemQty = 10, PhysicalQty = null };

        Assert.Null(item.Discrepancy);
        Assert.False(item.HasDiscrepancy);
    }

    [Fact]
    public void Discrepancy_Zero_WhenMatches()
    {
        var item = new StockTakeItem { SystemQty = 10, PhysicalQty = 10 };

        Assert.Equal(0, item.Discrepancy);
        Assert.False(item.HasDiscrepancy);
    }

    [Fact]
    public void Discrepancy_Positive_WhenPhysicalGreater()
    {
        var item = new StockTakeItem { SystemQty = 10, PhysicalQty = 15 };

        Assert.Equal(5, item.Discrepancy);
        Assert.True(item.HasDiscrepancy);
    }

    [Fact]
    public void Discrepancy_Negative_WhenPhysicalLess()
    {
        var item = new StockTakeItem { SystemQty = 10, PhysicalQty = 7 };

        Assert.Equal(-3, item.Discrepancy);
        Assert.True(item.HasDiscrepancy);
    }

    [Fact]
    public void PropertyChanged_Fires_OnPhysicalQtySet()
    {
        var item = new StockTakeItem { SystemQty = 10 };
        var changedProps = new List<string>();
        item.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName!);

        item.PhysicalQty = 8;

        Assert.Contains("PhysicalQty", changedProps);
        Assert.Contains("Discrepancy", changedProps);
        Assert.Contains("HasDiscrepancy", changedProps);
    }

    [Fact]
    public void PhysicalQty_Zero_ShowsNegativeDiscrepancy()
    {
        var item = new StockTakeItem { SystemQty = 5, PhysicalQty = 0 };

        Assert.Equal(-5, item.Discrepancy);
        Assert.True(item.HasDiscrepancy);
    }

    [Fact]
    public void ProductName_Barcode_SKU_StoreValues()
    {
        var item = new StockTakeItem
        {
            ProductId = 42,
            ProductName = "Test Widget",
            Barcode = "8901234567890",
            SKU = "WDG-001",
            SystemQty = 100
        };

        Assert.Equal(42, item.ProductId);
        Assert.Equal("Test Widget", item.ProductName);
        Assert.Equal("8901234567890", item.Barcode);
        Assert.Equal("WDG-001", item.SKU);
        Assert.Equal(100, item.SystemQty);
    }
}
