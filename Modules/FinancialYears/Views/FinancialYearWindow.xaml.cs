using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.FinancialYears.ViewModels;

namespace StoreAssistantPro.Modules.FinancialYears.Views;

public partial class FinancialYearWindow : BaseDialogWindow
{
    protected override double DialogWidth => 550;
    protected override double DialogHeight => 550;

    public FinancialYearWindow(
        IWindowSizingService sizingService,
        FinancialYearViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is FinancialYearViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
