using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelListing.Api.Domain.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.ShortName)
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(c => c.Name)
            .HasDatabaseName("IX_Countries_Name")
            .IsUnique();

        builder.HasIndex(c => c.ShortName)
            .HasDatabaseName("IX_Countries_ShortName")
            .IsUnique();
    }
}
