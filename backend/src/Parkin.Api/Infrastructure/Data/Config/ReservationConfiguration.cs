using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parkin.Api.Domain.ReservationAggregate;

namespace Parkin.Api.Infrastructure.Data.Config;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
  public void Configure(EntityTypeBuilder<Reservation> builder)
  {
    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenGuidIdValueGenerator<AppDbContext, Reservation, ReservationId>>()
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.SpaceId)
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.DriverId)
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.LotId)
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.Status)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();

    builder.HasIndex(entity => entity.SpaceId)
      .HasDatabaseName("ux_reservation_active_space")
      .IsUnique()
      .HasFilter("\"Status\" = 'Active'");

    builder.HasIndex(entity => new { entity.DriverId, entity.LotId })
      .HasDatabaseName("ux_reservation_active_driver_lot")
      .IsUnique()
      .HasFilter("\"Status\" = 'Active'");
  }
}
