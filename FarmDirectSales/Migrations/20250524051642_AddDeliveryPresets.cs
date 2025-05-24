using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmDirectSales.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryPresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryPresets",
                columns: table => new
                {
                    PresetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerId = table.Column<int>(type: "int", nullable: false),
                    PresetName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeliveryInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryContact = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeliveryPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EstimatedDeliveryTime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsSystemDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsUserDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryPresets", x => x.PresetId);
                    table.ForeignKey(
                        name: "FK_DeliveryPresets_Users_FarmerId",
                        column: x => x.FarmerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPresets_FarmerId",
                table: "DeliveryPresets",
                column: "FarmerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryPresets");
        }
    }
}
