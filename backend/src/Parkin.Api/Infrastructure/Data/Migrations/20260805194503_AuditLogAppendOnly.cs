using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parkin.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditLogAppendOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Append-only enforcement: the app connects with a single role for both migrations and
            // runtime queries, so revoke UPDATE/DELETE from whichever role is currently connected
            // (current_user) rather than hardcoding a role name that may differ per environment.
            // NOTE: in local/dev the app connects as the Postgres superuser (Username=postgres in
            // appsettings.json) and superusers bypass all GRANT/REVOKE checks, so this has no
            // observable effect locally. It is real defense-in-depth for a properly scoped
            // non-superuser application role in production.
            migrationBuilder.Sql(@"
DO $$
BEGIN
  EXECUTE format('REVOKE UPDATE, DELETE ON TABLE ""AuditLogEntries"" FROM %I', current_user);
END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
  EXECUTE format('GRANT UPDATE, DELETE ON TABLE ""AuditLogEntries"" TO %I', current_user);
END $$;");
        }
    }
}
