using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Parkin.Api.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace Parkin.IntegrationTests.AccessEventFeatures;

public class ScopeAccessEventIdempotencyMigrationTests : IClassFixture<PostgresFixture>
{
  private const string MigrationBeforeScoping = "20260925184155_AddSpacePlacementAndLotLayout";

  private readonly PostgresFixture _fixture;

  public ScopeAccessEventIdempotencyMigrationTests(PostgresFixture fixture) => _fixture = fixture;

  private AppDbContext CreateContext()
  {
    var connectionString = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
    {
      Database = "scope_idempotency_migration"
    }.ConnectionString;

    return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);
  }

  [Fact]
  public async Task Migration_BackfillsTheScopeFromTheStaffMemberOrTheAuditedApiKey()
  {
    var staffId = Guid.NewGuid();
    var apiKeyId = Guid.NewGuid();
    var manualEventId = Guid.NewGuid();
    var gateEventId = Guid.NewGuid();
    var orphanEventId = Guid.NewGuid();

    await using (var context = CreateContext())
    {
      await context.Database.EnsureDeletedAsync();
      await context.GetService<IMigrator>().MigrateAsync(MigrationBeforeScoping);

      await context.Database.ExecuteSqlAsync($"""
        INSERT INTO "AccessEvents" ("Id", "LotId", "RawPlate", "NormalizedPlate", "Direction", "Source",
          "Decision", "OccurredAt", "ReceivedAt", "IdempotencyKey", "ActingStaffId")
        VALUES
          ({manualEventId}, {Guid.NewGuid()}, 'AAA 111', 'AAA111', 'Enter', 'Manual', 'Allow', now(), now(), 'manual:k1', {staffId}),
          ({gateEventId}, {Guid.NewGuid()}, 'BBB 222', 'BBB222', 'Enter', 'Lpr', 'Allow', now(), now(), 'k1', NULL),
          ({orphanEventId}, {Guid.NewGuid()}, 'CCC 333', 'CCC333', 'Enter', 'Lpr', 'Allow', now(), now(), 'k2', NULL)
        """);

      await context.Database.ExecuteSqlAsync($"""
        INSERT INTO "AuditLogEntries" ("Id", "ActorType", "ActorId", "Action", "EntityType", "EntityId", "OccurredAt")
        VALUES ({Guid.NewGuid()}, 'Api', {apiKeyId}, 'access_event.ingested', 'AccessEvent', {gateEventId}, now())
        """);

      await context.Database.MigrateAsync();
    }

    await using var verify = CreateContext();
    var rows = await verify.AccessEvents
      .Select(e => new { Id = e.Id.Value, e.ActorId, e.IdempotencyKey })
      .ToDictionaryAsync(e => e.Id);

    rows[manualEventId].ActorId.ShouldBe(staffId);
    rows[manualEventId].IdempotencyKey.ShouldBe("k1");
    rows[gateEventId].ActorId.ShouldBe(apiKeyId);
    rows[gateEventId].IdempotencyKey.ShouldBe("k1");
    rows[orphanEventId].ActorId.ShouldBe(Guid.Empty);
  }
}
