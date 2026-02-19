using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.Property(si => si.UnitPrice).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(s => s.SaleDate);
        });
    }
}
