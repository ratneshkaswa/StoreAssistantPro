using System.Windows;
using StoreAssistantPro.Modules.SystemSettings.ViewModels;

namespace StoreAssistantPro.Modules.SystemSettings.Views;

public partial class SystemSettingsWindow : Window
{
    public SystemSettingsWindow(SystemSettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
