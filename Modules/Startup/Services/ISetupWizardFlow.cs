namespace StoreAssistantPro.Modules.Startup.Services;

/// <summary>
/// Orchestrates the first-run setup wizard window lifecycle.
/// Called from App.xaml.cs after login when SetupCompleted is false.
/// </summary>
public interface ISetupWizardFlow
{
    /// <summary>
    /// Shows the setup wizard. Returns <c>true</c> if completed, <c>false</c> if cancelled.
    /// </summary>
    bool RunSetupWizard();
}
