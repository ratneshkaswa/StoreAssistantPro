using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Payments.ViewModels;

namespace StoreAssistantPro.Modules.Payments.Views;

public partial class PaymentManagementView : UserControl
{
    public PaymentManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PaymentManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
