using AutoMapper;
using AutoMapper.QueryableExtensions;
using BenchmarkDotNet.Attributes;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Application.MappingProfiles;
using HotelListing.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotelListing.Benchmarks;

[MemoryDiagnoser]
public class TrackingBenchmark
{
    private const string ConnectionString = "Server=localhost, 1433;Database=HotelListingDb;User Id=sa;Password=P@ssword123!;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False";

    private DbContextOptions<HotelListingDbContext> _options = null!;

    private IConfigurationProvider _mapperConfig = null!;

    [GlobalSetup]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<HotelListingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        _mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<HotelMappingProfile>(),
            NullLoggerFactory.Instance);
    }

    [Benchmark]
    public async Task<List<Hotel>> WithTracking()
    {
        using var db = new HotelListingDbContext(_options);

        return await db.Hotels
            .Take(50)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Hotel>> WithoutTracking()
    {
        using var db = new HotelListingDbContext(_options);

        return await db.Hotels
            .AsNoTracking()
            .Take(50)
            .ToListAsync();
    }
    [Benchmark]
    public async Task<List<GetHotelDto>> WithProjectTo()
    {
        using var db = new HotelListingDbContext(_options);

        return await db.Hotels
            .AsNoTracking()
            .Take(50)
            .ProjectTo<GetHotelDto>(_mapperConfig)
            .ToListAsync();
    }
}
