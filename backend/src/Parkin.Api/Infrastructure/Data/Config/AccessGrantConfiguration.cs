using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parkin.Api.Domain.AccessGrantAggregate;

namespace Parkin.Api.Infrastructure.Data.Config;

public class AccessGrantConfiguration : IEntityTypeConfiguration<AccessGrant>
{
  public void Configure(EntityTypeBuilder<AccessGrant> builder)
  {
    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenGuidIdValueGenerator<AppDbContext, AccessGrant, AccessGrantId>>()
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.DriverId)
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.ParkingLotId)
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.Status)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.ValidFrom)
      .IsRequired();

    builder.HasIndex(entity => new { entity.DriverId, entity.ParkingLotId, entity.Status })
      .HasDatabaseName("ix_grant_driver_lot_status")
      .IsUnique()
      .HasFilter("\"Status\" = 'Active'");
  }
}
