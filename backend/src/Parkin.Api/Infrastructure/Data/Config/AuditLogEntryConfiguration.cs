using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Infrastructure.Data.Config;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
  public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
  {
    builder.Property(entity => entity.Id)
      .HasValueGenerator<VogenGuidIdValueGenerator<AppDbContext, AuditLogEntry, AuditLogEntryId>>()
      .HasVogenConversion()
      .IsRequired();

    builder.Property(entity => entity.ActorType)
      .HasConversion<string>()
      .HasMaxLength(20)
      .IsRequired();

    builder.Property(entity => entity.Action)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(entity => entity.EntityType)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(entity => entity.MetadataJson)
      .HasColumnType("jsonb");

    builder.HasIndex(entity => entity.OccurredAt)
      .HasDatabaseName("ix_audit_occurred_at")
      .IsDescending();

    builder.HasIndex(entity => new { entity.ActorType, entity.ActorId })
      .HasDatabaseName("ix_audit_actor");

    builder.HasIndex(entity => new { entity.EntityType, entity.EntityId })
      .HasDatabaseName("ix_audit_entity");
  }
}
