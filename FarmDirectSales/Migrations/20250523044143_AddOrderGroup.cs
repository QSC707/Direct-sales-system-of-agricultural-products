using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmDirectSales.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderGroupId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderGroups",
                columns: table => new
                {
                    OrderGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GroupNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderCount = table.Column<int>(type: "int", nullable: false),
                    TotalProductAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShippingFeeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShippingAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiverName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveryAreaId = table.Column<int>(type: "int", nullable: true),
                    ShippingFeeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderGroups", x => x.OrderGroupId);
                    table.ForeignKey(
                        name: "FK_OrderGroups_ShippingFees_ShippingFeeId",
                        column: x => x.ShippingFeeId,
                        principalTable: "ShippingFees",
                        principalColumn: "ShippingFeeId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderGroups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderGroupId",
                table: "Orders",
                column: "OrderGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroups_ShippingFeeId",
                table: "OrderGroups",
                column: "ShippingFeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroups_UserId",
                table: "OrderGroups",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_OrderGroups_OrderGroupId",
                table: "Orders",
                column: "OrderGroupId",
                principalTable: "OrderGroups",
                principalColumn: "OrderGroupId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_OrderGroups_OrderGroupId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "OrderGroups");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderGroupId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderGroupId",
                table: "Orders");
        }
    }
}
