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
    public async Task InitializeAppAsync(CompleteFirstSetupCommand command, CancellationToken ct = default)
    {
        using var perfScope = perf.BeginScope("SetupService.InitializeAppAsync");

        try
        {
            await transactionHelper.ExecuteInTransactionAsync(async context =>
            {
                if (await context.AppConfigs.AnyAsync(ct).ConfigureAwait(false))
                    throw new InvalidOperationException("Application has already been initialized.");

                context.AppConfigs.Add(new AppConfig
                {
                    FirmName = command.FirmName,
                    Address = string.IsNullOrWhiteSpace(command.Address) ? string.Empty : command.Address,
                    State = string.IsNullOrWhiteSpace(command.State) ? string.Empty : command.State,
                    Pincode = string.IsNullOrWhiteSpace(command.Pincode) ? string.Empty : command.Pincode,
                    Phone = string.IsNullOrWhiteSpace(command.Phone) ? string.Empty : command.Phone,
                    Email = string.IsNullOrWhiteSpace(command.Email) ? string.Empty : command.Email,
                    GSTNumber = string.IsNullOrWhiteSpace(command.GSTIN) ? null : command.GSTIN,
                    PANNumber = string.IsNullOrWhiteSpace(command.PAN) ? null : command.PAN,
                    GstRegistrationType = string.IsNullOrWhiteSpace(command.BusinessOptions.GstRegistrationType) ? "Regular" : command.BusinessOptions.GstRegistrationType,
                    StateCode = string.IsNullOrWhiteSpace(command.BusinessOptions.StateCode) ? null : command.BusinessOptions.StateCode,
                    CompositionSchemeRate = command.BusinessOptions.CompositionSchemeRate,
                    CurrencyCode = string.IsNullOrWhiteSpace(command.CurrencyCode) ? "INR" : command.CurrencyCode,
                    CurrencySymbol = string.IsNullOrWhiteSpace(command.CurrencySymbol) ? "\u20b9" : command.CurrencySymbol,
                    FinancialYearStartMonth = command.FinancialYearStartMonth is >= 1 and <= 12 ? command.FinancialYearStartMonth : 4,
                    FinancialYearEndMonth = command.FinancialYearEndMonth is >= 1 and <= 12 ? command.FinancialYearEndMonth : 3,
                    DateFormat = string.IsNullOrWhiteSpace(command.DateFormat) ? "dd/MM/yyyy" : command.DateFormat,
                    NumberFormat = "Indian",
                    IsInitialized = true,
                    MasterPinHash = PinHasher.Hash(command.MasterPin)
                });

                context.UserCredentials.Add(new UserCredential
                {
                    UserType = UserType.Admin,
                    PinHash = PinHasher.Hash(command.AdminPin)
                });

                context.UserCredentials.Add(new UserCredential
                {
                    UserType = UserType.Manager,
                    PinHash = PinHasher.Hash(command.ManagerPin)
                });

                context.UserCredentials.Add(new UserCredential
                {
                    UserType = UserType.User,
                    PinHash = PinHasher.Hash(command.UserPin)
                });

                var istNow = regionalSettings.Now;

                SeedDefaultTaxSlabs(context, istNow);
                SeedColours(context);
                SeedSystemSettings(context, command.BusinessOptions);
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
    /// outside this list — it is the single source of truth.
    /// </summary>
    private static void SeedColours(AppDbContext context)
    {
        context.Colours.AddRange(ColourSeedData.GetAll());
    }

    /// <summary>
    /// Seeds the single SystemSettings row with safe defaults.
    /// </summary>
    private static void SeedSystemSettings(AppDbContext context, SetupBusinessOptions opts)
    {
        context.SystemSettings.Add(new SystemSettings
        {
            DefaultTaxMode = opts.DefaultTaxMode == "Tax-Inclusive (MRP)" ? "Inclusive" : "Exclusive",
            RoundingMethod = opts.RoundingMethod switch
            {
                "Round to nearest \u20b91" => "NearestOne",
                "Round to nearest \u20b95" => "NearestFive",
                "Round to nearest \u20b910" => "NearestTen",
                _ => "None"
            },
            NegativeStockAllowed = opts.NegativeStockAllowed,
            NumberToWordsLanguage = opts.NumberToWordsLanguage,
            AutoBackupEnabled = opts.AutoBackupEnabled,
            BackupTime = opts.BackupTime,
            BackupLocation = opts.BackupLocation
        });
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
            Name = $"{fyStart.Year}–{fyEnd.Year % 100:D2}",
            StartDate = fyStart,
            EndDate = fyEnd,
            IsCurrent = true,
            BillingCounterResetDate = fyStart,
            CreatedDate = istNow
        });
    }
}
