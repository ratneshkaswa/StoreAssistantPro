using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Modules.Startup.Services;

namespace StoreAssistantPro.Modules.Startup.Workflows;

/// <summary>
/// Controls the application bootstrap sequence:
/// 1. Migrate database
/// 2. Auto-initialize if first run (default admin PIN)
/// 3. Load firm info
/// Outcome: app is ready for the Login workflow.
/// </summary>
public class StartupWorkflow(
    IStartupService startupService,
    ILogger<StartupWorkflow> logger) : IWorkflow
{
    public const string WorkflowName = "Startup";

    private static readonly WorkflowStep MigrateDb = new("MigrateDatabase");
    private static readonly WorkflowStep AutoInit = new("AutoInitialize");
    private static readonly WorkflowStep LoadFirm = new("LoadFirmInfo");
    private static readonly WorkflowStep LoadFeatures = new("LoadFeatureFlags");
    private static readonly WorkflowStep EnsureFY = new("EnsureFinancialYear");

    public string Name => WorkflowName;

    public IReadOnlyList<WorkflowStep> Steps { get; } =
        [MigrateDb, AutoInit, LoadFirm, LoadFeatures, EnsureFY];

    public async Task<StepResult> ExecuteStepAsync(WorkflowStep step, WorkflowContext context)
    {
        logger.LogInformation("Startup step: {Step}", step.Key);

        return step.Key switch
        {
            "MigrateDatabase" => await MigrateDatabaseAsync(context),
            "AutoInitialize" => await AutoInitializeAsync(),
            "LoadFirmInfo" => await LoadFirmInfoAsync(),
            "LoadFeatureFlags" => LoadFeatureFlags(),
            "EnsureFinancialYear" => await EnsureFinancialYearAsync(),
            _ => StepResult.Continue
        };
    }

    private async Task<StepResult> MigrateDatabaseAsync(WorkflowContext context)
    {
        try
        {
            await startupService.MigrateDatabaseAsync();
            logger.LogInformation("Database migration completed");
            return StepResult.Continue;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed");
            context.Set("Error", ex.Message);
            return StepResult.Cancel;
        }
    }

    private async Task<StepResult> AutoInitializeAsync()
    {
        await startupService.AutoInitializeIfNeededAsync();
        logger.LogInformation("Auto-initialize check completed");
        return StepResult.Continue;
    }

    private async Task<StepResult> LoadFirmInfoAsync()
    {
        await startupService.LoadFirmInfoAsync();
        logger.LogInformation("Firm info loaded");
        return StepResult.Continue;
    }

    private StepResult LoadFeatureFlags()
    {
        startupService.LoadFeatureFlags();
        logger.LogInformation("Feature flags loaded");
        return StepResult.Continue;
    }

    private async Task<StepResult> EnsureFinancialYearAsync()
    {
        await startupService.EnsureFinancialYearAsync();
        return StepResult.Complete;
    }

    public Task OnCompletedAsync(WorkflowContext context) => Task.CompletedTask;

    public Task OnCancelledAsync(WorkflowContext context) => Task.CompletedTask;
}
