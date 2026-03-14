using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.FinancialYears.ViewModels;

namespace StoreAssistantPro.Modules.FinancialYears.Views;

public partial class FinancialYearWindow : BaseDialogWindow
{
    protected override double DialogWidth => 550;
    protected override double DialogHeight => 550;
    protected override double DialogMinWidth => 520;
    protected override double DialogMinHeight => 500;
    protected override bool AllowResize => true;

    public FinancialYearWindow(
        IWindowSizingService sizingService,
        FinancialYearViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        RunDeferredInitialLoad(async () =>
        {
            if (DataContext is FinancialYearViewModel vm)
            {
                try { await vm.LoadCommand.ExecuteAsync(null); }
                catch { /* RunLoadAsync handles logging */ }
            }
        });
}
