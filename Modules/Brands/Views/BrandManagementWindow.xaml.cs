using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Brands.ViewModels;

namespace StoreAssistantPro.Modules.Brands.Views;

public partial class BrandManagementWindow : BaseDialogWindow
{
    protected override double DialogWidth => 800;
    protected override double DialogHeight => 640;
    protected override double DialogMinWidth => 700;
    protected override double DialogMinHeight => 580;
    protected override bool AllowResize => true;

    public BrandManagementWindow(
        IWindowSizingService sizingService,
        BrandManagementViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        RunDeferredInitialLoad(async () =>
        {
            if (DataContext is BrandManagementViewModel vm)
            {
                try { await vm.LoadCommand.ExecuteAsync(null); }
                catch { /* RunLoadAsync handles logging */ }
            }
        });
}
