using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parkin.Api.Domain.ApiKeyAggregate;

namespace Parkin.Api.Infrastructure.Data.Config;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
  public void Configure(EntityTypeBuilder<ApiKey> builder)
  {
    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenGuidIdValueGenerator<AppDbContext, ApiKey, ApiKeyId>>()
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.Name)
      .HasMaxLength(200)
      .IsRequired();

    builder.Property(entity => entity.KeyHash)
      .HasMaxLength(64)
      .IsRequired();

    builder.HasIndex(entity => entity.KeyHash)
      .IsUnique();

    builder.Property(entity => entity.Prefix)
      .HasMaxLength(16)
      .IsRequired();

    builder.Property(entity => entity.Status)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();
  }
}
