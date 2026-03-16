using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Ironing.ViewModels;

namespace StoreAssistantPro.Modules.Ironing.Views;

public partial class IroningManagementView : UserControl
{
    public IroningManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IroningManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
