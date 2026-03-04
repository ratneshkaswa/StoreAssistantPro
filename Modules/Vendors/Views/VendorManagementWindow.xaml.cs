using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Vendors.ViewModels;

namespace StoreAssistantPro.Modules.Vendors.Views;

public partial class VendorManagementWindow : BaseDialogWindow
{
    protected override double DialogWidth => 820;
    protected override double DialogHeight => 650;

    public VendorManagementWindow(
        IWindowSizingService sizingService,
        VendorManagementViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is VendorManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
