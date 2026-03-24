using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Modules.GRN.ViewModels;

/// <summary>Mutable input row for direct GRN creation (#364).</summary>
public partial class GRNLineInput : ObservableObject
{
    [ObservableProperty]
    public partial int ProductId { get; set; }

    [ObservableProperty]
    public partial int QtyExpected { get; set; }

    [ObservableProperty]
    public partial decimal UnitCost { get; set; }
}
