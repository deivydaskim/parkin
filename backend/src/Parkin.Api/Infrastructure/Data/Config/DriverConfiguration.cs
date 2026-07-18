using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parkin.Api.Domain.DriverAggregate;

namespace Parkin.Api.Infrastructure.Data.Config;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
  public void Configure(EntityTypeBuilder<Driver> builder)
  {
    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenGuidIdValueGenerator<AppDbContext, Driver, DriverId>>()
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.Name)
      .HasMaxLength(200)
      .IsRequired();

    builder.Property(entity => entity.Contact)
      .HasMaxLength(300);

    builder.Property(entity => entity.Status)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();

    builder.HasMany(entity => entity.Plates)
      .WithOne()
      .HasForeignKey(plate => plate.DriverId);

    builder.Metadata.FindNavigation(nameof(Driver.Plates))!
      .SetPropertyAccessMode(PropertyAccessMode.Field);
  }
}
