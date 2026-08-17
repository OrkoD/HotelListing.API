using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Data;

public class HotelListingDbContext(DbContextOptions<HotelListingDbContext> options)
    : DbContext(options)
{
    public DbSet<Hotel> Hotels { get; set; }

    public DbSet<Country> Countries { get; set; }
}
