using System.Windows;
using Microsoft.Extensions.DependencyInjection;

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
    IWindowSizingService sizingService) : IWindowRegistry
{
    private readonly Dictionary<string, Type> _dialogMap = [];

    public void RegisterDialog<TWindow>(string dialogKey) where TWindow : Window
    {
        _dialogMap[dialogKey] = typeof(TWindow);
    }

    public bool? ShowDialog(string dialogKey)
    {
        if (!_dialogMap.TryGetValue(dialogKey, out var windowType))
            throw new InvalidOperationException($"No dialog registered for key '{dialogKey}'.");

        var window = (Window)serviceProvider.GetRequiredService(windowType);
        ApplySizingIfNeeded(window);
        return window.ShowDialog();
    }

    public bool? ShowDialog<TWindow>() where TWindow : Window
    {
        var window = serviceProvider.GetRequiredService<TWindow>();
        ApplySizingIfNeeded(window);
        return window.ShowDialog();
    }

    private void ApplySizingIfNeeded(Window window)
    {
        if (window is BaseDialogWindow)
            return;

        sizingService.ConfigureDialogWindow(window, window.Width, window.Height);
    }
}
