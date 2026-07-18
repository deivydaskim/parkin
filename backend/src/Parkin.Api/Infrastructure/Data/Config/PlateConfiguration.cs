using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Infrastructure.Data.Config;

public class PlateConfiguration : IEntityTypeConfiguration<Plate>
{
  public void Configure(EntityTypeBuilder<Plate> builder)
  {
    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenGuidIdValueGenerator<AppDbContext, Plate, PlateId>>()
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.DriverId)
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.NormalizedPlateNumber)
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.Status)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();

    builder.HasIndex(entity => entity.NormalizedPlateNumber)
      .IsUnique()
      .HasDatabaseName("ux_plate_normalized");
  }
}
