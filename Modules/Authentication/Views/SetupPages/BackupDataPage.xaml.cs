using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication.Views.SetupPages;

public partial class BackupDataPage : Page
{
    public BackupDataPage()
    {
        InitializeComponent();
    }

    private void OnBrowseBackupFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Backup Folder"
        };

        if (DataContext is SetupViewModel vm && !string.IsNullOrWhiteSpace(vm.BackupLocation)
            && System.IO.Directory.Exists(vm.BackupLocation))
        {
            dialog.InitialDirectory = vm.BackupLocation;
        }

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            if (DataContext is SetupViewModel vmAfter)
                vmAfter.BackupLocation = dialog.FolderName;
        }
    }
}
