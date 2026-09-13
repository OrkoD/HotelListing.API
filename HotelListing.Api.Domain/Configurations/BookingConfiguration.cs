using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelListing.Api.Domain.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.TotalPrice)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(b => b.UserId);
        builder.HasIndex(b => b.HotelId);
        builder.HasIndex(b => new { b.CheckIn, b.CheckOut });
        builder.HasIndex(b => new { b.HotelId, b.CheckIn });
        builder.HasIndex(b => new { b.HotelId, b.UserId });
    }
}
