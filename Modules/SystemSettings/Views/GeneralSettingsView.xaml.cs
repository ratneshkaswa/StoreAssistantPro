using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.SystemSettings.ViewModels;

namespace StoreAssistantPro.Modules.SystemSettings.Views;

public partial class GeneralSettingsView : UserControl
{
    public GeneralSettingsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is GeneralSettingsViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
