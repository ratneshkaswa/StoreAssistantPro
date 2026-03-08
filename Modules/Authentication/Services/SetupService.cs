using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Authentication.Services;

public class SetupService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : ISetupService
{
    public async Task InitializeAppAsync(
        string firmName, string address, string state, string pincode,
        string phone, string email, string gstin, string pan,
        string currencyCode, string currencySymbol,
        int financialYearStartMonth, int financialYearEndMonth,
        string dateFormat,
        string adminPin, string managerPin,
        string userPin, string masterPin,
        CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("SetupService.InitializeAppAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (await context.AppConfigs.AnyAsync(ct).ConfigureAwait(false))
            throw new InvalidOperationException("Application has already been initialized.");

        context.AppConfigs.Add(new AppConfig
        {
            FirmName = firmName,
            Address = address,
            State = string.IsNullOrWhiteSpace(state) ? string.Empty : state,
            Pincode = string.IsNullOrWhiteSpace(pincode) ? string.Empty : pincode,
            Phone = phone,
            Email = string.IsNullOrWhiteSpace(email) ? string.Empty : email,
            GSTNumber = string.IsNullOrWhiteSpace(gstin) ? null : gstin,
            PANNumber = string.IsNullOrWhiteSpace(pan) ? null : pan,
            CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "INR" : currencyCode,
            CurrencySymbol = string.IsNullOrWhiteSpace(currencySymbol) ? "₹" : currencySymbol,
            FinancialYearStartMonth = financialYearStartMonth is >= 1 and <= 12 ? financialYearStartMonth : 4,
            FinancialYearEndMonth = financialYearEndMonth is >= 1 and <= 12 ? financialYearEndMonth : 3,
            DateFormat = string.IsNullOrWhiteSpace(dateFormat) ? "dd/MM/yyyy" : dateFormat,
            NumberFormat = "Indian",
            IsInitialized = true,
            MasterPinHash = PinHasher.Hash(masterPin)
        });

        context.UserCredentials.Add(new UserCredential
        {
            UserType = UserType.Admin,
            PinHash = PinHasher.Hash(adminPin)
        });

        context.UserCredentials.Add(new UserCredential
        {
            UserType = UserType.Manager,
            PinHash = PinHasher.Hash(managerPin)
        });

        context.UserCredentials.Add(new UserCredential
        {
            UserType = UserType.User,
            PinHash = PinHasher.Hash(userPin)
        });

        SeedDefaultTaxSlabs(context);
        SeedColours(context);
        SeedSystemSettings(context);
        SeedFinancialYear(context, financialYearStartMonth);

        try
        {
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException(
                "Application was already initialized by another machine. Please restart the app.");
        }
    }

    /// <summary>
    /// Seeds Indian GST tax slabs and matching intra-state profiles.
    /// Called once during first-time setup within the same transaction.
    /// </summary>
    private static void SeedDefaultTaxSlabs(AppDbContext context)
    {
        var now = DateTime.UtcNow;

        context.TaxMasters.AddRange(
            new TaxMaster { TaxName = "GST 0%",  SlabPercent = 0m,  IsActive = true, CreatedDate = now },
            new TaxMaster { TaxName = "GST 5%",  SlabPercent = 5m,  IsActive = true, CreatedDate = now },
            new TaxMaster { TaxName = "GST 12%", SlabPercent = 12m, IsActive = true, CreatedDate = now },
            new TaxMaster { TaxName = "GST 18%", SlabPercent = 18m, IsActive = true, CreatedDate = now },
            new TaxMaster { TaxName = "GST 28%", SlabPercent = 28m, IsActive = true, CreatedDate = now });
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
    private static void SeedSystemSettings(AppDbContext context)
    {
        context.SystemSettings.Add(new SystemSettings
        {
            DefaultTaxMode = "Exclusive",
            AutoBackupEnabled = false
        });
    }

    /// <summary>
    /// Seeds the initial financial year using the user-selected start month.
    /// </summary>
    private static void SeedFinancialYear(AppDbContext context, int fyStartMonth)
    {
        var now = DateTime.UtcNow;
        var fyStart = now.Month >= fyStartMonth
            ? new DateTime(now.Year, fyStartMonth, 1)
            : new DateTime(now.Year - 1, fyStartMonth, 1);
        var fyEnd = fyStart.AddYears(1).AddDays(-1);

        context.FinancialYears.Add(new FinancialYear
        {
            Name = $"{fyStart.Year}–{fyEnd.Year % 100:D2}",
            StartDate = fyStart,
            EndDate = fyEnd,
            IsCurrent = true,
            BillingCounterResetDate = fyStart,
            CreatedDate = now
        });
    }
}
