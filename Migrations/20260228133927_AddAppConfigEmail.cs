using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAssistantPro.Migrations
{
    /// <inheritdoc />
    public partial class AddAppConfigEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "AppConfigs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "AppConfigs");
        }
    }
}
