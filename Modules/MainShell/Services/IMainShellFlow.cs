namespace StoreAssistantPro.Modules.MainShell.Services;

/// <summary>
/// Encapsulates the MainShell module's window lifecycle.
/// Creates and shows the main application window (non-modal, single instance).
/// </summary>
public interface IMainShellFlow
{
    /// <summary>
    /// Creates and shows the main application window.
    /// The window remains open for the lifetime of the application.
    /// </summary>
    Task ShowMainWindowAsync();
}
