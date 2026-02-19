using System.Windows;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Modules.MainShell.Services;

public class DialogService(IWindowRegistry windowRegistry) : IDialogService
{
    public bool Confirm(string message, string title = "Confirm")
    {
        var result = MessageBox.Show(
            message, title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    public bool? ShowDialog(string dialogKey) =>
        windowRegistry.ShowDialog(dialogKey);
}
