using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();
    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<TaxMaster> TaxMasters => Set<TaxMaster>();
    public DbSet<TaxProfile> TaxProfiles => Set<TaxProfile>();
    public DbSet<TaxProfileItem> TaxProfileItems => Set<TaxProfileItem>();
    public DbSet<BillingSession> BillingSessions => Set<BillingSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserCredential>(entity =>
        {
            entity.HasIndex(c => c.UserType).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.SalePrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.HSNCode).HasMaxLength(8);
            entity.HasIndex(p => p.HSNCode);
            entity.HasOne(p => p.TaxProfile)
                  .WithMany()
                  .HasForeignKey(p => p.TaxProfileId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.Property(si => si.UnitPrice).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(s => s.DiscountValue).HasColumnType("decimal(18,2)");
            entity.Property(s => s.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(s => s.SaleDate);
            entity.HasIndex(s => s.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<TaxMaster>(entity =>
        {
            entity.Property(t => t.TaxRate).HasColumnType("decimal(5,2)");
            entity.HasIndex(t => t.TaxName).IsUnique();
            entity.HasIndex(t => t.IsActive);
        });

        modelBuilder.Entity<TaxProfile>(entity =>
        {
            entity.HasIndex(t => t.ProfileName).IsUnique();
            entity.HasIndex(t => t.IsActive);
        });

        modelBuilder.Entity<TaxProfileItem>(entity =>
        {
            entity.HasOne(i => i.TaxProfile)
                  .WithMany(p => p.Items)
                  .HasForeignKey(i => i.TaxProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.TaxMaster)
                  .WithMany(m => m.ProfileItems)
                  .HasForeignKey(i => i.TaxMasterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(i => new { i.TaxProfileId, i.TaxMasterId }).IsUnique();
        });

        modelBuilder.Entity<BillingSession>(entity =>
        {
            entity.HasIndex(b => b.SessionId).IsUnique();
            entity.HasIndex(b => new { b.UserId, b.IsActive });
            entity.HasIndex(b => b.IsActive);

            entity.HasOne(b => b.User)
                  .WithMany()
                  .HasForeignKey(b => b.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
