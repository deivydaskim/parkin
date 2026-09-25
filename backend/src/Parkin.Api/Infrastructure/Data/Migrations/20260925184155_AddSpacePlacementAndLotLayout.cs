using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkin.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpacePlacementAndLotLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Zone",
                table: "ParkingSpaces",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "placement_length",
                table: "ParkingSpaces",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "placement_level",
                table: "ParkingSpaces",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "placement_rotation_degrees",
                table: "ParkingSpaces",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "placement_width",
                table: "ParkingSpaces",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "placement_x",
                table: "ParkingSpaces",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "placement_y",
                table: "ParkingSpaces",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "layout_length_meters",
                table: "ParkingLots",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "layout_level_count",
                table: "ParkingLots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "layout_width_meters",
                table: "ParkingLots",
                type: "numeric(8,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Zone",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "placement_length",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "placement_level",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "placement_rotation_degrees",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "placement_width",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "placement_x",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "placement_y",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "layout_length_meters",
                table: "ParkingLots");

            migrationBuilder.DropColumn(
                name: "layout_level_count",
                table: "ParkingLots");

            migrationBuilder.DropColumn(
                name: "layout_width_meters",
                table: "ParkingLots");
        }
    }
}
