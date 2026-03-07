using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Applies Windows 11 DWM rounded corners and custom WindowChrome to WPF windows.
/// Falls back gracefully on older Windows versions.
/// </summary>
public static class Win11Backdrop
{
    // ── DWM interop ──

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        nint hwnd, int attribute, ref int value, int size);

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

    // Corner preference: 0=Default, 1=DoNotRound, 2=Round, 3=RoundSmall
    private const int DWMWCP_ROUND = 2;

    /// <summary>
    /// Applies DWM rounded corners + custom WindowChrome to a window.
    /// Call from Loaded or SourceInitialized event.
    /// </summary>
    public static void Apply(Window window, bool useMicaAlt = false)
    {
        if (!IsWindows11OrLater()) return;

        // Apply WindowChrome for extended title bar.
        // CornerRadius is 0 — DWM handles rounding at the compositor level.
        var chrome = new WindowChrome
        {
            CaptionHeight = 48,
            GlassFrameThickness = new Thickness(-1),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = true,
            ResizeBorderThickness = new Thickness(4)
        };
        WindowChrome.SetWindowChrome(window, chrome);

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == nint.Zero) return;

        // Force DWM rounded corners (required for NoResize windows)
        int cornerPref = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE,
            ref cornerPref, sizeof(int));
    }

    /// <summary>
    /// Applies DWM rounded corners + WindowChrome to a dialog window.
    /// </summary>
    public static void ApplyDialog(Window window)
    {
        if (!IsWindows11OrLater()) return;

        var chrome = new WindowChrome
        {
            CaptionHeight = 36,
            GlassFrameThickness = new Thickness(-1),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = true,
            ResizeBorderThickness = new Thickness(0)
        };
        WindowChrome.SetWindowChrome(window, chrome);

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == nint.Zero) return;

        // Force DWM rounded corners (required for NoResize windows)
        int cornerPref = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE,
            ref cornerPref, sizeof(int));
    }

    private static bool IsWindows11OrLater()
    {
        return Environment.OSVersion.Version.Build >= 22000;
    }
}
