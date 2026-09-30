using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Parkin.Api.Domain.ApiKeyAggregate;
using Parkin.Api.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace Parkin.IntegrationTests.AccessEventFeatures;

public class ScopeApiKeysToLotMigrationTests : IClassFixture<PostgresFixture>
{
  private const string MigrationBeforeScoping = "20260930194653_ScopeAccessEventIdempotency";

  private readonly PostgresFixture _fixture;

  public ScopeApiKeysToLotMigrationTests(PostgresFixture fixture) => _fixture = fixture;

  private AppDbContext CreateContext(string database)
  {
    var connectionString = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
    {
      Database = database
    }.ConnectionString;

    return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);
  }

  private static async Task InsertLotAsync(AppDbContext context, Guid lotId)
  {
    await context.Database.ExecuteSqlAsync($"""
      INSERT INTO "ParkingLots" ("Id", "Name", "Timezone", "AccessMode", "FullBehavior", "Status")
      VALUES ({lotId}, {$"Lot {lotId:N}"}, 'Europe/Vilnius', 'Open', 'Block', 'Active')
      """);
  }

  private static async Task InsertActiveKeyAsync(AppDbContext context, Guid keyId)
  {
    await context.Database.ExecuteSqlAsync($"""
      INSERT INTO "ApiKeys" ("Id", "Name", "KeyHash", "Prefix", "Status", "CreatedAt")
      VALUES ({keyId}, {$"Key {keyId:N}"}, {keyId.ToString("N")}, 'pk_test', 'Active', now())
      """);
  }

  [Fact]
  public async Task Migration_WithASingleLot_BackfillsExistingKeysAndKeepsThemActive()
  {
    var lotId = Guid.NewGuid();
    var keyId = Guid.NewGuid();

    await using (var context = CreateContext("scope_api_keys_single_lot"))
    {
      await context.Database.EnsureDeletedAsync();
      await context.GetService<IMigrator>().MigrateAsync(MigrationBeforeScoping);
      await InsertLotAsync(context, lotId);
      await InsertActiveKeyAsync(context, keyId);

      await context.Database.MigrateAsync();
    }

    await using var verify = CreateContext("scope_api_keys_single_lot");
    var key = await verify.ApiKeys.SingleAsync(k => k.Id == ApiKeyId.From(keyId));
    key.LotId.Value.ShouldBe(lotId);
    key.Status.ShouldBe(ApiKeyStatus.Active);
  }

  [Fact]
  public async Task Migration_WithSeveralLots_RevokesExistingKeys()
  {
    var keyId = Guid.NewGuid();

    await using (var context = CreateContext("scope_api_keys_many_lots"))
    {
      await context.Database.EnsureDeletedAsync();
      await context.GetService<IMigrator>().MigrateAsync(MigrationBeforeScoping);
      await InsertLotAsync(context, Guid.NewGuid());
      await InsertLotAsync(context, Guid.NewGuid());
      await InsertActiveKeyAsync(context, keyId);

      await context.Database.MigrateAsync();
    }

    await using var verify = CreateContext("scope_api_keys_many_lots");
    var key = await verify.ApiKeys.SingleAsync(k => k.Id == ApiKeyId.From(keyId));
    key.Status.ShouldBe(ApiKeyStatus.Revoked);
    key.RevokedAt.ShouldNotBeNull();
  }

  [Fact]
  public async Task Migration_WithNoLots_RemovesKeysThatCannotBeBound()
  {
    await using (var context = CreateContext("scope_api_keys_no_lots"))
    {
      await context.Database.EnsureDeletedAsync();
      await context.GetService<IMigrator>().MigrateAsync(MigrationBeforeScoping);
      await InsertActiveKeyAsync(context, Guid.NewGuid());

      await context.Database.MigrateAsync();
    }

    await using var verify = CreateContext("scope_api_keys_no_lots");
    (await verify.ApiKeys.CountAsync()).ShouldBe(0);
  }

  [Fact]
  public async Task ApiKey_ForAMissingLot_ViolatesTheForeignKey()
  {
    await using var context = CreateContext("scope_api_keys_fk");
    await context.Database.EnsureDeletedAsync();
    await context.Database.MigrateAsync();

    var insert = () => context.Database.ExecuteSqlAsync($"""
      INSERT INTO "ApiKeys" ("Id", "Name", "KeyHash", "Prefix", "Status", "CreatedAt", "LotId")
      VALUES ({Guid.NewGuid()}, 'Orphan', {Guid.NewGuid().ToString("N")}, 'pk_test', 'Active', now(), {Guid.NewGuid()})
      """);

    var exception = await Should.ThrowAsync<PostgresException>(insert);
    exception.SqlState.ShouldBe(PostgresErrorCodes.ForeignKeyViolation);
  }
}
