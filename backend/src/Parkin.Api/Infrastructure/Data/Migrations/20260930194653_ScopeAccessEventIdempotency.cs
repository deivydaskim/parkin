using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkin.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ScopeAccessEventIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_access_event_idempotency",
                table: "AccessEvents");

            migrationBuilder.AddColumn<Guid>(
                name: "ActorId",
                table: "AccessEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(@"
UPDATE ""AccessEvents""
SET ""ActorId"" = ""ActingStaffId"",
    ""IdempotencyKey"" = CASE WHEN ""IdempotencyKey"" LIKE 'manual:%'
                             THEN substr(""IdempotencyKey"", 8)
                             ELSE ""IdempotencyKey"" END
WHERE ""ActingStaffId"" IS NOT NULL;

UPDATE ""AccessEvents"" e
SET ""ActorId"" = a.""ActorId""
FROM ""AuditLogEntries"" a
WHERE e.""ActingStaffId"" IS NULL
  AND a.""EntityType"" = 'AccessEvent'
  AND a.""Action"" = 'access_event.ingested'
  AND a.""EntityId"" = e.""Id""
  AND a.""ActorId"" IS NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "ux_access_event_idempotency",
                table: "AccessEvents",
                columns: new[] { "ActorId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_access_event_idempotency",
                table: "AccessEvents");

            migrationBuilder.Sql(@"
UPDATE ""AccessEvents""
SET ""IdempotencyKey"" = left('manual:' || ""IdempotencyKey"", 200)
WHERE ""ActingStaffId"" IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "AccessEvents");

            migrationBuilder.CreateIndex(
                name: "ux_access_event_idempotency",
                table: "AccessEvents",
                column: "IdempotencyKey",
                unique: true);
        }
    }
}
