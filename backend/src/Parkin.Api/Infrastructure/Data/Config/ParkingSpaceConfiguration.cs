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

    builder.HasIndex(entity => new { entity.LotId, entity.Label })
      .IsUnique()
      .HasDatabaseName("ux_space_lot_label");
  }
}
