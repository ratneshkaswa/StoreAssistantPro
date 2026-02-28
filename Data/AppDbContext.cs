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
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<BillingSession> BillingSessions => Set<BillingSession>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<InwardEntry> InwardEntries => Set<InwardEntry>();
    public DbSet<InwardParcel> InwardParcels => Set<InwardParcel>();
    public DbSet<StockAdjustmentLog> StockAdjustmentLogs => Set<StockAdjustmentLog>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<State> States => Set<State>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<ExtraCharge> ExtraCharges => Set<ExtraCharge>();
    public DbSet<FinancialYear> FinancialYears => Set<FinancialYear>();
    public DbSet<StockAlert> StockAlerts => Set<StockAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserCredential>(entity =>
        {
            entity.HasIndex(c => c.UserType).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.SalePrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.HSNCode).HasMaxLength(8);
            entity.Property(p => p.Barcode).HasMaxLength(50);
            entity.Property(p => p.UOM).HasMaxLength(20).HasDefaultValue("pcs");
            entity.HasIndex(p => p.HSNCode);
            entity.HasIndex(p => p.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
            entity.HasOne(p => p.TaxProfile)
                  .WithMany()
                  .HasForeignKey(p => p.TaxProfileId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(p => p.Brand)
                  .WithMany()
                  .HasForeignKey(p => p.BrandId)
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

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasIndex(b => b.Name).IsUnique();
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

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(s => s.Name);
            entity.HasIndex(s => s.GSTIN).HasFilter("[GSTIN] IS NOT NULL");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.Property(p => p.OrderNumber).HasMaxLength(30);
            entity.HasIndex(p => p.OrderNumber).IsUnique();
            entity.HasIndex(p => p.OrderDate);
            entity.HasOne(p => p.Supplier)
                  .WithMany()
                  .HasForeignKey(p => p.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.Property(i => i.UnitCost).HasColumnType("decimal(18,2)");
            entity.HasOne(i => i.PurchaseOrder)
                  .WithMany(p => p.Items)
                  .HasForeignKey(i => i.PurchaseOrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Product)
                  .WithMany()
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.Phone).HasFilter("[Phone] IS NOT NULL");
            entity.HasIndex(c => c.GSTIN).HasFilter("[GSTIN] IS NOT NULL");
            entity.Property(c => c.TotalPurchaseAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasIndex(s => s.StaffCode).IsUnique().HasFilter("[StaffCode] IS NOT NULL");
            entity.Property(s => s.NormalIncentiveRate).HasColumnType("decimal(5,2)");
            entity.Property(s => s.SpecialIncentiveRate).HasColumnType("decimal(5,2)");
            entity.Property(s => s.DailyTarget).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.DiscountValue).HasColumnType("decimal(18,2)");
            entity.Property(c => c.MinBillAmount).HasColumnType("decimal(18,2)");
            entity.Property(c => c.MaxDiscountAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasIndex(v => v.Code).IsUnique();
            entity.Property(v => v.FaceValue).HasColumnType("decimal(18,2)");
            entity.Property(v => v.Balance).HasColumnType("decimal(18,2)");
            entity.HasOne(v => v.Customer)
                  .WithMany()
                  .HasForeignKey(v => v.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SaleReturn>(entity =>
        {
            entity.HasIndex(r => r.ReturnNumber).IsUnique();
            entity.Property(r => r.RefundAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(r => r.Sale)
                  .WithMany(s => s.Returns)
                  .HasForeignKey(r => r.SaleId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.SaleItem)
                  .WithMany()
                  .HasForeignKey(r => r.SaleItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExtraCharge>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Sale)
                  .WithMany(s => s.ExtraCharges)
                  .HasForeignKey(e => e.SaleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FinancialYear>(entity =>
        {
            entity.HasIndex(f => f.Name).IsUnique();
            entity.HasIndex(f => f.IsCurrent);
        });

        modelBuilder.Entity<StockAlert>(entity =>
        {
            entity.HasIndex(a => a.ProductId).IsUnique();
            entity.HasOne(a => a.Product)
                  .WithMany()
                  .HasForeignKey(a => a.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasOne(s => s.Customer)
                  .WithMany()
                  .HasForeignKey(s => s.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(s => s.Staff)
                  .WithMany()
                  .HasForeignKey(s => s.StaffId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.Property(si => si.ItemDiscountRate).HasColumnType("decimal(5,2)");
            entity.HasOne(si => si.Staff)
                  .WithMany()
                  .HasForeignKey(si => si.StaffId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
