using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.SystemSettings.ViewModels;

namespace StoreAssistantPro.Modules.SystemSettings.Views;

public partial class AppInfoView : UserControl
{
    public AppInfoView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AppInfoViewModel vm)
            await vm.CheckConnectionCommand.ExecuteAsync(null);
    }

    /// <summary>13c: Copy version and connection info to clipboard.</summary>
    private void OnCopyInfoClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AppInfoViewModel vm) return;

        var text = $"""
            Application: Store Assistant Pro
            Version: {vm.AppVersion}
            User: {vm.CurrentUser}
            Firm: {vm.FirmName}
            Database: {vm.DatabaseStatus}
            """;

        Clipboard.SetText(text.Trim());
    }
}
