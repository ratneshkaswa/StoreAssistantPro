using System.Windows;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Modules.MainShell.Views;

public partial class MainWindow : Window
{
    public MainWindow(IWindowSizingService sizingService)
    {
        InitializeComponent();
        sizingService.ConfigureMainWindow(this);

        SourceInitialized += (_, _) => Win11Backdrop.Apply(this);

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.RequestClose = Close;
                vm.ApplyShortcuts(this);
            }
        };
        Closed += (_, _) => (DataContext as IDisposable)?.Dispose();
    }
}