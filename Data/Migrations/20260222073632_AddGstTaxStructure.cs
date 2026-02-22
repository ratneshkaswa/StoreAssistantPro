using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreAssistantPro.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGstTaxStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HSNCode",
                table: "Products",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTaxInclusive",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TaxProfileId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaxMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxProfileItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxProfileId = table.Column<int>(type: "int", nullable: false),
                    TaxMasterId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxProfileItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxProfileItems_TaxMasters_TaxMasterId",
                        column: x => x.TaxMasterId,
                        principalTable: "TaxMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxProfileItems_TaxProfiles_TaxProfileId",
                        column: x => x.TaxProfileId,
                        principalTable: "TaxProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_HSNCode",
                table: "Products",
                column: "HSNCode");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TaxProfileId",
                table: "Products",
                column: "TaxProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxMasters_IsActive",
                table: "TaxMasters",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaxMasters_TaxName",
                table: "TaxMasters",
                column: "TaxName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxProfileItems_TaxMasterId",
                table: "TaxProfileItems",
                column: "TaxMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxProfileItems_TaxProfileId_TaxMasterId",
                table: "TaxProfileItems",
                columns: new[] { "TaxProfileId", "TaxMasterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxProfiles_IsActive",
                table: "TaxProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaxProfiles_ProfileName",
                table: "TaxProfiles",
                column: "ProfileName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_TaxProfiles_TaxProfileId",
                table: "Products",
                column: "TaxProfileId",
                principalTable: "TaxProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_TaxProfiles_TaxProfileId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "TaxProfileItems");

            migrationBuilder.DropTable(
                name: "TaxMasters");

            migrationBuilder.DropTable(
                name: "TaxProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Products_HSNCode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TaxProfileId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HSNCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsTaxInclusive",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TaxProfileId",
                table: "Products");
        }
    }
}
