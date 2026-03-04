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
    public DbSet<CategoryType> CategoryTypes => Set<CategoryType>();
    public DbSet<Colour> Colours => Set<Colour>();
    public DbSet<ProductPattern> ProductPatterns => Set<ProductPattern>();
    public DbSet<ProductSize> ProductSizes => Set<ProductSize>();
    public DbSet<ProductVariantType> ProductVariantTypes => Set<ProductVariantType>();
    public DbSet<InwardProduct> InwardProducts => Set<InwardProduct>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
    public DbSet<TaxGroup> TaxGroups => Set<TaxGroup>();
    public DbSet<TaxSlab> TaxSlabs => Set<TaxSlab>();
    public DbSet<HSNCode> HSNCodes => Set<HSNCode>();
    public DbSet<ProductTaxMapping> ProductTaxMappings => Set<ProductTaxMapping>();

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
            entity.HasOne(p => p.Tax)
                  .WithMany()
                  .HasForeignKey(p => p.TaxId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(p => p.Brand)
                  .WithMany()
                  .HasForeignKey(p => p.BrandId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(p => p.Category)
                  .WithMany()
                  .HasForeignKey(p => p.CategoryId)
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
            entity.Property(t => t.SlabPercent).HasColumnType("decimal(5,2)");
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
                  .WithMany()
                  .HasForeignKey(i => i.TaxMasterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(i => new { i.TaxProfileId, i.TaxMasterId }).IsUnique();
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasIndex(b => b.Name).IsUnique();
        });

        modelBuilder.Entity<CategoryType>(entity =>
        {
            entity.HasIndex(ct => ct.Name).IsUnique();
            entity.HasMany(ct => ct.Categories)
                  .WithOne(c => c.CategoryType)
                  .HasForeignKey(c => c.CategoryTypeId)
                  .OnDelete(DeleteBehavior.SetNull);
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

        // ── Colour (predefined palette — unique name) ──────────────
        modelBuilder.Entity<Colour>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasIndex(c => c.SortOrder);
        });

        // ── Product attributes (manual entry — unique names) ───────
        modelBuilder.Entity<ProductPattern>(entity =>
        {
            entity.HasIndex(p => p.Name).IsUnique();
        });

        modelBuilder.Entity<ProductSize>(entity =>
        {
            entity.HasIndex(s => s.Name).IsUnique();
            entity.HasIndex(s => s.SortOrder);
        });

        modelBuilder.Entity<ProductVariantType>(entity =>
        {
            entity.HasIndex(t => t.Name).IsUnique();
        });

        // ── Vendor (GSTIN + PAN indexes) ───────────────────────────
        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.HasIndex(v => v.Name);
            entity.HasIndex(v => v.GSTIN).HasFilter("[GSTIN] IS NOT NULL");
            entity.HasIndex(v => v.PAN).HasFilter("[PAN] IS NOT NULL");
            entity.Property(v => v.CreditLimit).HasColumnType("decimal(18,2)");
            entity.Property(v => v.OpeningBalance).HasColumnType("decimal(18,2)");
        });

        // ── Inward Entry ───────────────────────────────────────────
        modelBuilder.Entity<InwardEntry>(entity =>
        {
            entity.HasIndex(e => e.InwardNumber).IsUnique();
            entity.HasIndex(e => e.InwardDate);
            entity.Property(e => e.TransportCharges).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Vendor)
                  .WithMany()
                  .HasForeignKey(e => e.VendorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Inward Parcel ──────────────────────────────────────────
        modelBuilder.Entity<InwardParcel>(entity =>
        {
            entity.HasIndex(p => p.ParcelNumber);
            entity.Property(p => p.TransportCharge).HasColumnType("decimal(18,2)");
            entity.HasOne(p => p.InwardEntry)
                  .WithMany(e => e.Parcels)
                  .HasForeignKey(p => p.InwardEntryId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.Vendor)
                  .WithMany()
                  .HasForeignKey(p => p.VendorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Inward Product (parcel line items with attributes) ─────
        modelBuilder.Entity<InwardProduct>(entity =>
        {
            entity.Property(ip => ip.Quantity).HasColumnType("decimal(18,3)");
            entity.HasOne(ip => ip.InwardParcel)
                  .WithMany(p => p.Products)
                  .HasForeignKey(ip => ip.InwardParcelId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ip => ip.Product)
                  .WithMany()
                  .HasForeignKey(ip => ip.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(ip => ip.Colour)
                  .WithMany()
                  .HasForeignKey(ip => ip.ColourId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(ip => ip.Size)
                  .WithMany()
                  .HasForeignKey(ip => ip.SizeId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(ip => ip.Pattern)
                  .WithMany()
                  .HasForeignKey(ip => ip.PatternId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(ip => ip.VariantType)
                  .WithMany()
                  .HasForeignKey(ip => ip.VariantTypeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Product (add ProductType index) ────────────────────────
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.ProductType);
        });

        // ── Tax Group ──────────────────────────────────────────────
        modelBuilder.Entity<TaxGroup>(entity =>
        {
            entity.HasIndex(g => g.Name).IsUnique();
            entity.HasIndex(g => g.IsActive);
        });

        // ── Tax Slab (price-based GST within a group) ──────────────
        modelBuilder.Entity<TaxSlab>(entity =>
        {
            entity.Property(s => s.GSTPercent).HasColumnType("decimal(5,2)");
            entity.Property(s => s.CGSTPercent).HasColumnType("decimal(5,2)");
            entity.Property(s => s.SGSTPercent).HasColumnType("decimal(5,2)");
            entity.Property(s => s.IGSTPercent).HasColumnType("decimal(5,2)");
            entity.Property(s => s.PriceFrom).HasColumnType("decimal(18,2)");
            entity.Property(s => s.PriceTo).HasColumnType("decimal(18,2)");

            entity.HasOne(s => s.TaxGroup)
                  .WithMany(g => g.Slabs)
                  .HasForeignKey(s => s.TaxGroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.TaxGroupId, s.PriceFrom, s.PriceTo });
            entity.HasIndex(s => s.EffectiveFrom);
        });

        // ── HSN Code master ────────────────────────────────────────
        modelBuilder.Entity<HSNCode>(entity =>
        {
            entity.HasIndex(h => h.Code).IsUnique();
            entity.HasIndex(h => h.Category);
        });

        // ── Product → Tax Group + HSN mapping ──────────────────────
        modelBuilder.Entity<ProductTaxMapping>(entity =>
        {
            entity.HasOne(m => m.Product)
                  .WithMany()
                  .HasForeignKey(m => m.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.TaxGroup)
                  .WithMany(g => g.ProductMappings)
                  .HasForeignKey(m => m.TaxGroupId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(m => m.HSNCode)
                  .WithMany(h => h.ProductMappings)
                  .HasForeignKey(m => m.HSNCodeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => m.ProductId).IsUnique();
        });
    }
}
