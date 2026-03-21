using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Quotations.ViewModels;

namespace StoreAssistantPro.Modules.Quotations.Views;

public partial class QuotationManagementView : UserControl
{
    public QuotationManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is QuotationViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
