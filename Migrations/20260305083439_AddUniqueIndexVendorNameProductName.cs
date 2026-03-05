using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAssistantPro.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexVendorNameProductName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deduplicate vendor names before adding unique index:
            // append " (2)", " (3)" etc. to duplicates, keeping the lowest Id as-is.
            migrationBuilder.Sql("""
                WITH cte AS (
                    SELECT Id, Name, ROW_NUMBER() OVER (PARTITION BY Name ORDER BY Id) AS rn
                    FROM Vendors
                )
                UPDATE cte SET Name = Name + ' (' + CAST(rn AS NVARCHAR(10)) + ')'
                WHERE rn > 1;
                """);

            // Deduplicate product names before adding unique index.
            migrationBuilder.Sql("""
                WITH cte AS (
                    SELECT Id, Name, ROW_NUMBER() OVER (PARTITION BY Name ORDER BY Id) AS rn
                    FROM Products
                )
                UPDATE cte SET Name = Name + ' (' + CAST(rn AS NVARCHAR(10)) + ')'
                WHERE rn > 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Vendors_Name",
                table: "Vendors");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_Name",
                table: "Vendors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vendors_Name",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "IX_Products_Name",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_Name",
                table: "Vendors",
                column: "Name");
        }
    }
}
