using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.FinancialYears.ViewModels;

namespace StoreAssistantPro.Modules.FinancialYears.Views;

public partial class FinancialYearView : UserControl
{
    public FinancialYearView()
    {
        InitializeComponent();
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
