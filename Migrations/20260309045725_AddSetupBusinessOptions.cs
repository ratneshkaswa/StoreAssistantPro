using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAssistantPro.Migrations
{
    /// <inheritdoc />
    public partial class AddSetupBusinessOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NegativeStockAllowed",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NumberToWordsLanguage",
                table: "SystemSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RoundingMethod",
                table: "SystemSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CompositionSchemeRate",
                table: "AppConfigs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GstRegistrationType",
                table: "AppConfigs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                table: "AppConfigs",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NegativeStockAllowed",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "NumberToWordsLanguage",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "RoundingMethod",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "CompositionSchemeRate",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "GstRegistrationType",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "StateCode",
                table: "AppConfigs");
        }
    }
}
