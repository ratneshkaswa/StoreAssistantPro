using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Startup.Services;

public class StartupService(
    IDbContextFactory<AppDbContext> contextFactory,
    IAppStateService appState,
    IConfiguration configuration,
    IFeatureToggleService featureToggle,
    ITransactionHelper transactionHelper,
    IRegionalSettingsService regionalSettings,
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
        var config = await context.AppConfigs
            .AsNoTracking()
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return config?.IsInitialized == true;
    }

    public async Task AutoInitializeIfNeededAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("StartupService.AutoInitializeIfNeededAsync");

        await using var checkCtx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await checkCtx.AppConfigs.AsNoTracking().SingleOrDefaultAsync(ct).ConfigureAwait(false);
        if (existing?.IsInitialized == true)
            return;

        logger.LogInformation("First run detected — auto-initializing with defaults");

        await transactionHelper.ExecuteInTransactionAsync(async context =>
        {
            var appConfig = await context.AppConfigs.SingleOrDefaultAsync(ct).ConfigureAwait(false);
            appConfig ??= new AppConfig();
            appConfig.FirmName = "My Store";
            appConfig.Address = string.Empty;
            appConfig.State = string.Empty;
            appConfig.Pincode = string.Empty;
            appConfig.Phone = string.Empty;
            appConfig.Email = string.Empty;
            appConfig.NumberFormat = "Indian";
            appConfig.CurrencyCode = "INR";
            appConfig.CurrencySymbol = "\u20b9";
            appConfig.DateFormat = "dd/MM/yyyy";
            appConfig.FinancialYearStartMonth = 4;
            appConfig.FinancialYearEndMonth = 3;
            appConfig.GstRegistrationType = "Regular";
            appConfig.InvoicePrefix = "INV";
            appConfig.ReceiptFooterText = "Thank you! Visit again!";
            appConfig.MasterPinHash = PinHasher.Hash("123456");
            appConfig.IsDefaultAdminPin = true;
            appConfig.IsInitialized = true;

            if (appConfig.Id == 0)
                context.AppConfigs.Add(appConfig);

            // Seed admin credential with default PIN "1234"
            var adminCred = context.UserCredentials.FirstOrDefault(u => u.UserType == UserType.Admin);
            if (adminCred is null)
            {
                context.UserCredentials.Add(new UserCredential
                {
                    UserType = UserType.Admin,
                    PinHash = PinHasher.Hash("1234")
                });
            }

            var istNow = regionalSettings.Now;

            if (!context.TaxMasters.Any())
            {
                context.TaxMasters.AddRange(
                    new TaxMaster { TaxName = "GST 0%", SlabPercent = 0m, IsActive = true, CreatedDate = istNow },
                    new TaxMaster { TaxName = "GST 5%", SlabPercent = 5m, IsActive = true, CreatedDate = istNow },
                    new TaxMaster { TaxName = "GST 12%", SlabPercent = 12m, IsActive = true, CreatedDate = istNow },
                    new TaxMaster { TaxName = "GST 18%", SlabPercent = 18m, IsActive = true, CreatedDate = istNow },
                    new TaxMaster { TaxName = "GST 28%", SlabPercent = 28m, IsActive = true, CreatedDate = istNow });
            }

            if (!context.Colours.Any())
                context.Colours.AddRange(ColourSeedData.GetAll());

            var settings = context.SystemSettings.FirstOrDefault();
            if (settings is null)
            {
                settings = new SystemSettings();
                context.SystemSettings.Add(settings);
            }

            if (!context.FinancialYears.Any())
            {
                var now = istNow;
                var startYear = now.Month >= 4 ? now.Year : now.Year - 1;
                context.FinancialYears.Add(new FinancialYear
                {
                    Name = $"{startYear}-{(startYear + 1) % 100:D2}",
                    StartDate = new DateTime(startYear, 4, 1),
                    EndDate = new DateTime(startYear + 1, 3, 31),
                    IsCurrent = true
                });
            }
        }).ConfigureAwait(false);

        logger.LogInformation("Auto-initialization complete (admin PIN: 1234, master PIN: 123456)");
    }

    public async Task LoadFirmInfoAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var config = await context.AppConfigs
            .AsNoTracking()
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);

        appState.SetFirmInfo(config?.FirmName ?? string.Empty);
        appState.SetDefaultPinFlag(config?.IsDefaultAdminPin ?? false);
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
