using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAssistantPro.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleAppConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ;WITH Ranked AS
                (
                    SELECT Id,
                           ROW_NUMBER() OVER (
                               ORDER BY CASE WHEN IsInitialized = 1 THEN 0 ELSE 1 END, Id ASC) AS rn
                    FROM AppConfigs
                )
                DELETE FROM Ranked
                WHERE rn > 1;
                """);

            migrationBuilder.AddColumn<int>(
                name: "SingletonKey",
                table: "AppConfigs",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_AppConfigs_SingletonKey",
                table: "AppConfigs",
                column: "SingletonKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppConfigs_SingletonKey",
                table: "AppConfigs");

            migrationBuilder.DropColumn(
                name: "SingletonKey",
                table: "AppConfigs");
        }
    }
}
