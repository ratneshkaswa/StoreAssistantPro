using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Billing.ViewModels;

namespace StoreAssistantPro.Modules.Billing.Views;

public partial class SaleHistoryWindow : BaseDialogWindow
{
    protected override double DialogWidth => 1100;
    protected override double DialogHeight => 750;
    protected override double DialogMinWidth => 900;
    protected override double DialogMinHeight => 640;
    protected override bool AllowResize => true;

    public SaleHistoryWindow(
        IWindowSizingService sizingService,
        SaleHistoryViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SaleHistoryViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
