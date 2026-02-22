using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Tax.Commands;

public class SaveTaxProfileHandler(
    IDbContextFactory<AppDbContext> contextFactory)
    : BaseCommandHandler<SaveTaxProfileCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(SaveTaxProfileCommand command)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var halfRate = command.TaxRate / 2m;

        if (command.ExistingId is { } id)
        {
            var profile = await context.TaxProfiles
                .Include(p => p.Items).ThenInclude(i => i.TaxMaster)
                .FirstOrDefaultAsync(p => p.Id == id)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Tax profile not found.");

            profile.ProfileName = command.ProfileName;
            profile.IsActive = command.IsActive;

            // Remove old components and re-create
            context.TaxProfileItems.RemoveRange(profile.Items);
            var oldMasters = profile.Items.Select(i => i.TaxMaster!).ToList();
            context.TaxMasters.RemoveRange(oldMasters);

            AddComponents(context, profile, command.TaxRate, halfRate);
        }
        else
        {
            var profile = new TaxProfile
            {
                ProfileName = command.ProfileName,
                IsActive = command.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            context.TaxProfiles.Add(profile);
            AddComponents(context, profile, command.TaxRate, halfRate);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        return CommandResult.Success();
    }

    private static void AddComponents(AppDbContext context, TaxProfile profile,
        decimal totalRate, decimal halfRate)
    {
        var cgst = new TaxMaster
        {
            TaxName = $"CGST {halfRate}%",
            TaxRate = halfRate,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
        var sgst = new TaxMaster
        {
            TaxName = $"SGST {halfRate}%",
            TaxRate = halfRate,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        context.TaxMasters.AddRange(cgst, sgst);
        context.TaxProfileItems.AddRange(
            new TaxProfileItem { TaxProfile = profile, TaxMaster = cgst },
            new TaxProfileItem { TaxProfile = profile, TaxMaster = sgst });
    }
}
