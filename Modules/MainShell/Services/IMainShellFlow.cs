namespace StoreAssistantPro.Modules.MainShell.Services;

/// <summary>
/// Encapsulates the MainShell module's window lifecycle.
/// Consumers call <see cref="ShowMainWindow"/> without knowing
/// about <c>MainWindow</c> or <c>MainViewModel</c> types.
/// </summary>
public interface IMainShellFlow
{
    /// <summary>
    /// Shows the main application window (blocks until closed).
    /// Returns <c>true</c> if the user chose to log out,
    /// <c>false</c> if the window was closed normally.
    /// </summary>
    bool ShowMainWindow();
}
