using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Inventory.ViewModels;

namespace StoreAssistantPro.Modules.Inventory.Views;

public partial class InventoryWindow : BaseDialogWindow
{
    protected override double DialogWidth => 1050;
    protected override double DialogHeight => 800;
    protected override double DialogMinWidth => 860;
    protected override double DialogMinHeight => 680;
    protected override bool AllowResize => true;

    public InventoryWindow(
        IWindowSizingService sizingService,
        InventoryViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        RunDeferredInitialLoad(async () =>
        {
            if (DataContext is InventoryViewModel vm)
            {
                try { await vm.LoadCommand.ExecuteAsync(null); }
                catch { /* RunLoadAsync handles logging */ }
            }
        });
}
