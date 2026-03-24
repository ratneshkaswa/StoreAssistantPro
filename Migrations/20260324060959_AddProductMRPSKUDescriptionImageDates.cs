using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAssistantPro.Migrations
{
    /// <inheritdoc />
    public partial class AddProductMRPSKUDescriptionImageDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "UserCredentials",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "UserCredentials",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "UserCredentials",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsNoBillReturn",
                table: "SaleReturns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OriginalQuotationId",
                table: "Quotations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevisionNumber",
                table: "Quotations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "ProductVariants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Products",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MRP",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Products",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ReceiptPath",
                table: "Expenses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Anniversary",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Birthday",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "Customers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CustomerGroup",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "BackupEncryptionEnabled",
                table: "AppConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BackupIntervalHours",
                table: "AppConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BackupRetentionDays",
                table: "AppConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BarcodeFormat",
                table: "AppConfigs",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClosingTime",
                table: "AppConfigs",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DashboardWidgetLayout",
                table: "AppConfigs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DecimalPlaces",
                table: "AppConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpenseApprovalThreshold",
                table: "AppConfigs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "FontScalePercent",
                table: "AppConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "GoldTierThreshold",
                table: "AppConfigs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "HeldBillTimeoutMinutes",
                table: "AppConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsStockFrozen",
                table: "AppConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LabelPaperSize",
                table: "AppConfigs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdateCheck",
                table: "AppConfigs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseKey",
                table: "AppConfigs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyPointsRate",
                table: "AppConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxHeldBillsPerUser",
                table: "AppConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlySalesTarget",
                table: "AppConfigs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OpeningTime",
                table: "AppConfigs",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PlatinumTierThreshold",
                table: "AppConfigs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "QuotationTermsAndConditions",
                table: "AppConfigs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "SilverTierThreshold",
                table: "AppConfigs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "SoundEffectsEnabled",
                table: "AppConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StartupMode",
                table: "AppConfigs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StockAdjustmentApprovalThreshold",
                table: "AppConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThemeMode",
                table: "AppConfigs",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CashRegisterShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashRegisterId = table.Column<int>(type: "int", nullable: false),
                    CashierRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Discrepancy = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HandoverAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashRegisterShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashRegisterShifts_CashRegisters_CashRegisterId",
                        column: x => x.CashRegisterId,
                        principalTable: "CashRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscountRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinBillAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BuyQuantity = table.Column<int>(type: "int", nullable: false),
                    FreeQuantity = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    BrandId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ComboProductIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ComboPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AllowStacking = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductSuppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SupplierSKU = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: false),
                    MinOrderQty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSuppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSuppliers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSuppliers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DebitNoteNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_Vendors_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DueDay = table.Column<int>(type: "int", nullable: false),
                    LastGeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringExpenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturnItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseReturnId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnItems_PurchaseReturns_PurchaseReturnId",
                        column: x => x.PurchaseReturnId,
                        principalTable: "PurchaseReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU",
                unique: true,
                filter: "[SKU] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionEntries_FeatureKey",
                table: "PermissionEntries",
                column: "FeatureKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisterShifts_CashRegisterId",
                table: "CashRegisterShifts",
                column: "CashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisterShifts_OpenedAt",
                table: "CashRegisterShifts",
                column: "OpenedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRules_IsActive",
                table: "DiscountRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRules_RuleType",
                table: "DiscountRules",
                column: "RuleType");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRules_ValidFrom_ValidTo",
                table: "DiscountRules",
                columns: new[] { "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSuppliers_IsPrimary",
                table: "ProductSuppliers",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSuppliers_ProductId_SupplierId",
                table: "ProductSuppliers",
                columns: new[] { "ProductId", "SupplierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSuppliers_SupplierId",
                table: "ProductSuppliers",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnItems_ProductId",
                table: "PurchaseReturnItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnItems_PurchaseReturnId",
                table: "PurchaseReturnItems",
                column: "PurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_ReturnDate",
                table: "PurchaseReturns",
                column: "ReturnDate");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_ReturnNumber",
                table: "PurchaseReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_SupplierId",
                table: "PurchaseReturns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_Frequency",
                table: "RecurringExpenses",
                column: "Frequency");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_IsActive",
                table: "RecurringExpenses",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashRegisterShifts");

            migrationBuilder.DropTable(
                name: "DiscountRules");

            migrationBuilder.DropTable(
                name: "ProductSuppliers");

            migrationBuilder.DropTable(
                name: "PurchaseReturnItems");

            migrationBuilder.DropTable(
                name: "RecurringExpenses");

            migrationBuilder.DropTable(
                name: "PurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_Products_SKU",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PermissionEntries_FeatureKey",
                table: "PermissionEntries");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "UserCredentials");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "UserCredentials");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "UserCredentials");

            migrationBuilder.DropColumn(
                name: "IsNoBillReturn",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "OriginalQuotationId",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "RevisionNumber",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MRP",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReceiptPath",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "Anniversary",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Birthday",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerGroup",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BackupEncryptionEnabled",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "BackupIntervalHours",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "BackupRetentionDays",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "BarcodeFormat",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "ClosingTime",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "DashboardWidgetLayout",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "DecimalPlaces",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "ExpenseApprovalThreshold",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "FontScalePercent",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "GoldTierThreshold",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "HeldBillTimeoutMinutes",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "IsStockFrozen",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "LabelPaperSize",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "LastUpdateCheck",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "LicenseKey",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "LoyaltyPointsRate",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "MaxHeldBillsPerUser",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "MonthlySalesTarget",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "OpeningTime",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "PlatinumTierThreshold",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "QuotationTermsAndConditions",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "SilverTierThreshold",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "SoundEffectsEnabled",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "StartupMode",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "StockAdjustmentApprovalThreshold",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "ThemeMode",
                table: "AppConfigs");
        }
    }
}
