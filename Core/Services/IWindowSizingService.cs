using System.Windows;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Standardizes window sizing rules across the application.
/// <para>
/// <b>Main window:</b> 90% of screen working area, centered, no resize.
/// </para>
/// <para>
/// <b>Dialog windows:</b> Fixed size relative to main window,
/// centered over owner, no resize.
/// </para>
/// <para>
/// <b>Startup windows:</b> Fixed size, centered on screen (no owner).
/// </para>
/// </summary>
public interface IWindowSizingService
{
    /// <summary>
    /// Configure the main application shell window.
    /// Sets size to 90% of screen, centers, disables resize.
    /// </summary>
    void ConfigureMainWindow(Window window);

    /// <summary>
    /// Configure a modal dialog window.
    /// Sets owner, centers relative to owner, disables resize.
    /// </summary>
    void ConfigureDialogWindow(Window dialog, double width, double height);

    /// <summary>
    /// Configure a startup/authentication window (no owner exists).
    /// Centers on screen, disables resize.
    /// </summary>
    void ConfigureStartupWindow(Window window, double width, double height);
}
