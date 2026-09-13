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

        // Prevent duplicate hotel names within the same country and optimize HotelExists check
        builder.HasIndex(h => new { h.CountryId, h.Name })
            .IsUnique();

        // Support sorting and filtering by Rating and Price
        builder.HasIndex(h => h.Rating);
        builder.HasIndex(h => h.PerNightRate);
    }
}
