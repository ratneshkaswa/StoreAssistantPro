using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Applies Windows 11 DWM rounded corners to WPF windows.
/// Falls back gracefully on older Windows versions.
/// Without custom WindowChrome, windows use the standard WPF title bar
/// with native close/minimize/maximize buttons.
/// </summary>
public static class Win11Backdrop
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        nint hwnd, int attribute, ref int value, int size);

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMWCP_ROUND = 2;
    private const int DWMSBT_NONE = 1;

    /// <summary>
    /// Applies DWM rounded corners to a top-level window.
    /// Call from SourceInitialized event.
    /// </summary>
    public static void Apply(Window window)
    {
        if (!IsWindows11OrLater()) return;

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == nint.Zero) return;

        int cornerPref = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE,
            ref cornerPref, sizeof(int));

        ApplyLightNoBackdrop(hwnd);
    }

    /// <summary>
    /// Applies DWM rounded corners to a dialog window.
    /// Required for NoResize dialogs which don't get rounded corners by default.
    /// </summary>
    public static void ApplyDialog(Window window)
    {
        if (!IsWindows11OrLater()) return;

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == nint.Zero) return;

        int cornerPref = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE,
            ref cornerPref, sizeof(int));

        ApplyLightNoBackdrop(hwnd);
    }

    private static void ApplyLightNoBackdrop(nint hwnd)
    {
        // Force light title bar chrome regardless of OS dark mode.
        int useDarkMode = 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref useDarkMode, sizeof(int));

        // Disable Mica/Acrylic/Tabbed backdrop for a flat surface.
        int backdrop = DWMSBT_NONE;
        DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE,
            ref backdrop, sizeof(int));
    }

    private static bool IsWindows11OrLater()
    {
        return Environment.OSVersion.Version.Build >= 22000;
    }
}
