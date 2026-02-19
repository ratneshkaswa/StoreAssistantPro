using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAssistantPro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFirmAddressAndPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "AppConfigs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "AppConfigs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "AppConfigs");
        }
    }
}
