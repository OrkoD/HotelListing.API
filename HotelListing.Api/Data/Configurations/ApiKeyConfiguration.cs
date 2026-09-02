using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelListing.Api.Data.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasIndex(k => k.Key).IsUnique();

        builder.HasData(
            new ApiKey
            {
                Id = 1,
                AppName = "HotelListing",
                CreatedAtUts = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                Key = "AnP3NdcK49FsdDat45N2l4=",
            }
        );
    }
}
