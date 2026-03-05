using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAssistantPro.Migrations
{
    /// <inheritdoc />
    public partial class EnhancePurchaseOrderModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDate",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityReceived",
                table: "PurchaseOrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedDate",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "QuantityReceived",
                table: "PurchaseOrderItems");
        }
    }
}
