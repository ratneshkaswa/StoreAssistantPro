using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.PurchaseOrders.ViewModels;

namespace StoreAssistantPro.Modules.PurchaseOrders.Views;

public partial class PurchaseOrderWindow : BaseDialogWindow
{
    protected override double DialogWidth => 1150;
    protected override double DialogHeight => 800;

    public PurchaseOrderWindow(
        IWindowSizingService sizingService,
        PurchaseOrderViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PurchaseOrderViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
