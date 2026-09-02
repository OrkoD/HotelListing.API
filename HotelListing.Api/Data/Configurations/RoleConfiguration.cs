using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelListing.Api.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        builder.HasData(
            new IdentityRole
            {
                Id = "4e3f4ba8-aea3-4a0c-92d6-048686e09153",
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "bd001357-fc51-4c0a-b385-408ca45b4efb"
            },
            new IdentityRole
            {
                Id = "5028f678-7dea-4ca8-a1a5-a800ce770106",
                Name = "User",
                NormalizedName = "USER",
                ConcurrencyStamp = "25cb677d-53ab-4e25-8983-6c0e58997592"
            }
        );
    }
}
