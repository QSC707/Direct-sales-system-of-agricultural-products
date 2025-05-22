using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmDirectSales.Migrations
{
    /// <inheritdoc />
    public partial class AddIsNationwideToDeliveryArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNationwide",
                table: "DeliveryAreas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNationwide",
                table: "DeliveryAreas");
        }
    }
}
