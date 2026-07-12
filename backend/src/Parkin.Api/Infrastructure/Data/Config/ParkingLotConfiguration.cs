using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Infrastructure.Data.Config;

public class ParkingLotConfiguration : IEntityTypeConfiguration<ParkingLot>
{
  public void Configure(EntityTypeBuilder<ParkingLot> builder)
  {
    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenGuidIdValueGenerator<AppDbContext, ParkingLot, ParkingLotId>>()
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.Name)
      .HasMaxLength(200)
      .IsRequired();

    builder.HasIndex(entity => entity.Name)
      .IsUnique()
      .HasDatabaseName("ux_lot_name");

    builder.Property(entity => entity.Address)
      .HasMaxLength(300);

    builder.Property(entity => entity.Timezone)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(entity => entity.AccessMode)
      .HasConversion(
        mode => mode == AccessMode.Restricted ? "RESTRICTED" : "OPEN",
        value => value == "RESTRICTED" ? AccessMode.Restricted : AccessMode.Open)
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.FullBehavior)
      .HasConversion(
        behavior => behavior == FullBehavior.AllowOverflow ? "ALLOW_OVERFLOW" : "BLOCK",
        value => value == "ALLOW_OVERFLOW" ? FullBehavior.AllowOverflow : FullBehavior.Block)
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.Status)
      .HasConversion(
        status => status == LotStatus.Archived ? "ARCHIVED" : "ACTIVE",
        value => value == "ARCHIVED" ? LotStatus.Archived : LotStatus.Active)
      .HasMaxLength(20)
      .IsRequired();
  }
}
