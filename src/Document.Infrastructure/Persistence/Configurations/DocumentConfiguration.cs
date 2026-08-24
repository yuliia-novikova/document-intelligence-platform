using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Domain.Entities.Document>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.OriginalFileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(d => d.ContentType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(d => d.StorageKey)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(d => d.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(d => d.ProcessedAtUtc)
            .HasColumnType("timestamptz");

        builder.Property(d => d.FailureReason)
            .HasMaxLength(2000);

        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.CreatedAtUtc);
    }
}
