using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAssistantPro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIndianBusinessConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "AppConfigs",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "INR");

            migrationBuilder.AddColumn<int>(
                name: "FinancialYearEndMonth",
                table: "AppConfigs",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "FinancialYearStartMonth",
                table: "AppConfigs",
                type: "int",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<string>(
                name: "GSTNumber",
                table: "AppConfigs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "FinancialYearEndMonth",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "FinancialYearStartMonth",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "GSTNumber",
                table: "AppConfigs");
        }
    }
}
