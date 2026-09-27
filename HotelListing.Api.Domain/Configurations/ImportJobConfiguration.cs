using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelListing.Api.Domain.Configurations;

public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(j => j.StoredFileUri)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(j => j.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(j => j.FileExtension)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(j => j.Status)
            .HasDatabaseName("IX_ImportJobs_Status");

        builder.Property(j => j.CreatedAtUtc)
            .IsRequired();
    }
}
