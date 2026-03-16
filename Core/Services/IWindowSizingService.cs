using System.Windows;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Standardizes window sizing rules across the application.
/// <para>
/// <b>Main window:</b> Maximized to full screen, no resize.
/// </para>
/// <para>
/// <b>Dialog windows:</b> Preferred size relative to the work area,
/// centered over owner. Derived windows may opt into resize behavior.
/// </para>
/// <para>
/// <b>Startup windows:</b> Fixed size, centered on screen (no owner).
/// </para>
/// </summary>
public interface IWindowSizingService
{
    /// <summary>
    /// Configure the main application shell window.
    /// Maximizes to full screen, disables resize.
    /// </summary>
    void ConfigureMainWindow(Window window);

    /// <summary>
    /// Configure a modal dialog window.
    /// Sets owner, centers relative to owner, and clamps the initial size
    /// to the current work area.
    /// </summary>
    void ConfigureDialogWindow(Window dialog, double width, double height);

    /// <summary>
    /// Configure a startup/authentication window (no owner exists).
    /// Centers on screen, disables resize.
    /// </summary>
    void ConfigureStartupWindow(Window window, double width, double height);

    /// <summary>
    /// Configure a primary full-screen window (Setup, Main) at 90% of
    /// screen work area, centered, no resize.  Unlike
    /// <see cref="ConfigureMainWindow"/>, this does NOT register the
    /// window as the dialog-owner reference.
    /// </summary>
    void ConfigurePrimaryWindow(Window window);
}
