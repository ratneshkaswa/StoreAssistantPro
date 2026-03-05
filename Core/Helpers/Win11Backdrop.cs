using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Applies Windows 11 Mica backdrop and custom WindowChrome to WPF windows.
/// Falls back gracefully on older Windows versions.
/// </summary>
public static class Win11Backdrop
{
    // ── DWM interop ──

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        nint hwnd, int attribute, ref int value, int size);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    // Backdrop types: 0=Auto, 1=None, 2=Mica, 3=Acrylic, 4=MicaAlt
    private const int BACKDROP_MICA = 2;
    private const int BACKDROP_MICA_ALT = 4;

    /// <summary>
    /// Applies Mica backdrop + custom WindowChrome to a window.
    /// Call from Loaded or SourceInitialized event.
    /// </summary>
    public static void Apply(Window window, bool useMicaAlt = false)
    {
        if (!IsWindows11OrLater()) return;

        // Make window background transparent so Mica shows through
        window.Background = Brushes.Transparent;

        // Apply WindowChrome for extended title bar
        var chrome = new WindowChrome
        {
            CaptionHeight = 48,
            GlassFrameThickness = new Thickness(-1),
            CornerRadius = new CornerRadius(8),
            UseAeroCaptionButtons = true,
            ResizeBorderThickness = new Thickness(4)
        };
        WindowChrome.SetWindowChrome(window, chrome);

        // Apply Mica via DWM
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == nint.Zero) return;

        int backdropType = useMicaAlt ? BACKDROP_MICA_ALT : BACKDROP_MICA;
        DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE,
            ref backdropType, sizeof(int));
    }

    /// <summary>
    /// Applies Mica to a dialog window (no resize, centered).
    /// </summary>
    public static void ApplyDialog(Window window)
    {
        if (!IsWindows11OrLater()) return;

        window.Background = Brushes.Transparent;

        var chrome = new WindowChrome
        {
            CaptionHeight = 36,
            GlassFrameThickness = new Thickness(-1),
            CornerRadius = new CornerRadius(8),
            UseAeroCaptionButtons = true,
            ResizeBorderThickness = new Thickness(0)
        };
        WindowChrome.SetWindowChrome(window, chrome);

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == nint.Zero) return;

        int backdropType = BACKDROP_MICA_ALT;
        DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE,
            ref backdropType, sizeof(int));
    }

    private static bool IsWindows11OrLater()
    {
        return Environment.OSVersion.Version.Build >= 22000;
    }
}
