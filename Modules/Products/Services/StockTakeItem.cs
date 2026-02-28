using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Modules.Products.Services;

/// <summary>
/// Single row in a stock take session.
/// Tracks a product's system quantity, physical count, and discrepancy.
/// <para><b>Feature #69</b> — Stock take / physical count.</para>
/// </summary>
public partial class StockTakeItem : ObservableObject
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public string? SKU { get; init; }
    public int SystemQty { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Discrepancy))]
    [NotifyPropertyChangedFor(nameof(HasDiscrepancy))]
    public partial int? PhysicalQty { get; set; }

    /// <summary>Physical − System. Null if not yet counted.</summary>
    public int? Discrepancy => PhysicalQty.HasValue ? PhysicalQty.Value - SystemQty : null;

    /// <summary>True if counted and counts differ from system.</summary>
    public bool HasDiscrepancy => Discrepancy is not null and not 0;
}
