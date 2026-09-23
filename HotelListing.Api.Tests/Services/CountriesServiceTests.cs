using AutoMapper;
using FluentAssertions;
using HotelListing.Api.Application.DTOs.Country;
using HotelListing.Api.Application.MappingProfiles;
using HotelListing.Api.Application.Services;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Models.Filtering;
using HotelListing.Api.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotelListing.Api.Tests.Services;

public class CountriesServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly HotelListingDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly CountriesService _sut;

    public CountriesServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<HotelListingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new HotelListingDbContext(dbOptions);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CountryMappingProfile>();
            cfg.AddProfile<HotelMappingProfile>();
        }, NullLoggerFactory.Instance);

        _mapper = mapperConfig.CreateMapper();

        _sut = new CountriesService(_dbContext, _mapper);
    }

    [Fact]
    public async Task GetCountriesAsync_NoFilters_ReturnsAllCountriesOrderedByName()
    {
        // Arrange
        await SeedCountryAsync("Poland", "PL");
        await SeedCountryAsync("Ukraine", "UA");

        // Act
        var result = await _sut.GetCountriesAsync(new CountryFilterParameters());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(c => c.Name).Should().ContainInOrder("Poland", "Ukraine");
    }

    [Fact]
    public async Task GetCountriesAsync_WithSearchFilter_ReturnsOnlyMatchingCountries()
    {
        // Arrange
        await SeedCountryAsync("Poland", "PL");
        await SeedCountryAsync("Ukraine", "UA");
        var filters = new CountryFilterParameters { Search = "Ukr" };

        // Act
        var result = await _sut.GetCountriesAsync(filters);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().ContainSingle().Which.Name.Should().Be("Ukraine");
    }

    [Fact]
    public async Task GetCountriesAsync_SortByShortNameDescending_ReturnsInDescendingOrder()
    {
        // Arrange
        await SeedCountryAsync("Ukraine", "UA");
        await SeedCountryAsync("Poland", "PL");
        var filters = new CountryFilterParameters { SortBy = "shortname", SortDescending = true };

        // Act
        var result = await _sut.GetCountriesAsync(filters);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(c => c.ShortName).Should().ContainInOrder("UA", "PL");
    }

    [Fact]
    public async Task GetCountryAsync_WhenExists_ReturnsCountry()
    {
        // Arrange
        var country = await SeedCountryAsync("Ukraine", "UA");

        // Act
        var result = await _sut.GetCountryAsync(country.CountryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(country.CountryId);
        result.Value.Name.Should().Be(country.Name);
        result.Value.ShortName.Should().Be(country.ShortName);
    }

    [Fact]
    public async Task GetCountriesAsync_WhenNoCountriesExist_ReturnsEmptyCollection()
    {
        // Act
        var result = await _sut.GetCountriesAsync(new CountryFilterParameters());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCountryAsync_WhenNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        var countryId = 1;

        // Act
        var result = await _sut.GetCountryAsync(countryId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Fact]
    public async Task CreateCountryAsync_WhenNameIsUnique_PersistsAndReturnsCountry()
    {
        // Arrange
        var countryDto = new CreateCountryDto { Name = "Ukraine", ShortName = "UA" };

        // Act
        var result = await _sut.CreateCountryAsync(countryDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be(countryDto.Name);

        (await _dbContext.Countries.AnyAsync(c => c.Name == countryDto.Name)).Should().BeTrue();
    }

    [Fact]
    public async Task CreateCountryAsync_WhenNameAlreadyExists_ReturnsConflict()
    {
        // Arrange
        await SeedCountryAsync("Germany", "DE");
        var dto = new CreateCountryDto { Name = "Germany", ShortName = "DE-2" };

        // Act
        var result = await _sut.CreateCountryAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);

        // Nothing extra was inserted on the failed path
        (await _dbContext.Countries.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateCountryAsync_WhenRouteIdDoesNotMatchBodyId_ReturnsBadRequest()
    {
        var country = await SeedCountryAsync("Ukraine", "UA");
        var dto = new UpdateCountryDto { Id = country.CountryId + 1, Name = "Poland", ShortName = "PL" };

        var result = await _sut.UpdateCountryAsync(country.CountryId, dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
    }

    [Fact]
    public async Task UpdateCountryAsync_WhenCountryDoesNotExist_ReturnsNotFound()
    {
        var dto = new UpdateCountryDto { Id = 999, Name = "Poland", ShortName = "PL" };

        var result = await _sut.UpdateCountryAsync(999, dto);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateCountryAsync_WhenValid_UpdatesPersistedCountry()
    {
        var country = await SeedCountryAsync("Ukrain", "UA"); // seeded with a typo on purpose
        var dto = new UpdateCountryDto { Id = country.CountryId, Name = "Ukraine", ShortName = "UA" };

        var result = await _sut.UpdateCountryAsync(country.CountryId, dto);

        result.IsSuccess.Should().BeTrue();

        // 1. Evict all cached entities so FindAsync is forced to query SQLite
        _dbContext.ChangeTracker.Clear();

        var updated = await _dbContext.Countries.FindAsync(country.CountryId);
        updated!.Name.Should().Be("Ukraine");
    }

    [Fact]
    public async Task DeleteCountryAsync_WhenExists_RemovesItAndReturnsSuccess()
    {
        var country = await SeedCountryAsync("Ukraine", "UA");

        var result = await _sut.DeleteCountryAsync(country.CountryId);

        result.IsSuccess.Should().BeTrue();
        (await _dbContext.Countries.AnyAsync(c => c.CountryId == country.CountryId)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCountryAsync_WhenNotFound_ReturnsNotFound()
    {
        var result = await _sut.DeleteCountryAsync(999);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Theory]
    [InlineData("Ukraine", true)]
    [InlineData("Atlantis", false)]
    public async Task CountryExistsAsync_ByName_ReturnsExpectedResult(string name, bool expected)
    {
        await SeedCountryAsync("Ukraine", "UA");

        var exists = await _sut.CountryExistsAsync(name);

        exists.Should().Be(expected);
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
    }
    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<Country> SeedCountryAsync(string name, string shortName)
    {
        var country = new Country { Name = name, ShortName = shortName };
        await _dbContext.Countries.AddAsync(country);
        await _dbContext.SaveChangesAsync();

        return country;
    }
}
