using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAssistantPro.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyTaxModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InwardParcels_Categories_CategoryId",
                table: "InwardParcels");

            migrationBuilder.DropForeignKey(
                name: "FK_InwardParcels_Vendors_VendorId",
                table: "InwardParcels");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_TaxProfiles_TaxProfileId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_InwardParcels_CategoryId",
                table: "InwardParcels");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "TaxMasters");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "InwardParcels");

            migrationBuilder.RenameColumn(
                name: "TaxRate",
                table: "TaxMasters",
                newName: "SlabPercent");

            migrationBuilder.RenameColumn(
                name: "TaxProfileId",
                table: "Products",
                newName: "TaxId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_TaxProfileId",
                table: "Products",
                newName: "IX_Products_TaxId");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Vendors",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PAN",
                table: "Vendors",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransportPreference",
                table: "Vendors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductType",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsColour",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsPattern",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsSize",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsType",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Unit",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TransportCharge",
                table: "InwardParcels",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "VendorId",
                table: "InwardEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingCounterResetDate",
                table: "FinancialYears",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "AppConfigs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Pincode",
                table: "AppConfigs",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "AppConfigs",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "FirmName",
                table: "AppConfigs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AppConfigs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "AppConfigs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateTable(
                name: "Colours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HexCode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HSNCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HSNCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductPatterns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPatterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductSizes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSizes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BackupLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AutoBackupEnabled = table.Column<bool>(type: "bit", nullable: false),
                    BackupTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    RestoreOption = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DefaultPrinter = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultTaxMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InwardProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InwardParcelId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ColourId = table.Column<int>(type: "int", nullable: true),
                    SizeId = table.Column<int>(type: "int", nullable: true),
                    PatternId = table.Column<int>(type: "int", nullable: true),
                    VariantTypeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InwardProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InwardProducts_Colours_ColourId",
                        column: x => x.ColourId,
                        principalTable: "Colours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InwardProducts_InwardParcels_InwardParcelId",
                        column: x => x.InwardParcelId,
                        principalTable: "InwardParcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InwardProducts_ProductPatterns_PatternId",
                        column: x => x.PatternId,
                        principalTable: "ProductPatterns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InwardProducts_ProductSizes_SizeId",
                        column: x => x.SizeId,
                        principalTable: "ProductSizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InwardProducts_ProductVariantTypes_VariantTypeId",
                        column: x => x.VariantTypeId,
                        principalTable: "ProductVariantTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InwardProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductTaxMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    TaxGroupId = table.Column<int>(type: "int", nullable: false),
                    HSNCodeId = table.Column<int>(type: "int", nullable: false),
                    OverrideAllowed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTaxMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductTaxMappings_HSNCodes_HSNCodeId",
                        column: x => x.HSNCodeId,
                        principalTable: "HSNCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductTaxMappings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductTaxMappings_TaxGroups_TaxGroupId",
                        column: x => x.TaxGroupId,
                        principalTable: "TaxGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxSlabs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxGroupId = table.Column<int>(type: "int", nullable: false),
                    GSTPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CGSTPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SGSTPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IGSTPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PriceFrom = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PriceTo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxSlabs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxSlabs_TaxGroups_TaxGroupId",
                        column: x => x.TaxGroupId,
                        principalTable: "TaxGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_GSTIN",
                table: "Vendors",
                column: "GSTIN",
                filter: "[GSTIN] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_Name",
                table: "Vendors",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_PAN",
                table: "Vendors",
                column: "PAN",
                filter: "[PAN] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductType",
                table: "Products",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_InwardParcels_ParcelNumber",
                table: "InwardParcels",
                column: "ParcelNumber");

            migrationBuilder.CreateIndex(
                name: "IX_InwardEntries_InwardDate",
                table: "InwardEntries",
                column: "InwardDate");

            migrationBuilder.CreateIndex(
                name: "IX_InwardEntries_InwardNumber",
                table: "InwardEntries",
                column: "InwardNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InwardEntries_VendorId",
                table: "InwardEntries",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Colours_Name",
                table: "Colours",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colours_SortOrder",
                table: "Colours",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_HSNCodes_Category",
                table: "HSNCodes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_HSNCodes_Code",
                table: "HSNCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InwardProducts_ColourId",
                table: "InwardProducts",
                column: "ColourId");

            migrationBuilder.CreateIndex(
                name: "IX_InwardProducts_InwardParcelId",
                table: "InwardProducts",
                column: "InwardParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_InwardProducts_PatternId",
                table: "InwardProducts",
                column: "PatternId");

            migrationBuilder.CreateIndex(
                name: "IX_InwardProducts_ProductId",
                table: "InwardProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InwardProducts_SizeId",
                table: "InwardProducts",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_InwardProducts_VariantTypeId",
                table: "InwardProducts",
                column: "VariantTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPatterns_Name",
                table: "ProductPatterns",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSizes_Name",
                table: "ProductSizes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSizes_SortOrder",
                table: "ProductSizes",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTaxMappings_HSNCodeId",
                table: "ProductTaxMappings",
                column: "HSNCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTaxMappings_ProductId",
                table: "ProductTaxMappings",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductTaxMappings_TaxGroupId",
                table: "ProductTaxMappings",
                column: "TaxGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantTypes_Name",
                table: "ProductVariantTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxGroups_IsActive",
                table: "TaxGroups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaxGroups_Name",
                table: "TaxGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxSlabs_EffectiveFrom",
                table: "TaxSlabs",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_TaxSlabs_TaxGroupId_PriceFrom_PriceTo",
                table: "TaxSlabs",
                columns: new[] { "TaxGroupId", "PriceFrom", "PriceTo" });

            migrationBuilder.AddForeignKey(
                name: "FK_InwardEntries_Vendors_VendorId",
                table: "InwardEntries",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InwardParcels_Vendors_VendorId",
                table: "InwardParcels",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_TaxMasters_TaxId",
                table: "Products",
                column: "TaxId",
                principalTable: "TaxMasters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InwardEntries_Vendors_VendorId",
                table: "InwardEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_InwardParcels_Vendors_VendorId",
                table: "InwardParcels");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_TaxMasters_TaxId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "InwardProducts");

            migrationBuilder.DropTable(
                name: "ProductTaxMappings");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TaxSlabs");

            migrationBuilder.DropTable(
                name: "Colours");

            migrationBuilder.DropTable(
                name: "ProductPatterns");

            migrationBuilder.DropTable(
                name: "ProductSizes");

            migrationBuilder.DropTable(
                name: "ProductVariantTypes");

            migrationBuilder.DropTable(
                name: "HSNCodes");

            migrationBuilder.DropTable(
                name: "TaxGroups");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_GSTIN",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_Name",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Vendors_PAN",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductType",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_InwardParcels_ParcelNumber",
                table: "InwardParcels");

            migrationBuilder.DropIndex(
                name: "IX_InwardEntries_InwardDate",
                table: "InwardEntries");

            migrationBuilder.DropIndex(
                name: "IX_InwardEntries_InwardNumber",
                table: "InwardEntries");

            migrationBuilder.DropIndex(
                name: "IX_InwardEntries_VendorId",
                table: "InwardEntries");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PAN",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "TransportPreference",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupportsColour",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupportsPattern",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupportsSize",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupportsType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TransportCharge",
                table: "InwardParcels");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "InwardEntries");

            migrationBuilder.DropColumn(
                name: "BillingCounterResetDate",
                table: "FinancialYears");

            migrationBuilder.RenameColumn(
                name: "SlabPercent",
                table: "TaxMasters",
                newName: "TaxRate");

            migrationBuilder.RenameColumn(
                name: "TaxId",
                table: "Products",
                newName: "TaxProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_TaxId",
                table: "Products",
                newName: "IX_Products_TaxProfileId");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "TaxMasters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "InwardParcels",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "AppConfigs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Pincode",
                table: "AppConfigs",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(6)",
                oldMaxLength: 6);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "AppConfigs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15);

            migrationBuilder.AlterColumn<string>(
                name: "FirmName",
                table: "AppConfigs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AppConfigs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "AppConfigs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_InwardParcels_CategoryId",
                table: "InwardParcels",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_InwardParcels_Categories_CategoryId",
                table: "InwardParcels",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InwardParcels_Vendors_VendorId",
                table: "InwardParcels",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_TaxProfiles_TaxProfileId",
                table: "Products",
                column: "TaxProfileId",
                principalTable: "TaxProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
