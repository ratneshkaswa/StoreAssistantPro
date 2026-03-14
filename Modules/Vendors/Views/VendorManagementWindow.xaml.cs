using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Vendors.ViewModels;

namespace StoreAssistantPro.Modules.Vendors.Views;

public partial class VendorManagementWindow : BaseDialogWindow
{
    protected override double DialogWidth => 820;
    protected override double DialogHeight => 650;
    protected override double DialogMinWidth => 760;
    protected override double DialogMinHeight => 620;
    protected override bool AllowResize => true;

    public VendorManagementWindow(
        IWindowSizingService sizingService,
        VendorManagementViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        RunDeferredInitialLoad(async () =>
        {
            if (DataContext is VendorManagementViewModel vm)
            {
                try { await vm.LoadCommand.ExecuteAsync(null); }
                catch { /* RunLoadAsync handles logging */ }
            }
        });
}
