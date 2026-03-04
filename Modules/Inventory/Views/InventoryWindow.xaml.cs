using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Inventory.ViewModels;

namespace StoreAssistantPro.Modules.Inventory.Views;

public partial class InventoryWindow : BaseDialogWindow
{
    protected override double DialogWidth => 1050;
    protected override double DialogHeight => 800;

    public InventoryWindow(
        IWindowSizingService sizingService,
        InventoryViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InventoryViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
