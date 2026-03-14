using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Tax.ViewModels;

namespace StoreAssistantPro.Modules.Tax.Views;

public partial class TaxManagementWindow : BaseDialogWindow
{
    protected override double DialogWidth => 1050;
    protected override double DialogHeight => 780;
    protected override double DialogMinWidth => 880;
    protected override double DialogMinHeight => 680;
    protected override bool AllowResize => true;

    public TaxManagementWindow(
        IWindowSizingService sizingService,
        TaxManagementViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TaxManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
