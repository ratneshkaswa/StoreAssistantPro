using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Commands;

namespace StoreAssistantPro.Modules.Authentication.Services;

public class SetupService(
    IDbContextFactory<AppDbContext> contextFactory,
    ITransactionHelper transactionHelper,
    IRegionalSettingsService regionalSettings,
    IPerformanceMonitor perf) : ISetupService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory = contextFactory;

    public async Task InitializeAppAsync(CompleteFirstSetupCommand command, CancellationToken ct = default)
    {
        using var perfScope = perf.BeginScope("SetupService.InitializeAppAsync");
        _ = _contextFactory;

        try
        {
            await transactionHelper.ExecuteInTransactionAsync(async context =>
            {
                var appConfig = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
                if (appConfig?.IsInitialized == true)
                    throw new InvalidOperationException("Application has already been initialized.");

                appConfig ??= new AppConfig();
                appConfig.FirmName = command.FirmName;
                appConfig.Address = string.IsNullOrWhiteSpace(command.Address) ? string.Empty : command.Address;
                appConfig.State = string.IsNullOrWhiteSpace(command.State) ? string.Empty : command.State;
                appConfig.Pincode = string.IsNullOrWhiteSpace(command.Pincode) ? string.Empty : command.Pincode;
                appConfig.Phone = string.IsNullOrWhiteSpace(command.Phone) ? string.Empty : command.Phone;
                appConfig.Email = string.IsNullOrWhiteSpace(command.Email) ? string.Empty : command.Email;
                appConfig.GSTNumber = string.IsNullOrWhiteSpace(command.GSTIN) ? null : command.GSTIN;
                appConfig.PANNumber = string.IsNullOrWhiteSpace(command.PAN) ? null : command.PAN;
                appConfig.GstRegistrationType = string.IsNullOrWhiteSpace(command.BusinessOptions.GstRegistrationType) ? "Regular" : command.BusinessOptions.GstRegistrationType;
                appConfig.StateCode = string.IsNullOrWhiteSpace(command.BusinessOptions.StateCode) ? null : command.BusinessOptions.StateCode;
                appConfig.CompositionSchemeRate = command.BusinessOptions.CompositionSchemeRate;
                appConfig.CurrencyCode = string.IsNullOrWhiteSpace(command.CurrencyCode) ? "INR" : command.CurrencyCode;
                appConfig.CurrencySymbol = string.IsNullOrWhiteSpace(command.CurrencySymbol) ? "\u20b9" : command.CurrencySymbol;
                appConfig.FinancialYearStartMonth = command.FinancialYearStartMonth is >= 1 and <= 12 ? command.FinancialYearStartMonth : 4;
                appConfig.FinancialYearEndMonth = command.FinancialYearEndMonth is >= 1 and <= 12 ? command.FinancialYearEndMonth : 3;
                appConfig.DateFormat = string.IsNullOrWhiteSpace(command.DateFormat) ? "dd/MM/yyyy" : command.DateFormat;
                appConfig.NumberFormat = "Indian";
                appConfig.IsInitialized = true;
                appConfig.MasterPinHash = PinHasher.Hash(command.MasterPin);

                if (appConfig.Id == 0)
                    context.AppConfigs.Add(appConfig);

                UpsertUserCredential(context, UserType.Admin, command.AdminPin);
                UpsertUserCredential(context, UserType.Manager, command.ManagerPin);
                UpsertUserCredential(context, UserType.User, command.UserPin);

                var istNow = regionalSettings.Now;

                if (!context.TaxMasters.Any())
                    SeedDefaultTaxSlabs(context, istNow);

                if (!context.Colours.Any())
                    SeedColours(context);

                SeedSystemSettings(context, command.BusinessOptions);

                if (!context.FinancialYears.Any())
                    SeedFinancialYear(context, command.FinancialYearStartMonth, istNow);
            }).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException(
                "Setup could not be completed. Another instance may have initialized the application. Please restart.");
        }

        regionalSettings.UpdateSettings(command.CurrencySymbol, command.DateFormat);
    }

    /// <summary>
    /// Seeds Indian GST tax slabs.
    /// Called once during first-time setup within the same transaction.
    /// </summary>
    private static void SeedDefaultTaxSlabs(AppDbContext context, DateTime istNow)
    {
        context.TaxMasters.AddRange(
            new TaxMaster { TaxName = "GST 0%", SlabPercent = 0m, IsActive = true, CreatedDate = istNow },
            new TaxMaster { TaxName = "GST 5%", SlabPercent = 5m, IsActive = true, CreatedDate = istNow },
            new TaxMaster { TaxName = "GST 12%", SlabPercent = 12m, IsActive = true, CreatedDate = istNow },
            new TaxMaster { TaxName = "GST 18%", SlabPercent = 18m, IsActive = true, CreatedDate = istNow },
            new TaxMaster { TaxName = "GST 28%", SlabPercent = 28m, IsActive = true, CreatedDate = istNow });
    }

    /// <summary>
    /// Seeds the predefined 100-colour palette. Users cannot add colours
    /// outside this list - it is the single source of truth.
    /// </summary>
    private static void SeedColours(AppDbContext context)
    {
        context.Colours.AddRange(ColourSeedData.GetAll());
    }

    private static void UpsertUserCredential(AppDbContext context, UserType userType, string pin)
    {
        var credential = context.UserCredentials.FirstOrDefault(u => u.UserType == userType);
        if (credential is null)
        {
            context.UserCredentials.Add(new UserCredential
            {
                UserType = userType,
                PinHash = PinHasher.Hash(pin)
            });
            return;
        }

        credential.PinHash = PinHasher.Hash(pin);
    }

    /// <summary>
    /// Seeds the single SystemSettings row with safe defaults.
    /// </summary>
    private static void SeedSystemSettings(AppDbContext context, SetupBusinessOptions opts)
    {
        var settings = context.SystemSettings.FirstOrDefault();
        if (settings is null)
        {
            settings = new SystemSettings();
            context.SystemSettings.Add(settings);
        }

        settings.DefaultTaxMode = opts.DefaultTaxMode == "Tax-Inclusive (MRP)" ? "Inclusive" : "Exclusive";
        settings.RoundingMethod = opts.RoundingMethod switch
        {
            "Round to nearest \u20b91" => "NearestOne",
            "Round to nearest \u20b95" => "NearestFive",
            "Round to nearest \u20b910" => "NearestTen",
            _ => "None"
        };
        settings.NegativeStockAllowed = opts.NegativeStockAllowed;
        settings.NumberToWordsLanguage = opts.NumberToWordsLanguage;
        settings.AutoBackupEnabled = opts.AutoBackupEnabled;
        settings.BackupTime = opts.BackupTime;
        settings.BackupLocation = opts.BackupLocation;
    }

    /// <summary>
    /// Seeds the initial financial year using the user-selected start month.
    /// </summary>
    private static void SeedFinancialYear(AppDbContext context, int fyStartMonth, DateTime istNow)
    {
        var fyStart = istNow.Month >= fyStartMonth
            ? new DateTime(istNow.Year, fyStartMonth, 1)
            : new DateTime(istNow.Year - 1, fyStartMonth, 1);
        var fyEnd = fyStart.AddYears(1).AddDays(-1);

        context.FinancialYears.Add(new FinancialYear
        {
            Name = $"{fyStart.Year}-{fyEnd.Year % 100:D2}",
            StartDate = fyStart,
            EndDate = fyEnd,
            IsCurrent = true,
            BillingCounterResetDate = fyStart,
            CreatedDate = istNow
        });
    }
}


