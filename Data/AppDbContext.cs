using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Models;
using StoreAssistantPro.Models.AI;
using StoreAssistantPro.Models.Documents;
using StoreAssistantPro.Models.Hardware;
using StoreAssistantPro.Models.HR;
using StoreAssistantPro.Models.Preferences;
using StoreAssistantPro.Models.Workflows;
using StoreAssistantPro.Models.Commercial;
using StoreAssistantPro.Models.MultiStore;
using StoreAssistantPro.Models.Ecommerce;
using StoreAssistantPro.Models.NicheVertical;
using StoreAssistantPro.Models.CRM;
using StoreAssistantPro.Models.Compliance;
using StoreAssistantPro.Models.PaymentGateway;
using StoreAssistantPro.Models.Budgeting;
using StoreAssistantPro.Models.Reporting;

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

    // ── G1: Hardware Integration ──
    public DbSet<HardwareDeviceConfig> HardwareDeviceConfigs => Set<HardwareDeviceConfig>();

    // ── G2: AI & Smart Features ──
    public DbSet<AnomalyAlert> AnomalyAlerts => Set<AnomalyAlert>();

    // ── G13: HR / Staff ──
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<ShiftSchedule> ShiftSchedules => Set<ShiftSchedule>();
    public DbSet<StaffShiftAssignment> StaffShiftAssignments => Set<StaffShiftAssignment>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<StaffTarget> StaffTargets => Set<StaffTarget>();

    // ── G17: Document Management ──
    public DbSet<StoredDocument> StoredDocuments => Set<StoredDocument>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();

    // ── G18: Workflow Automation ──
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();

    // ── G19: User Preferences ──
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    // ── G3: Commercial ──
    public DbSet<LicenseInfo> LicenseInfos => Set<LicenseInfo>();

    // ── G5: Multi-Store ──
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();

    // ── G6: E-commerce ──
    public DbSet<PlatformConnection> PlatformConnections => Set<PlatformConnection>();
    public DbSet<OnlineOrder> OnlineOrders => Set<OnlineOrder>();
    public DbSet<ProductListing> ProductListings => Set<ProductListing>();

    // ── G10: Niche Vertical ──
    public DbSet<AlterationOrder> AlterationOrders => Set<AlterationOrder>();
    public DbSet<RentalRecord> RentalRecords => Set<RentalRecord>();
    public DbSet<WholesalePriceTier> WholesalePriceTiers => Set<WholesalePriceTier>();
    public DbSet<ConsignmentRecord> ConsignmentRecords => Set<ConsignmentRecord>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();
    public DbSet<LoyaltyRule> LoyaltyRules => Set<LoyaltyRule>();

    // ── G12: CRM ──
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<ServiceTicket> ServiceTickets => Set<ServiceTicket>();
    public DbSet<CrmTemplate> CrmTemplates => Set<CrmTemplate>();

    // ── G14: Compliance ──
    public DbSet<EWayBill> EWayBills => Set<EWayBill>();
    public DbSet<EInvoice> EInvoices => Set<EInvoice>();

    // ── G20: Payment Gateway ──
    public DbSet<GatewayConfig> GatewayConfigs => Set<GatewayConfig>();
    public DbSet<GatewayTransaction> GatewayTransactions => Set<GatewayTransaction>();
    public DbSet<PaymentSchedule> PaymentSchedules => Set<PaymentSchedule>();

    // ── G21: Budgeting ──
    public DbSet<BudgetEntry> BudgetEntries => Set<BudgetEntry>();
    public DbSet<FinancialGoal> FinancialGoals => Set<FinancialGoal>();

    // ── G22: Advanced Reporting ──
    public DbSet<CustomReport> CustomReports => Set<CustomReport>();
    public DbSet<ReportSchedule> ReportSchedules => Set<ReportSchedule>();

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
            entity.Property(c => c.CompositionSchemeRate).HasColumnType("decimal(5,2)");
            entity.Property(c => c.MaxDiscountPercent).HasColumnType("decimal(5,2)");
            entity.Property(c => c.SilverTierThreshold).HasColumnType("decimal(18,2)");
            entity.Property(c => c.GoldTierThreshold).HasColumnType("decimal(18,2)");
            entity.Property(c => c.PlatinumTierThreshold).HasColumnType("decimal(18,2)");
            entity.Property(c => c.MonthlySalesTarget).HasColumnType("decimal(18,2)");
            entity.Property(c => c.ExpenseApprovalThreshold).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.SalePrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.CessPercent).HasColumnType("decimal(5,2)");
            entity.Property(p => p.HSNCode).HasMaxLength(8);
            entity.Property(p => p.Barcode).HasMaxLength(50);
            entity.Property(p => p.UOM).HasMaxLength(20).HasDefaultValue("pcs");
            entity.Property(p => p.Name).HasMaxLength(200);
            entity.HasIndex(p => p.HSNCode);
            entity.HasIndex(p => p.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
            entity.HasOne(p => p.Tax)
                  .WithMany()
                  .HasForeignKey(p => p.TaxId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(p => p.Brand)
                  .WithMany(b => b.Products)
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
            entity.HasIndex(p => p.IsActive);
            entity.Ignore(p => p.IsLowStock);
            entity.Ignore(p => p.StockValue);
            entity.Ignore(p => p.Margin);
            entity.Ignore(p => p.MarginPercent);
            entity.Ignore(p => p.RetailValue);
            entity.Ignore(p => p.IsOverStock);
            entity.Ignore(p => p.HighlightLevel);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(s => s.DiscountValue).HasColumnType("decimal(18,2)");
            entity.Property(s => s.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Ignore(s => s.ItemsSummary);
            entity.Ignore(s => s.DiscountSummary);
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
            entity.Property(t => t.TaxName).HasMaxLength(100);
            entity.HasIndex(t => t.TaxName).IsUnique();
            entity.HasIndex(t => t.IsActive);
        });

        modelBuilder.Entity<TaxProfile>(entity =>
        {
            entity.Property(t => t.ProfileName).HasMaxLength(100);
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
            entity.Property(b => b.Name).HasMaxLength(100);
            entity.HasIndex(b => b.Name).IsUnique();
            entity.HasIndex(b => b.IsActive);
            entity.Ignore(b => b.ProductCount);
            entity.Ignore(b => b.HighlightLevel);
        });

        modelBuilder.Entity<CategoryType>(entity =>
        {
            entity.Property(ct => ct.Name).HasMaxLength(100);
            entity.HasIndex(ct => ct.Name).IsUnique();
            entity.HasIndex(ct => ct.IsActive);
            entity.HasMany(ct => ct.Categories)
                  .WithOne(c => c.CategoryType)
                  .HasForeignKey(c => c.CategoryTypeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(100);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasIndex(c => c.IsActive);
            entity.Ignore(c => c.HighlightLevel);
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
            entity.Property(s => s.Name).HasMaxLength(200);
            entity.HasIndex(s => s.Name);
            entity.HasIndex(s => s.GSTIN).HasFilter("[GSTIN] IS NOT NULL");
            entity.HasIndex(s => s.IsActive);
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.Property(p => p.OrderNumber).HasMaxLength(30);
            entity.HasIndex(p => p.OrderNumber).IsUnique();
            entity.HasIndex(p => p.OrderDate);
            entity.HasIndex(p => p.Status);
            entity.Ignore(p => p.TotalAmount);
            entity.HasOne(p => p.Supplier)
                  .WithMany()
                  .HasForeignKey(p => p.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.Property(i => i.UnitCost).HasColumnType("decimal(18,2)");
            entity.Ignore(i => i.Subtotal);
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
            entity.Property(c => c.CreditLimit).HasColumnType("decimal(18,2)");
            entity.HasIndex(c => c.IsActive);
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasIndex(s => s.StaffCode).IsUnique().HasFilter("[StaffCode] IS NOT NULL");
            entity.Property(s => s.NormalIncentiveRate).HasColumnType("decimal(5,2)");
            entity.Property(s => s.SpecialIncentiveRate).HasColumnType("decimal(5,2)");
            entity.Property(s => s.DailyTarget).HasColumnType("decimal(18,2)");
            entity.Property(s => s.BaseSalary).HasColumnType("decimal(18,2)");
            entity.HasIndex(s => s.IsActive);
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.Property(c => c.Code).HasMaxLength(30);
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.DiscountValue).HasColumnType("decimal(18,2)");
            entity.Property(c => c.MinBillAmount).HasColumnType("decimal(18,2)");
            entity.Property(c => c.MaxDiscountAmount).HasColumnType("decimal(18,2)");
            entity.Ignore(c => c.IsValid);
            entity.HasIndex(c => c.IsActive);
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.Property(v => v.Code).HasMaxLength(30);
            entity.HasIndex(v => v.Code).IsUnique();
            entity.Property(v => v.FaceValue).HasColumnType("decimal(18,2)");
            entity.Property(v => v.Balance).HasColumnType("decimal(18,2)");
            entity.Ignore(v => v.IsValid);
            entity.HasOne(v => v.Customer)
                  .WithMany()
                  .HasForeignKey(v => v.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(v => v.IsActive);
        });

        modelBuilder.Entity<SaleReturn>(entity =>
        {
            entity.Property(r => r.ReturnNumber).HasMaxLength(30);
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
            entity.Property(f => f.Name).HasMaxLength(30);
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
            entity.Property(si => si.ItemFlatDiscount).HasColumnType("decimal(18,2)");
            entity.Property(si => si.TaxRate).HasColumnType("decimal(5,2)");
            entity.Property(si => si.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(si => si.CessRate).HasColumnType("decimal(5,2)");
            entity.Property(si => si.CessAmount).HasColumnType("decimal(18,2)");
            entity.Ignore(si => si.CgstAmount);
            entity.Ignore(si => si.SgstAmount);
            entity.Ignore(si => si.IgstAmount);
            entity.Ignore(si => si.CgstRate);
            entity.Ignore(si => si.SgstRate);
            entity.Ignore(si => si.ItemDiscountAmount);
            entity.Ignore(si => si.DiscountedUnitPrice);
            entity.Ignore(si => si.Subtotal);
            entity.HasOne(si => si.Sale)
                  .WithMany(s => s.Items)
                  .HasForeignKey(si => si.SaleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(si => si.Product)
                  .WithMany()
                  .HasForeignKey(si => si.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
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
            entity.Property(c => c.Name).HasMaxLength(50);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasIndex(c => c.SortOrder);
            entity.HasIndex(c => c.IsActive);
        });

        // ── Product attributes (manual entry — unique names) ───────
        modelBuilder.Entity<ProductPattern>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(100);
            entity.HasIndex(p => p.Name).IsUnique();
            entity.HasIndex(p => p.IsActive);
        });

        modelBuilder.Entity<ProductSize>(entity =>
        {
            entity.Property(s => s.Name).HasMaxLength(50);
            entity.HasIndex(s => s.Name).IsUnique();
            entity.HasIndex(s => s.SortOrder);
            entity.HasIndex(s => s.IsActive);
        });

        modelBuilder.Entity<ProductVariantType>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(100);
            entity.HasIndex(t => t.Name).IsUnique();
            entity.HasIndex(t => t.IsActive);
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
            entity.Ignore(v => v.DisplayName);
            entity.Ignore(v => v.HighlightLevel);
            entity.HasIndex(v => v.IsActive);
        });

        // ── StockAdjustment
        modelBuilder.Entity<StockAdjustment>(entity =>
        {
            entity.Ignore(a => a.QuantityChange);
            entity.HasOne(a => a.Product)
                  .WithMany()
                  .HasForeignKey(a => a.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.ProductVariant)
                  .WithMany()
                  .HasForeignKey(a => a.ProductVariantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.ProductId);
            entity.HasIndex(a => a.ProductVariantId);
            entity.HasIndex(a => a.Timestamp);
        });

        // ── Vendor (GSTIN + PAN indexes)
        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.Property(v => v.Name).HasMaxLength(200);
            entity.HasIndex(v => v.Name).IsUnique();
            entity.HasIndex(v => v.GSTIN).HasFilter("[GSTIN] IS NOT NULL");
            entity.HasIndex(v => v.PAN).HasFilter("[PAN] IS NOT NULL");
            entity.Property(v => v.CreditLimit).HasColumnType("decimal(18,2)");
            entity.Property(v => v.OpeningBalance).HasColumnType("decimal(18,2)");
            entity.Ignore(v => v.HighlightLevel);
            entity.Ignore(v => v.ProductCount);
            entity.HasIndex(v => v.IsActive);
        });

        // ── Inward Entry
        modelBuilder.Entity<InwardEntry>(entity =>
        {
            entity.Property(e => e.InwardNumber).HasMaxLength(30);
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
            entity.Property(p => p.ParcelNumber).HasMaxLength(30);
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
            entity.Property(g => g.Name).HasMaxLength(100);
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
            entity.HasIndex(s => s.IsActive);
        });

        // ── HSN Code master ────────────────────────────────────────
        modelBuilder.Entity<HSNCode>(entity =>
        {
            entity.Property(h => h.Code).HasMaxLength(8);
            entity.HasIndex(h => h.Code).IsUnique();
            entity.HasIndex(h => h.Category);
            entity.HasIndex(h => h.IsActive);
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
            entity.Property(s => s.StateCode).HasMaxLength(5);
            entity.Property(s => s.Name).HasMaxLength(100);
            entity.HasIndex(s => s.StateCode).IsUnique();
            entity.HasIndex(s => s.Name).IsUnique();
            entity.HasIndex(s => s.IsActive);
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
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.Category);
        });

        // ── ExpenseCategory (#227) ────────────────────────────────
        modelBuilder.Entity<ExpenseCategory>(entity =>
        {
            entity.Property(ec => ec.Name).HasMaxLength(100);
            entity.HasIndex(ec => ec.Name).IsUnique();
            entity.HasIndex(ec => ec.IsActive);
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
            entity.Ignore(s => s.DaysInMonth);
            entity.Ignore(s => s.OvertimeHours);
            entity.Ignore(s => s.PenaltyCount);
            entity.Ignore(s => s.PenaltyAmount);
            entity.Ignore(s => s.PenaltyDisplay);
            entity.Ignore(s => s.MonthAdjustment);
            entity.Ignore(s => s.NetPay);
            entity.Ignore(s => s.MonthIndex);
            entity.Ignore(s => s.TuesdaysInMonth);
            entity.Ignore(s => s.WorkingDays);
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
            entity.Property(a => a.Action).HasMaxLength(50);
            entity.Property(a => a.EntityType).HasMaxLength(50);
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
            entity.Property(i => i.TaxRate).HasColumnType("decimal(5,2)");
            entity.Property(i => i.CessRate).HasColumnType("decimal(5,2)");
            entity.HasOne(i => i.HeldBill)
                  .WithMany(h => h.Items)
                  .HasForeignKey(i => i.HeldBillId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Product>()
                  .WithMany()
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ProductVariant>()
                  .WithMany()
                  .HasForeignKey(i => i.ProductVariantId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ── StockTake (#69) ──────────────────────────────────────
        modelBuilder.Entity<StockTake>(entity =>
        {
            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(s => s.Status);
            entity.HasIndex(s => s.StartedAt);
        });

        modelBuilder.Entity<StockTakeItem>(entity =>
        {
            entity.Ignore(i => i.Variance);
            entity.HasOne(i => i.StockTake)
                  .WithMany(s => s.Items)
                  .HasForeignKey(i => i.StockTakeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Product)
                  .WithMany()
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ProductVariant>()
                  .WithMany()
                  .HasForeignKey(i => i.ProductVariantId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ── VendorPayment (#87 / #90) ─────────────────────────────
        modelBuilder.Entity<VendorPayment>(entity =>
        {
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(p => p.Vendor)
                  .WithMany()
                  .HasForeignKey(p => p.VendorId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(p => p.VendorId);
            entity.HasIndex(p => p.PaymentDate);
        });

        // ── Quotation (#348-#358)
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
            entity.HasOne<Quotation>()
                  .WithMany()
                  .HasForeignKey(q => q.OriginalQuotationId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QuotationItem>(entity =>
        {
            entity.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(i => i.DiscountRate).HasColumnType("decimal(5,2)");
            entity.Property(i => i.TaxRate).HasColumnType("decimal(5,2)");
            entity.Property(i => i.CessRate).HasColumnType("decimal(5,2)");
            entity.Property(i => i.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(i => i.CessAmount).HasColumnType("decimal(18,2)");
            entity.Ignore(i => i.DiscountAmount);
            entity.Ignore(i => i.DiscountedPrice);
            entity.Ignore(i => i.Subtotal);
            entity.Ignore(i => i.LineTotal);
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
            entity.Ignore(i => i.Subtotal);
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
            entity.Property(d => d.RuleType).HasMaxLength(30);
            entity.HasOne<Category>()
                  .WithMany()
                  .HasForeignKey(d => d.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Brand>()
                  .WithMany()
                  .HasForeignKey(d => d.BrandId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Customer>()
                  .WithMany()
                  .HasForeignKey(d => d.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
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
            entity.Property(r => r.ReturnNumber).HasMaxLength(30);
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
            entity.Property(p => p.FeatureKey).HasMaxLength(100);
            entity.HasIndex(p => p.FeatureKey).IsUnique();
        });

        // ── SystemSettings (single-row workstation config) ────────
        modelBuilder.Entity<SystemSettings>(entity =>
        {
            entity.Property(s => s.DefaultTaxMode).HasMaxLength(20);
            entity.Property(s => s.RoundingMethod).HasMaxLength(20);
            entity.Property(s => s.NumberToWordsLanguage).HasMaxLength(20);
            entity.Property(s => s.DefaultPageSize).HasMaxLength(10);
        });

        // ── TaskItem ──────────────────────────────────────────────
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.Ignore(t => t.IsOverdue);
            entity.Ignore(t => t.DueDateDisplay);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.Priority);
            entity.HasIndex(t => t.DueDate);
            entity.HasIndex(t => t.CreatedAt);
        });

        // ── G1: HardwareDeviceConfig (#471-#518) ─────────────────
        modelBuilder.Entity<HardwareDeviceConfig>(entity =>
        {
            entity.HasIndex(d => d.DeviceType);
            entity.HasIndex(d => d.IsEnabled);
            entity.Property(d => d.DeviceName).HasMaxLength(100);
            entity.Property(d => d.PortName).HasMaxLength(50);
            entity.Property(d => d.ConnectionType).HasMaxLength(20);
            entity.Property(d => d.IpAddress).HasMaxLength(45);
            entity.Property(d => d.ModelName).HasMaxLength(100);
        });

        // ── G2: AnomalyAlert (#535-#540) ─────────────────────────
        modelBuilder.Entity<AnomalyAlert>(entity =>
        {
            entity.Property(a => a.AlertType).HasMaxLength(50);
            entity.Property(a => a.Severity).HasMaxLength(20);
            entity.Property(a => a.Description).HasMaxLength(500);
            entity.Property(a => a.RelatedEntityType).HasMaxLength(50);
            entity.HasIndex(a => a.AlertType);
            entity.HasIndex(a => a.Severity);
            entity.HasIndex(a => a.DetectedAtUtc);
            entity.HasIndex(a => a.IsReviewed);
        });

        // ── G13: HR / Staff (#824-#831) ──────────────────────────
        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasIndex(a => new { a.UserId, a.Date }).IsUnique();
            entity.Property(a => a.Status).HasMaxLength(20);
            entity.Property(a => a.Notes).HasMaxLength(500);
            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShiftSchedule>(entity =>
        {
            entity.Property(s => s.ShiftName).HasMaxLength(50);
        });

        modelBuilder.Entity<StaffShiftAssignment>(entity =>
        {
            entity.HasIndex(a => new { a.UserId, a.ShiftScheduleId });
            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ShiftSchedule>()
                  .WithMany()
                  .HasForeignKey(a => a.ShiftScheduleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.Property(l => l.LeaveType).HasMaxLength(30);
            entity.Property(l => l.Status).HasMaxLength(20);
            entity.Property(l => l.Reason).HasMaxLength(500);
            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(l => l.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(l => l.ApprovedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(l => new { l.UserId, l.Status });
        });

        modelBuilder.Entity<StaffTarget>(entity =>
        {
            entity.Property(t => t.TargetAmount).HasColumnType("decimal(18,2)");
            entity.Property(t => t.AchievedAmount).HasColumnType("decimal(18,2)");
            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(t => new { t.UserId, t.Month, t.Year }).IsUnique();
        });

        // ── G17: Document Management (#881-#889) ────────────────
        modelBuilder.Entity<StoredDocument>(entity =>
        {
            entity.Property(d => d.DocumentType).HasMaxLength(30);
            entity.Property(d => d.FileName).HasMaxLength(255);
            entity.Property(d => d.FilePath).HasMaxLength(500);
            entity.Property(d => d.RelatedEntityType).HasMaxLength(50);
            entity.Property(d => d.CreatedByUser).HasMaxLength(100);
            entity.Property(d => d.Tags).HasMaxLength(500);
            entity.HasIndex(d => d.DocumentType);
            entity.HasIndex(d => d.CreatedAt);
            entity.HasIndex(d => d.CustomerId);
            entity.HasOne<Customer>()
                  .WithMany()
                  .HasForeignKey(d => d.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DocumentTemplate>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(100);
            entity.Property(t => t.DocumentType).HasMaxLength(30);
            entity.Property(t => t.LogoPath).HasMaxLength(500);
            entity.HasIndex(t => t.DocumentType);
            entity.HasIndex(t => t.IsDefault);
        });

        // ── G18: Workflow Automation (#890-#897) ─────────────────
        modelBuilder.Entity<AutomationRule>(entity =>
        {
            entity.Property(r => r.Name).HasMaxLength(100);
            entity.Property(r => r.TriggerType).HasMaxLength(50);
            entity.Property(r => r.ActionType).HasMaxLength(50);
            entity.HasIndex(r => r.TriggerType);
            entity.HasIndex(r => r.IsEnabled);
        });

        // ── G19: User Preferences (#898-#916) ───────────────────
        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.Property(p => p.Key).HasMaxLength(100);
            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(p => new { p.UserId, p.Key }).IsUnique();
        });

        // ── G3: Commercial (#541-#567) ──────────────────────────
        modelBuilder.Entity<LicenseInfo>(entity =>
        {
            entity.Property(l => l.LicenseKey).HasMaxLength(100);
            entity.HasIndex(l => l.LicenseKey).IsUnique();
            entity.Property(l => l.MachineId).HasMaxLength(100);
            entity.HasIndex(l => l.IsActive);
        });

        // ── G5: Multi-Store (#591-#639) ─────────────────────────
        modelBuilder.Entity<Store>(entity =>
        {
            entity.Property(s => s.Name).HasMaxLength(200);
            entity.HasIndex(s => s.Name).IsUnique();
            entity.Property(s => s.Address).HasMaxLength(500);
            entity.Property(s => s.Phone).HasMaxLength(20);
            entity.Property(s => s.GSTIN).HasMaxLength(15);
            entity.HasOne<Store>()
                  .WithMany()
                  .HasForeignKey(s => s.ParentStoreId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(s => s.IsActive);
        });

        modelBuilder.Entity<StockTransfer>(entity =>
        {
            entity.Property(t => t.Status).HasMaxLength(20);
            entity.Property(t => t.Notes).HasMaxLength(500);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.RequestedAt);
            entity.HasIndex(t => new { t.FromStoreId, t.ToStoreId });
            entity.HasOne<Store>()
                  .WithMany()
                  .HasForeignKey(t => t.FromStoreId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Store>()
                  .WithMany()
                  .HasForeignKey(t => t.ToStoreId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>()
                  .WithMany()
                  .HasForeignKey(t => t.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(t => t.RequestedByUserId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(t => t.ApprovedByUserId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ── G6: E-commerce (#640-#685)
        modelBuilder.Entity<PlatformConnection>(entity =>
        {
            entity.Property(p => p.StoreUrl).HasMaxLength(500);
            entity.Property(p => p.ApiKey).HasMaxLength(500);
            entity.Property(p => p.ApiSecret).HasMaxLength(500);
            entity.HasIndex(p => p.Platform);
            entity.HasIndex(p => p.IsActive);
        });

        modelBuilder.Entity<OnlineOrder>(entity =>
        {
            entity.Property(o => o.PlatformOrderId).HasMaxLength(100);
            entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(o => o.Status).HasMaxLength(20);
            entity.Property(o => o.CustomerName).HasMaxLength(200);
            entity.Property(o => o.CustomerEmail).HasMaxLength(256);
            entity.Property(o => o.ShippingAddress).HasMaxLength(500);
            entity.Property(o => o.TrackingNumber).HasMaxLength(100);
            entity.HasIndex(o => o.PlatformOrderId);
            entity.HasIndex(o => o.Platform);
            entity.HasIndex(o => o.OrderDate);
            entity.HasIndex(o => o.Status);
            entity.HasOne<Sale>()
                  .WithMany()
                  .HasForeignKey(o => o.ConvertedSaleId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProductListing>(entity =>
        {
            entity.Property(l => l.PlatformProductId).HasMaxLength(100);
            entity.Property(l => l.PlatformSku).HasMaxLength(100);
            entity.Property(l => l.PlatformPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(l => new { l.ProductId, l.Platform }).IsUnique();
            entity.HasOne<Product>()
                  .WithMany()
                  .HasForeignKey(l => l.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── G10: Niche Vertical (#728-#787) ─────────────────────
        modelBuilder.Entity<AlterationOrder>(entity =>
        {
            entity.Property(a => a.AlterationType).HasMaxLength(30);
            entity.Property(a => a.Status).HasMaxLength(20);
            entity.Property(a => a.Measurements).HasMaxLength(500);
            entity.Property(a => a.FittingNotes).HasMaxLength(500);
            entity.Property(a => a.Charge).HasColumnType("decimal(18,2)");
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.CreatedAt);
            entity.HasOne<Sale>()
                  .WithMany()
                  .HasForeignKey(a => a.SaleId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Customer>()
                  .WithMany()
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Staff>()
                  .WithMany()
                  .HasForeignKey(a => a.AssignedTailorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RentalRecord>(entity =>
        {
            entity.Property(r => r.RentalPrice).HasColumnType("decimal(18,2)");
            entity.Property(r => r.DepositAmount).HasColumnType("decimal(18,2)");
            entity.Property(r => r.LateFeePerDay).HasColumnType("decimal(18,2)");
            entity.Property(r => r.Status).HasMaxLength(20);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.RentalEnd);
            entity.HasOne<Product>()
                  .WithMany()
                  .HasForeignKey(r => r.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>()
                  .WithMany()
                  .HasForeignKey(r => r.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WholesalePriceTier>(entity =>
        {
            entity.Property(w => w.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(w => w.DiscountPercent).HasColumnType("decimal(5,2)");
            entity.HasIndex(w => new { w.ProductId, w.MinQuantity }).IsUnique();
            entity.HasOne<Product>()
                  .WithMany()
                  .HasForeignKey(w => w.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConsignmentRecord>(entity =>
        {
            entity.Property(c => c.CommissionPercent).HasColumnType("decimal(5,2)");
            entity.Property(c => c.ConsignorName).HasMaxLength(200);
            entity.HasIndex(c => c.IsSettled);
            entity.HasIndex(c => c.ReceivedDate);
            entity.HasOne<Product>()
                  .WithMany()
                  .HasForeignKey(c => c.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Season>(entity =>
        {
            entity.Property(s => s.Name).HasMaxLength(100);
            entity.HasIndex(s => s.Name).IsUnique();
            entity.HasIndex(s => s.IsActive);
        });

        modelBuilder.Entity<GiftCard>(entity =>
        {
            entity.Property(g => g.CardNumber).HasMaxLength(50);
            entity.HasIndex(g => g.CardNumber).IsUnique();
            entity.Property(g => g.OriginalValue).HasColumnType("decimal(18,2)");
            entity.Property(g => g.RemainingBalance).HasColumnType("decimal(18,2)");
            entity.HasIndex(g => g.IsActive);
            entity.HasOne<Customer>()
                  .WithMany()
                  .HasForeignKey(g => g.IssuedToCustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LoyaltyRule>(entity =>
        {
            entity.Property(l => l.EarningRatio).HasColumnType("decimal(18,4)");
            entity.Property(l => l.RedemptionRatio).HasColumnType("decimal(18,4)");
            entity.Property(l => l.TierExtraPercent).HasColumnType("decimal(5,2)");
            entity.Property(l => l.TierName).HasMaxLength(50);
            entity.HasIndex(l => l.IsActive);
        });

        // ── G12: CRM (#799-#823) ───────────────────────────────
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(200);
            entity.Property(c => c.Channel).HasMaxLength(20);
            entity.Property(c => c.Status).HasMaxLength(20);
            entity.Property(c => c.TargetSegment).HasMaxLength(200);
            entity.HasIndex(c => c.Status);
            entity.HasIndex(c => c.ScheduledAt);
        });

        modelBuilder.Entity<ServiceTicket>(entity =>
        {
            entity.Property(t => t.TicketType).HasMaxLength(30);
            entity.Property(t => t.Status).HasMaxLength(20);
            entity.Property(t => t.Priority).HasMaxLength(20);
            entity.Property(t => t.Subject).HasMaxLength(200);
            entity.Property(t => t.Description).HasMaxLength(2000);
            entity.Property(t => t.ResolutionNotes).HasMaxLength(2000);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.Priority);
            entity.HasIndex(t => t.CreatedAt);
            entity.HasOne<Customer>()
                  .WithMany()
                  .HasForeignKey(t => t.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(t => t.AssignedToUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CrmTemplate>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(100);
            entity.Property(t => t.Channel).HasMaxLength(20);
            entity.HasIndex(t => t.Name).IsUnique();
            entity.HasIndex(t => t.IsActive);
        });

        // ── G14: Compliance (#832-#848) ─────────────────────────
        modelBuilder.Entity<EWayBill>(entity =>
        {
            entity.Property(e => e.EWayBillNumber).HasMaxLength(20);
            entity.HasIndex(e => e.EWayBillNumber).IsUnique();
            entity.Property(e => e.TransportMode).HasMaxLength(20);
            entity.Property(e => e.VehicleNumber).HasMaxLength(20);
            entity.Property(e => e.GoodsValue).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.SaleId);
            entity.HasOne<Sale>()
                  .WithMany()
                  .HasForeignKey(e => e.SaleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EInvoice>(entity =>
        {
            entity.Property(i => i.Irn).HasMaxLength(100);
            entity.HasIndex(i => i.Irn).IsUnique();
            entity.HasIndex(i => i.SaleId);
            entity.HasIndex(i => i.IsValid);
            entity.HasOne<Sale>()
                  .WithMany()
                  .HasForeignKey(i => i.SaleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── G20: Payment Gateway (#917-#933) ───────────────────
        modelBuilder.Entity<GatewayConfig>(entity =>
        {
            entity.Property(c => c.MerchantId).HasMaxLength(100);
            entity.Property(c => c.ApiKey).HasMaxLength(500);
            entity.Property(c => c.ApiSecret).HasMaxLength(500);
            entity.HasIndex(c => c.Provider);
            entity.HasIndex(c => c.IsActive);
        });

        modelBuilder.Entity<GatewayTransaction>(entity =>
        {
            entity.Property(t => t.GatewayTransactionId).HasMaxLength(100);
            entity.HasIndex(t => t.GatewayTransactionId);
            entity.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            entity.Property(t => t.Status).HasMaxLength(20);
            entity.Property(t => t.UpiDeepLink).HasMaxLength(500);
            entity.Property(t => t.RefundId).HasMaxLength(100);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.CreatedAt);
            entity.HasIndex(t => t.SaleId);
            entity.HasOne<Sale>()
                  .WithMany()
                  .HasForeignKey(t => t.SaleId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PaymentSchedule>(entity =>
        {
            entity.Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(s => s.PaidAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(s => s.IsComplete);
            entity.HasIndex(s => s.NextDueDate);
            entity.HasOne<Customer>()
                  .WithMany()
                  .HasForeignKey(s => s.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Sale>()
                  .WithMany()
                  .HasForeignKey(s => s.SaleId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── G21: Budgeting (#934-#939) ──────────────────────────
        modelBuilder.Entity<BudgetEntry>(entity =>
        {
            entity.Property(b => b.BudgetType).HasMaxLength(20);
            entity.Property(b => b.Category).HasMaxLength(100);
            entity.Property(b => b.BudgetAmount).HasColumnType("decimal(18,2)");
            entity.Property(b => b.ActualAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(b => new { b.BudgetType, b.Month, b.Year });
        });

        modelBuilder.Entity<FinancialGoal>(entity =>
        {
            entity.Property(g => g.GoalName).HasMaxLength(200);
            entity.Property(g => g.MetricType).HasMaxLength(30);
            entity.Property(g => g.TargetValue).HasColumnType("decimal(18,2)");
            entity.Property(g => g.CurrentValue).HasColumnType("decimal(18,2)");
            entity.HasIndex(g => g.IsAchieved);
        });

        // ── G22: Advanced Reporting (#940-#948) ─────────────────
        modelBuilder.Entity<CustomReport>(entity =>
        {
            entity.Property(r => r.Name).HasMaxLength(200);
            entity.Property(r => r.DataSource).HasMaxLength(50);
            entity.Property(r => r.ChartType).HasMaxLength(30);
            entity.Property(r => r.Description).HasMaxLength(500);
            entity.HasOne<UserCredential>()
                  .WithMany()
                  .HasForeignKey(r => r.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(r => r.CreatedByUserId);
            entity.HasIndex(r => r.IsBookmarked);
        });

        modelBuilder.Entity<ReportSchedule>(entity =>
        {
            entity.Property(s => s.Frequency).HasMaxLength(20);
            entity.Property(s => s.RecipientEmail).HasMaxLength(200);
            entity.HasIndex(s => s.CustomReportId);
            entity.HasIndex(s => s.IsActive);
            entity.HasIndex(s => s.NextRunAt);
            entity.HasOne<CustomReport>()
                  .WithMany()
                  .HasForeignKey(s => s.CustomReportId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
