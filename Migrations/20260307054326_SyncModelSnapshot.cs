using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAssistantPro.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentLogs_Products_ProductId",
                table: "StockAdjustmentLogs");

            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "SaleItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLogs_AdjustedAt",
                table: "StockAdjustmentLogs",
                column: "AdjustedAt");

            migrationBuilder.CreateIndex(
                name: "IX_States_Name",
                table: "States",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_States_StateCode",
                table: "States",
                column: "StateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_InvoiceNumber",
                table: "Sales",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_ProductVariantId",
                table: "SaleItems",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_ChangedDate",
                table: "PriceHistories",
                column: "ChangedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive",
                table: "Categories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_ProductVariants_ProductVariantId",
                table: "SaleItems",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentLogs_Products_ProductId",
                table: "StockAdjustmentLogs",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_ProductVariants_ProductVariantId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentLogs_Products_ProductId",
                table: "StockAdjustmentLogs");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustmentLogs_AdjustedAt",
                table: "StockAdjustmentLogs");

            migrationBuilder.DropIndex(
                name: "IX_States_Name",
                table: "States");

            migrationBuilder.DropIndex(
                name: "IX_States_StateCode",
                table: "States");

            migrationBuilder.DropIndex(
                name: "IX_Sales_InvoiceNumber",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_SaleItems_ProductVariantId",
                table: "SaleItems");

            migrationBuilder.DropIndex(
                name: "IX_PriceHistories_ChangedDate",
                table: "PriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsActive",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "SaleItems");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentLogs_Products_ProductId",
                table: "StockAdjustmentLogs",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
