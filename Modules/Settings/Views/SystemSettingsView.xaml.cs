using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Settings.ViewModels;

namespace StoreAssistantPro.Modules.Settings.Views;

public partial class SystemSettingsView : UserControl
{
    public SystemSettingsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SystemSettingsViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
