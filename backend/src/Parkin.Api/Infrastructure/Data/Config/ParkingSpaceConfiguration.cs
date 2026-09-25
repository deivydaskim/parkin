using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Infrastructure.Data.Config;

public class ParkingSpaceConfiguration : IEntityTypeConfiguration<ParkingSpace>
{
  public void Configure(EntityTypeBuilder<ParkingSpace> builder)
  {
    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenGuidIdValueGenerator<AppDbContext, ParkingSpace, ParkingSpaceId>>()
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.LotId)
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.Label)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(entity => entity.Type)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.Status)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.Zone)
      .HasMaxLength(ParkingSpace.ZoneMaxLength);

    builder.OwnsOne(entity => entity.Placement, placement =>
    {
      placement.Property(p => p.X).HasColumnName("placement_x").HasColumnType("numeric(8,2)");
      placement.Property(p => p.Y).HasColumnName("placement_y").HasColumnType("numeric(8,2)");
      placement.Property(p => p.RotationDegrees).HasColumnName("placement_rotation_degrees").HasColumnType("numeric(5,2)");
      placement.Property(p => p.Level).HasColumnName("placement_level");
      placement.Property(p => p.Width).HasColumnName("placement_width").HasColumnType("numeric(8,2)");
      placement.Property(p => p.Length).HasColumnName("placement_length").HasColumnType("numeric(8,2)");
    });

    builder.HasIndex(entity => new { entity.LotId, entity.Label })
      .IsUnique()
      .HasDatabaseName("ux_space_lot_label");
  }
}
