using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.SystemSettings.ViewModels;

namespace StoreAssistantPro.Modules.SystemSettings.Views;

public partial class BackupSettingsView : UserControl
{
    public BackupSettingsView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BackupSettingsViewModel vm)
            vm.LoadCommand.Execute(null);
    }
}
