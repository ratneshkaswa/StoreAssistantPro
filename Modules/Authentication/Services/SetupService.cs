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
        string firmName, string address, string phone,
        string email, string gstin, string currencyCode,
        string adminPin, string managerPin, string userPin, string masterPin,
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
            Phone = phone,
            Email = string.IsNullOrWhiteSpace(email) ? string.Empty : email,
            GSTNumber = string.IsNullOrWhiteSpace(gstin) ? null : gstin,
            CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "INR" : currencyCode,
            CurrencySymbol = "₹",
            FinancialYearStartMonth = 4,
            FinancialYearEndMonth = 3,
            DateFormat = "dd/MM/yyyy",
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
        SeedFinancialYear(context);

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
    /// Seeds the initial financial year based on the current date.
    /// Indian FY runs April 1 – March 31.
    /// </summary>
    private static void SeedFinancialYear(AppDbContext context)
    {
        var now = DateTime.UtcNow;
        var fyStart = now.Month >= 4
            ? new DateTime(now.Year, 4, 1)
            : new DateTime(now.Year - 1, 4, 1);
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
