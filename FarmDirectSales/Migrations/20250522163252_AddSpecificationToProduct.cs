using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmDirectSales.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecificationToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Specification",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Specification",
                table: "Products");
        }
    }
}
