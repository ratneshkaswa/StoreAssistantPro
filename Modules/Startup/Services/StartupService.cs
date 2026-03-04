using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Modules.Startup.Services;

public class StartupService(
    IDbContextFactory<AppDbContext> contextFactory,
    IAppStateService appState,
    IConfiguration configuration,
    IFeatureToggleService featureToggle,
    IPerformanceMonitor perf,
    ILogger<StartupService> logger) : IStartupService
{
    public async Task MigrateDatabaseAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("StartupService.MigrateDatabaseAsync", TimeSpan.FromSeconds(5));
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // 1. Apply migrations (creates the database if it doesn't exist)
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

        var pending = (await context.Database.GetPendingMigrationsAsync(ct).ConfigureAwait(false)).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("Database is up to date — no pending migrations");
            return;
        }

        logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
            pending.Count, string.Join(", ", pending));

        await context.Database.MigrateAsync(ct).ConfigureAwait(false);

        logger.LogInformation("All migrations applied successfully");
    }

    public async Task<bool> IsAppInitializedAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var config = await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync(ct).ConfigureAwait(false);
        return config?.IsInitialized == true;
    }

    public async Task LoadFirmInfoAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var config = await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync(ct).ConfigureAwait(false);
        appState.SetFirmInfo(config?.FirmName ?? string.Empty);
    }

    public void LoadFeatureFlags()
    {
        var section = configuration.GetSection("Features");
        var flags = section.GetChildren()
            .ToDictionary(
                c => c.Key,
                c => bool.TryParse(c.Value, out var v) && v,
                StringComparer.OrdinalIgnoreCase);

        featureToggle.Load(flags);
    }

    public Task EnsureFinancialYearAsync(CancellationToken ct = default)
    {
        // Financial year management not yet available — placeholder.
        logger.LogInformation("Financial year check skipped — module not loaded");
        return Task.CompletedTask;
    }
}
