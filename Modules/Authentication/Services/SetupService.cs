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
        string firmName, string adminPin, string managerPin, string userPin, string masterPin)
    {
        using var _ = perf.BeginScope("SetupService.InitializeAppAsync");
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        if (await context.AppConfigs.AnyAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Application has already been initialized.");

        context.AppConfigs.Add(new AppConfig
        {
            FirmName = firmName,
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

        try
        {
            await context.SaveChangesAsync().ConfigureAwait(false);
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

        // ── Tax components (CGST + SGST pairs for each slab) ───────
        var cgst0  = new TaxMaster { TaxName = "CGST 0%",   TaxRate = 0m,   IsActive = true, CreatedDate = now };
        var sgst0  = new TaxMaster { TaxName = "SGST 0%",   TaxRate = 0m,   IsActive = true, CreatedDate = now };
        var cgst25 = new TaxMaster { TaxName = "CGST 2.5%", TaxRate = 2.5m, IsActive = true, CreatedDate = now };
        var sgst25 = new TaxMaster { TaxName = "SGST 2.5%", TaxRate = 2.5m, IsActive = true, CreatedDate = now };
        var cgst6  = new TaxMaster { TaxName = "CGST 6%",   TaxRate = 6m,   IsActive = true, CreatedDate = now };
        var sgst6  = new TaxMaster { TaxName = "SGST 6%",   TaxRate = 6m,   IsActive = true, CreatedDate = now };
        var cgst9  = new TaxMaster { TaxName = "CGST 9%",   TaxRate = 9m,   IsActive = true, CreatedDate = now };
        var sgst9  = new TaxMaster { TaxName = "SGST 9%",   TaxRate = 9m,   IsActive = true, CreatedDate = now };
        var cgst14 = new TaxMaster { TaxName = "CGST 14%",  TaxRate = 14m,  IsActive = true, CreatedDate = now };
        var sgst14 = new TaxMaster { TaxName = "SGST 14%",  TaxRate = 14m,  IsActive = true, CreatedDate = now };

        context.TaxMasters.AddRange(cgst0, sgst0, cgst25, sgst25, cgst6, sgst6, cgst9, sgst9, cgst14, sgst14);

        // ── Profiles (composite: CGST + SGST = GST slab) ──────────
        var gst0  = new TaxProfile { ProfileName = "GST 0% (Exempt)", IsActive = true, IsDefault = true,  CreatedDate = now };
        var gst5  = new TaxProfile { ProfileName = "GST 5%",          IsActive = true, IsDefault = false, CreatedDate = now };
        var gst12 = new TaxProfile { ProfileName = "GST 12%",         IsActive = true, IsDefault = false, CreatedDate = now };
        var gst18 = new TaxProfile { ProfileName = "GST 18%",         IsActive = true, IsDefault = false, CreatedDate = now };
        var gst28 = new TaxProfile { ProfileName = "GST 28%",         IsActive = true, IsDefault = false, CreatedDate = now };

        context.TaxProfiles.AddRange(gst0, gst5, gst12, gst18, gst28);

        // ── Link components → profiles ─────────────────────────────
        context.TaxProfileItems.AddRange(
            new TaxProfileItem { TaxProfile = gst0,  TaxMaster = cgst0 },
            new TaxProfileItem { TaxProfile = gst0,  TaxMaster = sgst0 },
            new TaxProfileItem { TaxProfile = gst5,  TaxMaster = cgst25 },
            new TaxProfileItem { TaxProfile = gst5,  TaxMaster = sgst25 },
            new TaxProfileItem { TaxProfile = gst12, TaxMaster = cgst6 },
            new TaxProfileItem { TaxProfile = gst12, TaxMaster = sgst6 },
            new TaxProfileItem { TaxProfile = gst18, TaxMaster = cgst9 },
            new TaxProfileItem { TaxProfile = gst18, TaxMaster = sgst9 },
            new TaxProfileItem { TaxProfile = gst28, TaxMaster = cgst14 },
            new TaxProfileItem { TaxProfile = gst28, TaxMaster = sgst14 });
    }
}
