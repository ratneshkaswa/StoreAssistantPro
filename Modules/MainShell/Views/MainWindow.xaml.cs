using System.Windows;
using System.Windows.Media.Imaging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Modules.MainShell.Views;

public partial class MainWindow : Window
{
    public MainWindow(IWindowSizingService sizingService)
    {
        InitializeComponent();
        sizingService.ConfigureMainWindow(this);
        TrySetAppIcon();

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

    private void TrySetAppIcon()
    {
        var uri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
        var info = Application.GetResourceStream(uri);
        if (info != null)
        {
            Icon = BitmapFrame.Create(info.Stream);
        }
    }
}