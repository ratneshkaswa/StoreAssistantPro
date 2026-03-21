using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Inventory.ViewModels;

/// <summary>
/// Wraps a <see cref="StockTakeItem"/> for editable counted quantity in the UI.
/// </summary>
public partial class StockTakeItemVm : ObservableObject
{
    public StockTakeItemVm(StockTakeItem item)
    {
        ItemId = item.Id;
        ProductName = item.ProductName;
        SystemQuantity = item.SystemQuantity;
        CountedQuantity = item.CountedQuantity;
        IsCounted = item.IsCounted;
        CountedText = item.IsCounted ? item.CountedQuantity?.ToString() ?? string.Empty : string.Empty;
    }

    public int ItemId { get; }
    public string ProductName { get; }
    public int SystemQuantity { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Variance))]
    public partial int? CountedQuantity { get; set; }

    [ObservableProperty]
    public partial bool IsCounted { get; set; }

    [ObservableProperty]
    public partial string CountedText { get; set; }

    public int Variance => (CountedQuantity ?? SystemQuantity) - SystemQuantity;
}
