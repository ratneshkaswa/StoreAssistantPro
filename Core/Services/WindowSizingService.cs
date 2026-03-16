using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace StoreAssistantPro.Core.Services;

public class WindowSizingService : IWindowSizingService
{
    private const double MainWindowFillRatio = 0.9;
    private const double DialogMargin = 16;
    private const uint MonitorDefaultToNearest = 2;

    private readonly HashSet<Window> _visibilityGuardedWindows = [];
    private Window? _mainWindow;

    public void ConfigureMainWindow(Window window)
    {
        _mainWindow = window;

        window.ShowActivated = true;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.WindowState = WindowState.Maximized;
        window.ResizeMode = ResizeMode.NoResize;
        AttachVisibilityGuard(window);

        SystemParameters.StaticPropertyChanged += OnDisplayChanged;
        window.Closed += (_, _) =>
        {
            SystemParameters.StaticPropertyChanged -= OnDisplayChanged;

            if (ReferenceEquals(_mainWindow, window))
                _mainWindow = null;
        };
    }

    public void ConfigureDialogWindow(Window dialog, double width, double height)
    {
        var workArea = SystemParameters.WorkArea;

        dialog.Width = Math.Min(width, workArea.Width - DialogMargin);
        dialog.Height = Math.Min(height, workArea.Height - DialogMargin);
        dialog.MinWidth = Math.Min(dialog.MinWidth, dialog.Width);
        dialog.MinHeight = Math.Min(dialog.MinHeight, dialog.Height);
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

        AttachVisibilityGuard(dialog);
    }

    public void ConfigureStartupWindow(Window window, double width, double height)
    {
        var workArea = SystemParameters.WorkArea;

        window.Width = Math.Min(width, workArea.Width - DialogMargin);
        window.Height = Math.Min(height, workArea.Height - DialogMargin);
        window.MinWidth = Math.Min(window.MinWidth, window.Width);
        window.MinHeight = Math.Min(window.MinHeight, window.Height);
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.ResizeMode = ResizeMode.NoResize;
        AttachVisibilityGuard(window);
    }

    private void OnDisplayChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.WorkArea))
            ApplyMainWindowSizing();
    }

    private void ApplyMainWindowSizing()
    {
        if (_mainWindow is null)
            return;

        _mainWindow.WindowState = WindowState.Maximized;
    }

    public void ConfigurePrimaryWindow(Window window)
    {
        var workArea = SystemParameters.WorkArea;

        window.Width = workArea.Width * MainWindowFillRatio;
        window.Height = workArea.Height * MainWindowFillRatio;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.WindowState = WindowState.Normal;
        window.ResizeMode = ResizeMode.NoResize;
        AttachVisibilityGuard(window);
    }

    internal static Rect ClampToVisibleArea(Rect windowBounds, Rect workArea, double margin = DialogMargin)
    {
        var availableWidth = Math.Max(0, workArea.Width - margin);
        var availableHeight = Math.Max(0, workArea.Height - margin);

        var width = Math.Min(windowBounds.Width, availableWidth);
        var height = Math.Min(windowBounds.Height, availableHeight);
        var edgePadding = margin / 2;

        var minLeft = workArea.Left + edgePadding;
        var minTop = workArea.Top + edgePadding;
        var maxLeft = workArea.Right - width - edgePadding;
        var maxTop = workArea.Bottom - height - edgePadding;

        if (maxLeft < minLeft)
            minLeft = maxLeft = workArea.Left;

        if (maxTop < minTop)
            minTop = maxTop = workArea.Top;

        return new Rect(
            Clamp(windowBounds.Left, minLeft, maxLeft),
            Clamp(windowBounds.Top, minTop, maxTop),
            width,
            height);
    }

    private void AttachVisibilityGuard(Window window)
    {
        if (!_visibilityGuardedWindows.Add(window))
            return;

        window.SourceInitialized += (_, _) => ClampWindowToVisibleArea(window);
        window.Loaded += (_, _) => ClampWindowToVisibleArea(window);
        window.ContentRendered += (_, _) => ClampWindowToVisibleArea(window);
        window.SizeChanged += (_, _) => ClampWindowToVisibleArea(window);
        window.Closed += (_, _) => _visibilityGuardedWindows.Remove(window);
    }

    private static void ClampWindowToVisibleArea(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
            return;

        var width = window.ActualWidth > 0 ? window.ActualWidth : window.Width;
        var height = window.ActualHeight > 0 ? window.ActualHeight : window.Height;

        if (width <= 0
            || height <= 0
            || double.IsNaN(window.Left)
            || double.IsNaN(window.Top))
        {
            return;
        }

        var workArea = GetWorkArea(window);
        var bounds = ClampToVisibleArea(new Rect(window.Left, window.Top, width, height), workArea);

        if (Math.Abs(window.Width - bounds.Width) > 0.5)
            window.Width = bounds.Width;

        if (Math.Abs(window.Height - bounds.Height) > 0.5)
            window.Height = bounds.Height;

        if (Math.Abs(window.Left - bounds.Left) > 0.5)
            window.Left = bounds.Left;

        if (Math.Abs(window.Top - bounds.Top) > 0.5)
            window.Top = bounds.Top;
    }

    private static Rect GetWorkArea(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return SystemParameters.WorkArea;

        var monitor = MonitorFromWindow(handle, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
            return SystemParameters.WorkArea;

        var monitorInfo = new MonitorInfo { cbSize = (uint)Marshal.SizeOf<MonitorInfo>() };
        return GetMonitorInfo(monitor, ref monitorInfo)
            ? monitorInfo.WorkArea.ToRect()
            : SystemParameters.WorkArea;
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (maximum < minimum)
            return minimum;

        return Math.Max(minimum, Math.Min(value, maximum));
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public uint cbSize;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public Rect ToRect() => new(Left, Top, Right - Left, Bottom - Top);
    }
}
