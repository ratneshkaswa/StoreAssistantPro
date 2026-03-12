using System.Linq;
using System.Windows;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Views;

namespace StoreAssistantPro.Modules.MainShell.Services;

public class DialogService(
    IWindowRegistry windowRegistry,
    IWindowSizingService sizingService) : IDialogService
{
    public bool Confirm(string message, string title = "Confirm")
    {
        var dialog = new AppMessageDialog(
            sizingService,
            title,
            message,
            AppMessageDialogKind.Question,
            primaryButtonText: "Yes",
            secondaryButtonText: "No");

        PrepareOwner(dialog);
        return dialog.ShowDialog() == true && dialog.Confirmed;
    }

    public void ShowInfo(string message, string title = "Information")
    {
        var dialog = new AppMessageDialog(
            sizingService,
            title,
            message,
            AppMessageDialogKind.Information,
            primaryButtonText: "OK");

        PrepareOwner(dialog);
        dialog.ShowDialog();
    }

    public string? PromptPassword(string message, string title = "Authentication Required")
    {
        var dialog = new MasterPinDialog(sizingService, message) { Title = title };
        PrepareOwner(dialog);
        return dialog.ShowDialog() == true ? dialog.EnteredPin : null;
    }

    public bool? ShowDialog(string dialogKey) =>
        windowRegistry.ShowDialog(dialogKey);

    private static void PrepareOwner(Window dialog)
    {
        var owner = Application.Current?
            .Windows
            .OfType<Window>()
            .LastOrDefault(window => window.IsActive);

        if (owner is null || owner == dialog)
            return;

        dialog.Owner = owner;
        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
}
