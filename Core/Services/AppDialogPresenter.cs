using System.Linq;
using System.Threading;
using System.Windows;
using StoreAssistantPro.Core.Views;

namespace StoreAssistantPro.Core.Services;

public static class AppDialogPresenter
{
    private static readonly IWindowSizingService SizingService = new WindowSizingService();

    public static void ShowError(string title, string message, Window? owner = null)
    {
        ShowMessage(title, message, AppMessageDialogKind.Error, "OK", null, owner);
    }

    public static void ShowInfo(string title, string message, Window? owner = null)
    {
        ShowMessage(title, message, AppMessageDialogKind.Information, "OK", null, owner);
    }

    private static bool? ShowMessage(
        string title,
        string message,
        AppMessageDialogKind kind,
        string primaryButtonText,
        string? secondaryButtonText,
        Window? preferredOwner)
    {
        if (Application.Current is not Application app)
            return null;

        var dispatcher = app.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return null;

        if (!dispatcher.CheckAccess())
        {
            return dispatcher.Invoke(() => ShowMessage(
                title,
                message,
                kind,
                primaryButtonText,
                secondaryButtonText,
                preferredOwner));
        }

        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            return null;

        var dialog = new AppMessageDialog(
            SizingService,
            title,
            message,
            kind,
            primaryButtonText,
            secondaryButtonText);

        PrepareOwner(dialog, preferredOwner);
        return dialog.ShowDialog();
    }

    private static void PrepareOwner(Window dialog, Window? preferredOwner)
    {
        var owner = preferredOwner;

        if (owner is null || owner == dialog)
        {
            owner = Application.Current?
                .Windows
                .OfType<Window>()
                .LastOrDefault(window => window != dialog && window.IsVisible && window.IsActive);
        }

        if (owner is null || owner == dialog)
            return;

        dialog.Owner = owner;
        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
}
