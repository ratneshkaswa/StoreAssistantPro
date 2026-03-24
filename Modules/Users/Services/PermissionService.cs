using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Users.Services;

/// <summary>
/// EF Core implementation of permission management (#289).
/// Admin always has full access — only User role grants are stored.
/// </summary>
public class PermissionService(IDbContextFactory<AppDbContext> dbFactory) : IPermissionService
{
    private static readonly string[] FeatureKeys =
    [
        FeatureFlags.Products,
        FeatureFlags.Categories,
        FeatureFlags.Brands,
        FeatureFlags.Inventory,
        FeatureFlags.VendorManagement,
        FeatureFlags.Billing,
        FeatureFlags.SaleHistory,
        FeatureFlags.CashRegister,
        FeatureFlags.Customers,
        FeatureFlags.PurchaseOrders,
        FeatureFlags.InwardEntry,
        FeatureFlags.Expenses,
        FeatureFlags.Debtors,
        FeatureFlags.Orders,
        FeatureFlags.Ironing,
        FeatureFlags.Salaries,
        FeatureFlags.Branch,
        FeatureFlags.SalesPurchase,
        FeatureFlags.Payments,
        FeatureFlags.TaxManagement,
        FeatureFlags.Reports,
        FeatureFlags.Backup,
        FeatureFlags.Quotations,
        FeatureFlags.GRN,
        FeatureFlags.BarcodeLabels,
        FeatureFlags.FinancialYear,
        FeatureFlags.SystemSettings,
        FeatureFlags.UserManagement,
        FeatureFlags.FirmManagement
    ];

    public IReadOnlyList<string> GetAllFeatureKeys() => FeatureKeys;

    public async Task<bool> HasPermissionAsync(UserType userType, string featureKey, CancellationToken ct = default)
    {
        // Admin always has full access
        if (userType == UserType.Admin)
            return true;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entry = await db.PermissionEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.FeatureKey == featureKey, ct);

        // If no entry exists, default to allowed (opt-out model)
        return entry?.IsAllowed ?? true;
    }

    public async Task<IReadOnlyList<PermissionEntry>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.PermissionEntries
            .AsNoTracking()
            .OrderBy(p => p.FeatureKey)
            .ToListAsync(ct);
    }

    public async Task SaveAsync(IReadOnlyList<PermissionEntry> entries, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Full replace — remove all existing, insert new set
        var existing = await db.PermissionEntries.ToListAsync(ct);
        db.PermissionEntries.RemoveRange(existing);

        foreach (var entry in entries)
        {
            db.PermissionEntries.Add(new PermissionEntry
            {
                FeatureKey = entry.FeatureKey,
                IsAllowed = entry.IsAllowed
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
