using System.Windows;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Modules.MainShell.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.RequestClose = Close;
        };
        Closed += (_, _) => (DataContext as IDisposable)?.Dispose();
    }
}