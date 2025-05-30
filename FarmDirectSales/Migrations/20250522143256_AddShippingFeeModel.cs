using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmDirectSales.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingFeeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFee",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ShippingFeeId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShippingFees",
                columns: table => new
                {
                    ShippingFeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryAreaId = table.Column<int>(type: "int", nullable: true),
                    BaseFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FreeShippingThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExtraFeePerKg = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingFees", x => x.ShippingFeeId);
                    table.ForeignKey(
                        name: "FK_ShippingFees_DeliveryAreas_DeliveryAreaId",
                        column: x => x.DeliveryAreaId,
                        principalTable: "DeliveryAreas",
                        principalColumn: "DeliveryAreaId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingFeeId",
                table: "Orders",
                column: "ShippingFeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingFees_DeliveryAreaId",
                table: "ShippingFees",
                column: "DeliveryAreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ShippingFees_ShippingFeeId",
                table: "Orders",
                column: "ShippingFeeId",
                principalTable: "ShippingFees",
                principalColumn: "ShippingFeeId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ShippingFees_ShippingFeeId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "ShippingFees");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShippingFeeId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingFee",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingFeeId",
                table: "Orders");
        }
    }
}
