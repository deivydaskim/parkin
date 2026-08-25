using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkin.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessEventAndParkingSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawPlate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NormalizedPlate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MatchedPlateId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchedDriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DenyReason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActingStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    OverrideOf = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParkingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    Plate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Pool = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntryEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExitEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExitTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_access_event_idempotency",
                table: "AccessEvents",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_session_active_lot_plate",
                table: "ParkingSessions",
                columns: new[] { "LotId", "Plate" },
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_session_active_lot_pool",
                table: "ParkingSessions",
                columns: new[] { "LotId", "Pool" },
                filter: "\"Status\" = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessEvents");

            migrationBuilder.DropTable(
                name: "ParkingSessions");
        }
    }
}
