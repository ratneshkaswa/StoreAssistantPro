using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Resolves <see cref="Window"/> instances from the DI container and
/// displays them as modal dialogs. Supports both generic (compile-time)
/// and string-key (runtime) resolution for full module decoupling.
/// <para>
/// Windows inheriting <see cref="BaseDialogWindow"/> are already
/// configured by their base constructor. All other windows are
/// configured via <see cref="IWindowSizingService"/>.
/// </para>
/// </summary>
public class WindowRegistry(
    IServiceProvider serviceProvider,
    IWindowSizingService sizingService,
    ILogger<WindowRegistry> logger) : IWindowRegistry
{
    private readonly Dictionary<string, Type> _dialogMap = [];

    public void RegisterDialog<TWindow>(string dialogKey) where TWindow : Window
    {
        _dialogMap[dialogKey] = typeof(TWindow);
    }

    /// <summary>
    /// Non-generic registration used by <see cref="HostingExtensions.ApplyDialogRegistrations"/>
    /// to avoid reflection. The <paramref name="windowType"/> must derive from <see cref="Window"/>.
    /// </summary>
    internal void RegisterDialog(string dialogKey, Type windowType)
    {
        _dialogMap[dialogKey] = windowType;
    }

    public bool? ShowDialog(string dialogKey)
    {
        try
        {
            if (!_dialogMap.TryGetValue(dialogKey, out var windowType))
                throw new InvalidOperationException($"No dialog registered for key '{dialogKey}'.");

            var window = (Window)serviceProvider.GetRequiredService(windowType);
            ApplySizingIfNeeded(window);
            return window.ShowDialog();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open dialog {DialogKey}", dialogKey);
            ShowFallbackOpenError(dialogKey);
            return false;
        }
    }

    public bool? ShowDialog<TWindow>() where TWindow : Window
    {
        try
        {
            var window = serviceProvider.GetRequiredService<TWindow>();
            ApplySizingIfNeeded(window);
            return window.ShowDialog();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open dialog {DialogType}", typeof(TWindow).Name);
            ShowFallbackOpenError(typeof(TWindow).Name);
            return false;
        }
    }

    private void ApplySizingIfNeeded(Window window)
    {
        if (window is BaseDialogWindow)
            return;

        sizingService.ConfigureDialogWindow(window, window.Width, window.Height);
    }

    private static void ShowFallbackOpenError(string dialogKey)
    {
        AppDialogPresenter.ShowError(
            "Unable to Open Window",
            $"The requested window could not be opened.\n\n{dialogKey}\n\nThe error has been logged.");
    }
}
