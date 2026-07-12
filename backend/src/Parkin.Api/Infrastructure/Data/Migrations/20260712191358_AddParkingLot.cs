using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkin.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingLot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingLots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccessMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FullBehavior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingLots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_lot_name",
                table: "ParkingLots",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingLots");
        }
    }
}
