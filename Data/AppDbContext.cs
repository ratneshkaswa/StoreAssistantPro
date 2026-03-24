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
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<InwardProduct> InwardProducts => Set<InwardProduct>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
    public DbSet<TaxGroup> TaxGroups => Set<TaxGroup>();
    public DbSet<TaxSlab> TaxSlabs => Set<TaxSlab>();
    public DbSet<HSNCode> HSNCodes => Set<HSNCode>();
    public DbSet<ProductTaxMapping> ProductTaxMappings => Set<ProductTaxMapping>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<PettyCashDeposit> PettyCashDeposits => Set<PettyCashDeposit>();
    public DbSet<Debtor> Debtors => Set<Debtor>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<IroningEntry> IroningEntries => Set<IroningEntry>();
    public DbSet<IroningBatch> IroningBatches => Set<IroningBatch>();
    public DbSet<IroningBatchItem> IroningBatchItems => Set<IroningBatchItem>();
    public DbSet<Cloth> Cloths => Set<Cloth>();
    public DbSet<Salary> Salaries => Set<Salary>();
    public DbSet<BranchBill> BranchBills => Set<BranchBill>();
    public DbSet<SalesPurchaseEntry> SalesPurchaseEntries => Set<SalesPurchaseEntry>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<HeldBill> HeldBills => Set<HeldBill>();
    public DbSet<HeldBillItem> HeldBillItems => Set<HeldBillItem>();
    public DbSet<StockTake> StockTakes => Set<StockTake>();
    public DbSet<StockTakeItem> StockTakeItems => Set<StockTakeItem>();
    public DbSet<VendorPayment> VendorPayments => Set<VendorPayment>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<GoodsReceivedNote> GoodsReceivedNotes => Set<GoodsReceivedNote>();
    public DbSet<GRNItem> GRNItems => Set<GRNItem>();
    public DbSet<PermissionEntry> PermissionEntries => Set<PermissionEntry>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
    public DbSet<ProductSupplier> ProductSuppliers => Set<ProductSupplier>();
    public DbSet<DiscountRule> DiscountRules => Set<DiscountRule>();
    public DbSet<RecurringExpense> RecurringExpenses => Set<RecurringExpense>();
    public DbSet<CashRegisterShift> CashRegisterShifts => Set<CashRegisterShift>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserCredential>(entity =>
        {
            entity.HasIndex(c => c.UserType).IsUnique();
        });

        modelBuilder.Entity<AppConfig>(entity =>
        {
            // Enforces exactly one logical AppConfig row.
            entity.Property(c => c.SingletonKey).HasDefaultValue(1);
            entity.HasIndex(c => c.SingletonKey).IsUnique();
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
            entity.HasOne(p => p.Vendor)
                  .WithMany()
                  .HasForeignKey(p => p.VendorId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(p => p.ProductType);
            entity.HasIndex(p => p.Name).IsUnique();
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(s => s.DiscountValue).HasColumnType("decimal(18,2)");
            entity.Property(s => s.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(s => s.SaleDate);
            entity.HasIndex(s => s.InvoiceNumber).IsUnique();
            entity.HasIndex(s => s.IdempotencyKey).IsUnique();
            entity.HasOne(s => s.Customer)
                  .WithMany()
                  .HasForeignKey(s => s.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(s => s.Staff)
                  .WithMany()
                  .HasForeignKey(s => s.StaffId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SalePayment>(entity =>
        {
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(p => p.Sale)
                  .WithMany(s => s.Payments)
                  .HasForeignKey(p => p.SaleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CashRegister>(entity =>
        {
            entity.Property(r => r.OpeningBalance).HasColumnType("decimal(18,2)");
            entity.Property(r => r.ClosingBalance).HasColumnType("decimal(18,2)");
            entity.Property(r => r.ExpectedBalance).HasColumnType("decimal(18,2)");
            entity.Property(r => r.Discrepancy).HasColumnType("decimal(18,2)");
            entity.HasIndex(r => r.OpenedAt);
        });

        modelBuilder.Entity<CashMovement>(entity =>
        {
            entity.Property(m => m.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(m => m.CashRegister)
                  .WithMany(r => r.Movements)
                  .HasForeignKey(m => m.CashRegisterId)
                  .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasIndex(c => c.IsActive);
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

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.Property(si => si.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(si => si.ItemDiscountRate).HasColumnType("decimal(5,2)");
            entity.HasOne(si => si.Staff)
                  .WithMany()
                  .HasForeignKey(si => si.StaffId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(si => si.ProductVariant)
                  .WithMany()
                  .HasForeignKey(si => si.ProductVariantId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(si => si.ProductVariantId);
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

        // ── ProductVariant (size+colour per product) ────────────────
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasOne(v => v.Product)
                  .WithMany()
                  .HasForeignKey(v => v.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.Size)
                  .WithMany()
                  .HasForeignKey(v => v.SizeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Colour)
                  .WithMany()
                  .HasForeignKey(v => v.ColourId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(v => v.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
            entity.HasIndex(v => new { v.ProductId, v.SizeId, v.ColourId }).IsUnique();
            entity.Property(v => v.AdditionalPrice).HasColumnType("decimal(18,2)");
        });

        // ── StockAdjustment (immutable audit log) ───────────────────
        modelBuilder.Entity<StockAdjustment>(entity =>
        {
            entity.HasOne(a => a.Product)
                  .WithMany()
                  .HasForeignKey(a => a.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.ProductVariant)
                  .WithMany()
                  .HasForeignKey(a => a.ProductVariantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.ProductId);
            entity.HasIndex(a => a.ProductVariantId);
            entity.HasIndex(a => a.Timestamp);
        });

        // ── Vendor (GSTIN + PAN indexes) ───────────────────────────
        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.HasIndex(v => v.Name).IsUnique();
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

        // ── State (Indian GST state master) ────────────────────────
        modelBuilder.Entity<State>(entity =>
        {
            entity.HasIndex(s => s.StateCode).IsUnique();
            entity.HasIndex(s => s.Name).IsUnique();
        });

        // ── PriceHistory (audit trail for price changes) ───────────
        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.Property(p => p.OldSalePrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.NewSalePrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.OldCostPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.NewCostPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.OldMRP).HasColumnType("decimal(18,2)");
            entity.Property(p => p.NewMRP).HasColumnType("decimal(18,2)");
            entity.HasOne(p => p.Product)
                  .WithMany()
                  .HasForeignKey(p => p.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(p => p.ProductId);
            entity.HasIndex(p => p.ChangedDate);
        });

        // ── StockAdjustmentLog (immutable audit trail) ─────────────
        modelBuilder.Entity<StockAdjustmentLog>(entity =>
        {
            entity.HasOne(l => l.Product)
                  .WithMany()
                  .HasForeignKey(l => l.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(l => l.ProductId);
            entity.HasIndex(l => l.AdjustedAt);
        });

        // ── Expense ────────────────────────────────────────────────
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.Category);
        });

        // ── ExpenseCategory (#227) ────────────────────────────────
        modelBuilder.Entity<ExpenseCategory>(entity =>
        {
            entity.HasIndex(ec => ec.Name).IsUnique();
        });

        // ── PettyCashDeposit ───────────────────────────────────────
        modelBuilder.Entity<PettyCashDeposit>(entity =>
        {
            entity.Property(d => d.Amount).HasColumnType("decimal(18,2)");
            entity.HasIndex(d => d.Date);
        });

        // ── Debtor ─────────────────────────────────────────────────
        modelBuilder.Entity<Debtor>(entity =>
        {
            entity.Property(d => d.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(d => d.PaidAmount).HasColumnType("decimal(18,2)");
            entity.Ignore(d => d.Balance);
            entity.Ignore(d => d.DaysAgo);
            entity.HasIndex(d => d.Date);
        });

        // ── Order ──────────────────────────────────────────────────
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.Rate).HasColumnType("decimal(18,2)");
            entity.Property(o => o.Amount).HasColumnType("decimal(18,2)");
            entity.Ignore(o => o.IsOverdue);
            entity.Ignore(o => o.EntryTimestamp);
            entity.HasIndex(o => o.Date);
            entity.HasIndex(o => o.Status);
        });

        // ── Payment ────────────────────────────────────────────────
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(p => p.Customer)
                  .WithMany()
                  .HasForeignKey(p => p.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(p => p.PaymentDate);
        });

        // ── IroningEntry ───────────────────────────────────────────
        modelBuilder.Entity<IroningEntry>(entity =>
        {
            entity.Property(e => e.Rate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.IsPaid);
        });

        // ── IroningBatch ───────────────────────────────────────────
        modelBuilder.Entity<IroningBatch>(entity =>
        {
            entity.Property(b => b.PaidAmount).HasColumnType("decimal(18,2)");
            entity.HasMany(b => b.Items)
                  .WithOne(i => i.Batch)
                  .HasForeignKey(i => i.IroningBatchId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(b => b.Date);
            entity.HasIndex(b => b.Status);
        });

        // ── IroningBatchItem ───────────────────────────────────────
        modelBuilder.Entity<IroningBatchItem>(entity =>
        {
            entity.Property(i => i.Rate).HasColumnType("decimal(18,2)");
            entity.Property(i => i.Amount).HasColumnType("decimal(18,2)");
            entity.HasIndex(i => i.IroningBatchId);
        });

        // ── Cloth ──────────────────────────────────────────────────
        modelBuilder.Entity<Cloth>(entity =>
        {
            entity.Property(c => c.Price).HasColumnType("decimal(18,2)");
        });

        // ── Salary ─────────────────────────────────────────────────
        modelBuilder.Entity<Salary>(entity =>
        {
            entity.Property(s => s.Amount).HasColumnType("decimal(18,2)");
            entity.Property(s => s.BaseSalary).HasColumnType("decimal(18,2)");
            entity.Property(s => s.Advance).HasColumnType("decimal(18,2)");
            entity.Property(s => s.HoursWorked).HasColumnType("decimal(18,2)");
            entity.Property(s => s.Incentive).HasColumnType("decimal(18,2)");
            entity.HasIndex(s => s.EmployeeName);
            entity.HasIndex(s => s.IsPaid);
        });

        // ── BranchBill ─────────────────────────────────────────────
        modelBuilder.Entity<BranchBill>(entity =>
        {
            entity.Property(b => b.Amount).HasColumnType("decimal(18,2)");
            entity.HasIndex(b => b.Type);
            entity.HasIndex(b => b.Date);
        });

        // ── SalesPurchaseEntry ─────────────────────────────────────
        modelBuilder.Entity<SalesPurchaseEntry>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Ignore(e => e.DisplayAmount);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Date);
        });

        // ── AuditLog (#291) ───────────────────────────────────────
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => a.Action);
            entity.HasIndex(a => a.EntityType);
            entity.HasIndex(a => a.Timestamp);
            entity.HasIndex(a => a.UserId);
        });

        // ── HeldBill (#336-#346) ──────────────────────────────────
        modelBuilder.Entity<HeldBill>(entity =>
        {
            entity.Property(h => h.Total).HasColumnType("decimal(18,2)");
            entity.HasIndex(h => h.IsActive);
            entity.HasIndex(h => h.HeldAt);
        });

        modelBuilder.Entity<HeldBillItem>(entity =>
        {
            entity.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(i => i.ItemDiscountRate).HasColumnType("decimal(5,2)");
            entity.Property(i => i.ItemDiscountAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(i => i.HeldBill)
                  .WithMany(h => h.Items)
                  .HasForeignKey(i => i.HeldBillId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── StockTake (#69) ──────────────────────────────────────
        modelBuilder.Entity<StockTake>(entity =>
        {
            entity.HasIndex(s => s.Status);
            entity.HasIndex(s => s.StartedAt);
        });

        modelBuilder.Entity<StockTakeItem>(entity =>
        {
            entity.HasOne(i => i.StockTake)
                  .WithMany(s => s.Items)
                  .HasForeignKey(i => i.StockTakeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Product)
                  .WithMany()
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── VendorPayment (#87 / #90) ─────────────────────────────
        modelBuilder.Entity<VendorPayment>(entity =>
        {
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(p => p.Vendor)
                  .WithMany()
                  .HasForeignKey(p => p.VendorId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(p => p.VendorId);
            entity.HasIndex(p => p.PaymentDate);
        });

        // ── Quotation (#348-#358) ─────────────────────────────────
        modelBuilder.Entity<Quotation>(entity =>
        {
            entity.Property(q => q.QuoteNumber).HasMaxLength(30);
            entity.HasIndex(q => q.QuoteNumber).IsUnique();
            entity.HasIndex(q => q.QuoteDate);
            entity.HasIndex(q => q.Status);
            entity.Property(q => q.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(q => q.Customer)
                  .WithMany()
                  .HasForeignKey(q => q.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(q => q.ConvertedSale)
                  .WithMany()
                  .HasForeignKey(q => q.ConvertedSaleId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QuotationItem>(entity =>
        {
            entity.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(i => i.DiscountRate).HasColumnType("decimal(5,2)");
            entity.Property(i => i.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(i => i.CessAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(i => i.Quotation)
                  .WithMany(q => q.Items)
                  .HasForeignKey(i => i.QuotationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Product)
                  .WithMany()
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── GoodsReceivedNote (#362-#372) ─────────────────────────
        modelBuilder.Entity<GoodsReceivedNote>(entity =>
        {
            entity.Property(g => g.GRNNumber).HasMaxLength(30);
            entity.HasIndex(g => g.GRNNumber).IsUnique();
            entity.HasIndex(g => g.ReceivedDate);
            entity.HasIndex(g => g.Status);
            entity.Property(g => g.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(g => g.Supplier)
                  .WithMany()
                  .HasForeignKey(g => g.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(g => g.PurchaseOrder)
                  .WithMany()
                  .HasForeignKey(g => g.PurchaseOrderId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GRNItem>(entity =>
        {
            entity.Property(i => i.UnitCost).HasColumnType("decimal(18,2)");
            entity.HasOne(i => i.GRN)
                  .WithMany(g => g.Items)
                  .HasForeignKey(i => i.GRNId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Product)
                  .WithMany()
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ProductSupplier (#92/#93) ─────────────────────────────
        modelBuilder.Entity<ProductSupplier>(entity =>
        {
            entity.Property(ps => ps.UnitCost).HasColumnType("decimal(18,2)");
            entity.HasOne(ps => ps.Product)
                  .WithMany()
                  .HasForeignKey(ps => ps.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ps => ps.Supplier)
                  .WithMany()
                  .HasForeignKey(ps => ps.SupplierId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(ps => new { ps.ProductId, ps.SupplierId }).IsUnique();
            entity.HasIndex(ps => ps.IsPrimary);
        });

        // ── DiscountRule (#180-#190) ──────────────────────────────
        modelBuilder.Entity<DiscountRule>(entity =>
        {
            entity.Property(d => d.DiscountValue).HasColumnType("decimal(18,2)");
            entity.Property(d => d.MinBillAmount).HasColumnType("decimal(18,2)");
            entity.Property(d => d.MaxDiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(d => d.ComboPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(d => d.RuleType);
            entity.HasIndex(d => d.IsActive);
            entity.HasIndex(d => new { d.ValidFrom, d.ValidTo });
        });

        // ── RecurringExpense (#234) ───────────────────────────────
        modelBuilder.Entity<RecurringExpense>(entity =>
        {
            entity.Property(r => r.Amount).HasColumnType("decimal(18,2)");
            entity.HasIndex(r => r.IsActive);
            entity.HasIndex(r => r.Frequency);
        });

        // ── CashRegisterShift (#250) ─────────────────────────────
        modelBuilder.Entity<CashRegisterShift>(entity =>
        {
            entity.Property(s => s.OpeningBalance).HasColumnType("decimal(18,2)");
            entity.Property(s => s.ClosingBalance).HasColumnType("decimal(18,2)");
            entity.Property(s => s.Discrepancy).HasColumnType("decimal(18,2)");
            entity.Property(s => s.HandoverAmount).HasColumnType("decimal(18,2)");
            entity.Ignore(s => s.IsClosed);
            entity.HasOne(s => s.CashRegister)
                  .WithMany()
                  .HasForeignKey(s => s.CashRegisterId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(s => s.CashRegisterId);
            entity.HasIndex(s => s.OpenedAt);
        });

        // ── PurchaseReturn (#374) ────────────────────────────────
        modelBuilder.Entity<PurchaseReturn>(entity =>
        {
            entity.Property(r => r.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(r => r.ReturnNumber).IsUnique();
            entity.HasIndex(r => r.ReturnDate);
            entity.HasOne(r => r.Supplier)
                  .WithMany()
                  .HasForeignKey(r => r.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseReturnItem>(entity =>
        {
            entity.Property(i => i.UnitCost).HasColumnType("decimal(18,2)");
            entity.HasOne(i => i.PurchaseReturn)
                  .WithMany(r => r.Items)
                  .HasForeignKey(i => i.PurchaseReturnId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.Product)
                  .WithMany()
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PermissionEntry (#289) ───────────────────────────────
        modelBuilder.Entity<PermissionEntry>(entity =>
        {
            entity.HasIndex(p => p.FeatureKey).IsUnique();
        });
    }
}
