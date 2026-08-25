using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parkin.Api.Domain.ParkingSessionAggregate;

namespace Parkin.Api.Infrastructure.Data.Config;

public class ParkingSessionConfiguration : IEntityTypeConfiguration<ParkingSession>
{
  public void Configure(EntityTypeBuilder<ParkingSession> builder)
  {
    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenGuidIdValueGenerator<AppDbContext, ParkingSession, ParkingSessionId>>()
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.LotId)
      .HasVogenConversion()
      .IsRequired();

    // Vogen only generates HasVogenConversion() for PropertyBuilder<TId>, so nullable ID columns
    // name the generated converter/comparer pair directly.
    builder.Property(entity => entity.DriverId)
      .HasConversion<VogenEfCoreConverters.DriverIdEfCoreValueConverter,
        VogenEfCoreConverters.DriverIdEfCoreValueComparer>();

    builder.Property(entity => entity.Plate)
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.SpaceId)
      .HasConversion<VogenEfCoreConverters.ParkingSpaceIdEfCoreValueConverter,
        VogenEfCoreConverters.ParkingSpaceIdEfCoreValueComparer>();

    builder.Property(entity => entity.Pool)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.EntryEventId)
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.ExitEventId)
      .HasConversion<VogenEfCoreConverters.AccessEventIdEfCoreValueConverter,
        VogenEfCoreConverters.AccessEventIdEfCoreValueComparer>();

    builder.Property(entity => entity.Status)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();

    builder.HasIndex(entity => new { entity.LotId, entity.Pool })
      .HasDatabaseName("ix_session_active_lot_pool")
      .HasFilter("\"Status\" = 'Active'");

    builder.HasIndex(entity => new { entity.LotId, entity.Plate })
      .HasDatabaseName("ix_session_active_lot_plate")
      .HasFilter("\"Status\" = 'Active'");
  }
}
