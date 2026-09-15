using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelListing.Api.Domain.Configurations;

public class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        builder.Property(h => h.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(h => h.Address)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(h => h.PerNightRate)
            .HasColumnType("decimal(18,2)");

        // 1. Data Integrity + Fast Duplicate Check (Unique composite)
        builder.HasIndex(h => new { h.CountryId, h.Name })
            .IsUnique()
            .HasDatabaseName("IX_Hotels_CountryId_Name_Unique");

        // 2. Optimized for GET /api/countries/{id}/hotels (Filtered/Sorted by rating)
        builder.HasIndex(h => new { h.CountryId, h.Rating })
            .HasDatabaseName("IX_Hotels_CountryId_Rating");

        // 3. Optimized for global Price filtering & sorting (MinPrice / MaxPrice / SortBy=price)
        builder.HasIndex(h => h.PerNightRate)
            .HasDatabaseName("IX_Hotels_PerNightRate");

        // 4. Optimized for global Name sorting / searching
        builder.HasIndex(h => h.Name)
            .HasDatabaseName("IX_Hotels_Name");
    }
}
