using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parkin.Api.Domain.AccessEventAggregate;

namespace Parkin.Api.Infrastructure.Data.Config;

public class AccessEventConfiguration : IEntityTypeConfiguration<AccessEvent>
{
  public void Configure(EntityTypeBuilder<AccessEvent> builder)
  {
    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenGuidIdValueGenerator<AppDbContext, AccessEvent, AccessEventId>>()
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.LotId)
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.RawPlate)
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.NormalizedPlate)
      .HasMaxLength(20)
      .IsRequired();

    // Nullable ID columns cannot use HasVogenConversion() - see ParkingSessionConfiguration.
    builder.Property(entity => entity.MatchedPlateId)
      .HasConversion<VogenEfCoreConverters.PlateIdEfCoreValueConverter,
        VogenEfCoreConverters.PlateIdEfCoreValueComparer>();

    builder.Property(entity => entity.MatchedDriverId)
      .HasConversion<VogenEfCoreConverters.DriverIdEfCoreValueConverter,
        VogenEfCoreConverters.DriverIdEfCoreValueComparer>();

    builder.Property(entity => entity.SessionId)
      .HasConversion<VogenEfCoreConverters.ParkingSessionIdEfCoreValueConverter,
        VogenEfCoreConverters.ParkingSessionIdEfCoreValueComparer>();

    builder.Property(entity => entity.OverrideOf)
      .HasConversion<VogenEfCoreConverters.AccessEventIdEfCoreValueConverter,
        VogenEfCoreConverters.AccessEventIdEfCoreValueComparer>();

    builder.Property(entity => entity.Direction)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.Source)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.Decision)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.DenyReason)
      .HasConversion<string>()
      .HasMaxLength(30);

    builder.Property(entity => entity.IdempotencyKey)
      .HasMaxLength(200)
      .IsRequired();

    builder.HasIndex(entity => entity.IdempotencyKey)
      .HasDatabaseName("ux_access_event_idempotency")
      .IsUnique();
  }
}
