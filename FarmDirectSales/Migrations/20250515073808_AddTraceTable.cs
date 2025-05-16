using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmDirectSales.Migrations
{
    /// <inheritdoc />
    public partial class AddTraceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Traces",
                columns: table => new
                {
                    TraceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SourcePlace = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlantingMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlantingTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HarvestTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QualityLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsOrganic = table.Column<bool>(type: "bit", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Traces", x => x.TraceId);
                    table.ForeignKey(
                        name: "FK_Traces_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Traces_ProductId",
                table: "Traces",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Traces");
        }
    }
}
