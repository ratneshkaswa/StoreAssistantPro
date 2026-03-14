using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Customers.ViewModels;

namespace StoreAssistantPro.Modules.Customers.Views;

public partial class CustomerManagementWindow : BaseDialogWindow
{
    protected override double DialogWidth => 950;
    protected override double DialogHeight => 800;
    protected override double DialogMinWidth => 820;
    protected override double DialogMinHeight => 680;
    protected override bool AllowResize => true;

    public CustomerManagementWindow(
        IWindowSizingService sizingService,
        CustomerManagementViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        RunDeferredInitialLoad(async () =>
        {
            if (DataContext is CustomerManagementViewModel vm)
            {
                try { await vm.LoadCommand.ExecuteAsync(null); }
                catch { /* RunLoadAsync handles logging */ }
            }
        });
}
