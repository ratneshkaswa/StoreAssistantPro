using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Models;

/// <summary>
/// Tests for <see cref="StockAdjustmentLog"/> model.
/// Feature #68 — Stock adjustment audit trail.
/// </summary>
public class StockAdjustmentLogTests
{
    [Fact]
    public void QuantityAfter_IsBeforePlusAdjustment()
    {
        var log = new StockAdjustmentLog
        {
            QuantityBefore = 50,
            AdjustmentQty = -10,
            QuantityAfter = 40
        };

        Assert.Equal(log.QuantityBefore + log.AdjustmentQty, log.QuantityAfter);
    }

    [Fact]
    public void Defaults_SourceIsManual()
    {
        var log = new StockAdjustmentLog();
        Assert.Equal("Manual", log.Source);
    }

    [Fact]
    public void Defaults_ProductNameIsEmpty()
    {
        var log = new StockAdjustmentLog();
        Assert.Equal(string.Empty, log.ProductName);
    }

    [Fact]
    public void Reason_CanBeNull()
    {
        var log = new StockAdjustmentLog { Reason = null };
        Assert.Null(log.Reason);
    }

    [Fact]
    public void PositiveAdjustment_IncreasesStock()
    {
        var log = new StockAdjustmentLog
        {
            QuantityBefore = 20,
            AdjustmentQty = 30,
            QuantityAfter = 50
        };

        Assert.True(log.AdjustmentQty > 0);
        Assert.True(log.QuantityAfter > log.QuantityBefore);
    }

    [Fact]
    public void NegativeAdjustment_DecreasesStock()
    {
        var log = new StockAdjustmentLog
        {
            QuantityBefore = 100,
            AdjustmentQty = -25,
            QuantityAfter = 75
        };

        Assert.True(log.AdjustmentQty < 0);
        Assert.True(log.QuantityAfter < log.QuantityBefore);
    }

    [Fact]
    public void DenormalizedProductName_PreservesHistoricalName()
    {
        var log = new StockAdjustmentLog
        {
            ProductName = "Blue Denim Jeans - Size 32"
        };

        Assert.Equal("Blue Denim Jeans - Size 32", log.ProductName);
    }

    [Fact]
    public void Source_CanBeCustomValue()
    {
        var log = new StockAdjustmentLog { Source = "BulkImport" };
        Assert.Equal("BulkImport", log.Source);
    }

    [Fact]
    public void Reason_CombinesCodeAndNotes()
    {
        // Simulates the ViewModel pattern: "Damage: Broken during handling"
        var code = "Damage";
        var notes = "Broken during handling";
        var combined = $"{code}: {notes}";

        var log = new StockAdjustmentLog { Reason = combined };
        Assert.StartsWith("Damage", log.Reason);
        Assert.Contains("Broken during handling", log.Reason);
    }

    [Fact]
    public void Reason_CodeOnly_NoNotes()
    {
        var log = new StockAdjustmentLog { Reason = "Theft" };
        Assert.Equal("Theft", log.Reason);
    }
}
