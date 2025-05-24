using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmDirectSales.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmPhotosToFarmerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FarmPhoto1",
                table: "FarmerProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarmPhoto2",
                table: "FarmerProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarmPhoto3",
                table: "FarmerProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FarmPhoto1",
                table: "FarmerProfiles");

            migrationBuilder.DropColumn(
                name: "FarmPhoto2",
                table: "FarmerProfiles");

            migrationBuilder.DropColumn(
                name: "FarmPhoto3",
                table: "FarmerProfiles");
        }
    }
}
