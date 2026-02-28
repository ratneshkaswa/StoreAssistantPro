using System.Windows;

namespace StoreAssistantPro.Core.Services;

public class WindowSizingService : IWindowSizingService
{
    private const double MainWindowFillRatio = 0.9;

    private Window? _mainWindow;

    public void ConfigureMainWindow(Window window)
    {
        _mainWindow = window;

        ApplyMainWindowSizing();

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.WindowState = WindowState.Normal;
        window.ResizeMode = ResizeMode.NoResize;

        SystemParameters.StaticPropertyChanged += OnDisplayChanged;
        window.Closed += (_, _) => SystemParameters.StaticPropertyChanged -= OnDisplayChanged;
    }

    public void ConfigureDialogWindow(Window dialog, double width, double height)
    {
        dialog.Width = width;
        dialog.Height = height;
        dialog.ResizeMode = ResizeMode.NoResize;

        if (_mainWindow is not null)
        {
            dialog.Owner = _mainWindow;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    public void ConfigureStartupWindow(Window window, double width, double height)
    {
        window.Width = width;
        window.Height = height;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.ResizeMode = ResizeMode.NoResize;
    }

    private void OnDisplayChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.WorkArea))
            ApplyMainWindowSizing();
    }

    private void ApplyMainWindowSizing()
    {
        if (_mainWindow is null) return;

        var workArea = SystemParameters.WorkArea;

        _mainWindow.Width = workArea.Width * MainWindowFillRatio;
        _mainWindow.Height = workArea.Height * MainWindowFillRatio;
        _mainWindow.Left = workArea.Left + (workArea.Width - _mainWindow.Width) / 2;
        _mainWindow.Top = workArea.Top + (workArea.Height - _mainWindow.Height) / 2;
    }
}
