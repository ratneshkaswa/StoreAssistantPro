using System.Windows;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Provides loosely-coupled dialog window creation. Modules register their
/// windows by string key during DI setup; consumers open them by key
/// without taking a direct dependency on the window type.
/// </summary>
public interface IWindowRegistry
{
    void RegisterDialog<TWindow>(string dialogKey) where TWindow : Window;
    bool? ShowDialog(string dialogKey);
    bool? ShowDialog<TWindow>() where TWindow : Window;
}
