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

        try
        {
            var pending = (await context.Database.GetPendingMigrationsAsync(ct).ConfigureAwait(false)).ToList();

            if (pending.Count == 0)
            {
                logger.LogInformation("Database is up to date — no pending migrations");
                await EnsureLegacyColumnsAsync(context, ct).ConfigureAwait(false);
                return;
            }

            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                pending.Count, string.Join(", ", pending));

            await context.Database.MigrateAsync(ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("PendingModelChangesWarning", StringComparison.Ordinal))
        {
            logger.LogWarning(
                ex,
                "Skipping EF migration apply due to pending model-change warning and continuing with compatibility patches");
        }

        await EnsureLegacyColumnsAsync(context, ct).ConfigureAwait(false);

        logger.LogInformation("All migrations applied successfully");
    }

    private async Task EnsureLegacyColumnsAsync(AppDbContext context, CancellationToken ct)
    {
        // Batch all column patches into a single SQL round-trip instead of 28 individual calls.
        const string batchedSql = """
            IF COL_LENGTH('AppConfigs','BackupEncryptionEnabled') IS NULL ALTER TABLE [AppConfigs] ADD [BackupEncryptionEnabled] bit NOT NULL CONSTRAINT [DF_AppConfigs_BackupEncryptionEnabled] DEFAULT(0);
            IF COL_LENGTH('AppConfigs','BackupIntervalHours') IS NULL ALTER TABLE [AppConfigs] ADD [BackupIntervalHours] int NOT NULL CONSTRAINT [DF_AppConfigs_BackupIntervalHours] DEFAULT(0);
            IF COL_LENGTH('AppConfigs','BackupRetentionDays') IS NULL ALTER TABLE [AppConfigs] ADD [BackupRetentionDays] int NOT NULL CONSTRAINT [DF_AppConfigs_BackupRetentionDays] DEFAULT(0);
            IF COL_LENGTH('AppConfigs','BarcodeFormat') IS NULL ALTER TABLE [AppConfigs] ADD [BarcodeFormat] nvarchar(10) NOT NULL CONSTRAINT [DF_AppConfigs_BarcodeFormat] DEFAULT(N'EAN13');
            IF COL_LENGTH('AppConfigs','ClosingTime') IS NULL ALTER TABLE [AppConfigs] ADD [ClosingTime] nvarchar(5) NOT NULL CONSTRAINT [DF_AppConfigs_ClosingTime] DEFAULT(N'21:00');
            IF COL_LENGTH('AppConfigs','DashboardWidgetLayout') IS NULL ALTER TABLE [AppConfigs] ADD [DashboardWidgetLayout] nvarchar(4000) NOT NULL CONSTRAINT [DF_AppConfigs_DashboardWidgetLayout] DEFAULT(N'');
            IF COL_LENGTH('AppConfigs','DecimalPlaces') IS NULL ALTER TABLE [AppConfigs] ADD [DecimalPlaces] int NOT NULL CONSTRAINT [DF_AppConfigs_DecimalPlaces] DEFAULT(2);
            IF COL_LENGTH('AppConfigs','ExpenseApprovalThreshold') IS NULL ALTER TABLE [AppConfigs] ADD [ExpenseApprovalThreshold] decimal(18,2) NOT NULL CONSTRAINT [DF_AppConfigs_ExpenseApprovalThreshold] DEFAULT(0);
            IF COL_LENGTH('AppConfigs','FontScalePercent') IS NULL ALTER TABLE [AppConfigs] ADD [FontScalePercent] int NOT NULL CONSTRAINT [DF_AppConfigs_FontScalePercent] DEFAULT(100);
            IF COL_LENGTH('AppConfigs','GoldTierThreshold') IS NULL ALTER TABLE [AppConfigs] ADD [GoldTierThreshold] decimal(18,2) NOT NULL CONSTRAINT [DF_AppConfigs_GoldTierThreshold] DEFAULT(50000);
            IF COL_LENGTH('AppConfigs','HeldBillTimeoutMinutes') IS NULL ALTER TABLE [AppConfigs] ADD [HeldBillTimeoutMinutes] int NOT NULL CONSTRAINT [DF_AppConfigs_HeldBillTimeoutMinutes] DEFAULT(120);
            IF COL_LENGTH('AppConfigs','IsStockFrozen') IS NULL ALTER TABLE [AppConfigs] ADD [IsStockFrozen] bit NOT NULL CONSTRAINT [DF_AppConfigs_IsStockFrozen] DEFAULT(0);
            IF COL_LENGTH('AppConfigs','LabelPaperSize') IS NULL ALTER TABLE [AppConfigs] ADD [LabelPaperSize] nvarchar(20) NOT NULL CONSTRAINT [DF_AppConfigs_LabelPaperSize] DEFAULT(N'65up');
            IF COL_LENGTH('AppConfigs','LastUpdateCheck') IS NULL ALTER TABLE [AppConfigs] ADD [LastUpdateCheck] datetime2 NULL;
            IF COL_LENGTH('AppConfigs','LicenseKey') IS NULL ALTER TABLE [AppConfigs] ADD [LicenseKey] nvarchar(100) NULL;
            IF COL_LENGTH('AppConfigs','LoyaltyPointsRate') IS NULL ALTER TABLE [AppConfigs] ADD [LoyaltyPointsRate] int NOT NULL CONSTRAINT [DF_AppConfigs_LoyaltyPointsRate] DEFAULT(1);
            IF COL_LENGTH('AppConfigs','MaxHeldBillsPerUser') IS NULL ALTER TABLE [AppConfigs] ADD [MaxHeldBillsPerUser] int NOT NULL CONSTRAINT [DF_AppConfigs_MaxHeldBillsPerUser] DEFAULT(0);
            IF COL_LENGTH('AppConfigs','MonthlySalesTarget') IS NULL ALTER TABLE [AppConfigs] ADD [MonthlySalesTarget] decimal(18,2) NOT NULL CONSTRAINT [DF_AppConfigs_MonthlySalesTarget] DEFAULT(0);
            IF COL_LENGTH('AppConfigs','OpeningTime') IS NULL ALTER TABLE [AppConfigs] ADD [OpeningTime] nvarchar(5) NOT NULL CONSTRAINT [DF_AppConfigs_OpeningTime] DEFAULT(N'09:00');
            IF COL_LENGTH('AppConfigs','PlatinumTierThreshold') IS NULL ALTER TABLE [AppConfigs] ADD [PlatinumTierThreshold] decimal(18,2) NOT NULL CONSTRAINT [DF_AppConfigs_PlatinumTierThreshold] DEFAULT(200000);
            IF COL_LENGTH('AppConfigs','QuotationTermsAndConditions') IS NULL ALTER TABLE [AppConfigs] ADD [QuotationTermsAndConditions] nvarchar(2000) NOT NULL CONSTRAINT [DF_AppConfigs_QuotationTermsAndConditions] DEFAULT(N'');
            IF COL_LENGTH('AppConfigs','SilverTierThreshold') IS NULL ALTER TABLE [AppConfigs] ADD [SilverTierThreshold] decimal(18,2) NOT NULL CONSTRAINT [DF_AppConfigs_SilverTierThreshold] DEFAULT(10000);
            IF COL_LENGTH('AppConfigs','SoundEffectsEnabled') IS NULL ALTER TABLE [AppConfigs] ADD [SoundEffectsEnabled] bit NOT NULL CONSTRAINT [DF_AppConfigs_SoundEffectsEnabled] DEFAULT(1);
            IF COL_LENGTH('AppConfigs','StartupMode') IS NULL ALTER TABLE [AppConfigs] ADD [StartupMode] nvarchar(20) NOT NULL CONSTRAINT [DF_AppConfigs_StartupMode] DEFAULT(N'Management');
            IF COL_LENGTH('AppConfigs','StockAdjustmentApprovalThreshold') IS NULL ALTER TABLE [AppConfigs] ADD [StockAdjustmentApprovalThreshold] int NOT NULL CONSTRAINT [DF_AppConfigs_StockAdjustmentApprovalThreshold] DEFAULT(0);
            IF COL_LENGTH('AppConfigs','ThemeMode') IS NULL ALTER TABLE [AppConfigs] ADD [ThemeMode] nvarchar(10) NOT NULL CONSTRAINT [DF_AppConfigs_ThemeMode] DEFAULT(N'Light');
            IF COL_LENGTH('UserCredentials','DisplayName') IS NULL ALTER TABLE [UserCredentials] ADD [DisplayName] nvarchar(100) NULL;
            IF COL_LENGTH('UserCredentials','Email') IS NULL ALTER TABLE [UserCredentials] ADD [Email] nvarchar(100) NULL;
            IF COL_LENGTH('UserCredentials','Phone') IS NULL ALTER TABLE [UserCredentials] ADD [Phone] nvarchar(15) NULL;
            """;

        await context.Database.ExecuteSqlRawAsync(batchedSql, ct).ConfigureAwait(false);
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
            appConfig.LogoPath = string.Empty;
            appConfig.BankName = string.Empty;
            appConfig.BankAccountNumber = string.Empty;
            appConfig.BankIFSC = string.Empty;
            appConfig.ReceiptHeaderText = string.Empty;
            appConfig.InvoiceResetPeriod = "Never";
            appConfig.HeldBillTimeoutMinutes = 120;
            appConfig.MaxHeldBillsPerUser = 0;
            appConfig.DecimalPlaces = 2;
            appConfig.OpeningTime = "09:00";
            appConfig.ClosingTime = "21:00";
            appConfig.QuotationTermsAndConditions = string.Empty;
            appConfig.LoyaltyPointsRate = 1;
            appConfig.SilverTierThreshold = 10_000m;
            appConfig.GoldTierThreshold = 50_000m;
            appConfig.PlatinumTierThreshold = 200_000m;
            appConfig.IsStockFrozen = false;
            appConfig.StockAdjustmentApprovalThreshold = 0;
            appConfig.MonthlySalesTarget = 0;
            appConfig.ExpenseApprovalThreshold = 0;
            appConfig.StartupMode = "Management";
            appConfig.SoundEffectsEnabled = true;
            appConfig.ThemeMode = "Light";
            appConfig.FontScalePercent = 100;
            appConfig.BarcodeFormat = "EAN13";
            appConfig.LabelPaperSize = "65up";
            appConfig.BackupIntervalHours = 0;
            appConfig.BackupEncryptionEnabled = false;
            appConfig.BackupRetentionDays = 0;
            appConfig.DashboardWidgetLayout = string.Empty;
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
        var settings = await context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        appState.SetFirmInfo(config?.FirmName ?? string.Empty);
        appState.SetDefaultPinFlag(config?.IsDefaultAdminPin ?? false);
        appState.SetInitialSetupPending(!(settings?.SetupCompleted ?? false));
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
