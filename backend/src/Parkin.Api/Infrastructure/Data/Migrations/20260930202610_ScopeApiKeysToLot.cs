using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkin.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ScopeApiKeysToLot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LotId",
                table: "ApiKeys",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "ApiKeys"
                SET "LotId" = (SELECT "Id" FROM "ParkingLots" ORDER BY "Id" LIMIT 1)
                """);

            migrationBuilder.Sql("""
                UPDATE "ApiKeys"
                SET "Status" = 'Revoked', "RevokedAt" = now()
                WHERE "Status" = 'Active' AND (SELECT count(*) FROM "ParkingLots") <> 1
                """);

            migrationBuilder.Sql("""DELETE FROM "ApiKeys" WHERE "LotId" IS NULL""");

            migrationBuilder.AlterColumn<Guid>(
                name: "LotId",
                table: "ApiKeys",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_LotId",
                table: "ApiKeys",
                column: "LotId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_ParkingLots_LotId",
                table: "ApiKeys",
                column: "LotId",
                principalTable: "ParkingLots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_ParkingLots_LotId",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_LotId",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "LotId",
                table: "ApiKeys");
        }
    }
}
