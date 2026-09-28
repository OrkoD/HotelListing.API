using System.Text;
using FluentAssertions;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Application.Services.Parsers;
using HotelListing.Api.Common.Results;

namespace HotelListing.Api.Tests.Parsers;

public class CsvHotelParserTests
{
    private readonly CsvHotelParser _sut = new();

    [Fact]
    public void SupportedFormat_ShouldBeCsv()
    {
        _sut.Format.Should().Be(SupportedImportFormat.Csv);
    }

    [Fact]
    public async Task ParseAsync_WhenValidCsvStream_YieldsAllSuccessfulRows()
    {
        // Arrange
        var csvContent = """
            Name,Address,Rating,PerNightRate,Country
            Grand Hotel,123 Ocean Blvd,4.5,199.99,JM
            Royal Palace,789 Palm Way,4.8,320.00,Jamaica
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var results = new List<Result<HotelImportRowDto>>();
        await foreach (var row in _sut.ParseAsync(stream))
        {
            results.Add(row);
        }

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(x => x.IsSuccess);

        var first = results[0].Value!;
        first.RowNumber.Should().Be(2);
        first.Name.Should().Be("Grand Hotel");
        first.Address.Should().Be("123 Ocean Blvd");
        first.Rating.Should().Be(4.5);
        first.PerNightRate.Should().Be(199.99m);
        first.CountryIdentifier.Should().Be("JM");
    }

    [Fact]
    public async Task ParseAsync_WhenRowHasMalformedNumber_YieldsFailureForThatRowOnly()
    {
        // Arrange: Row 3 has bad rating, but Row 2 and Row 4 are valid
        var csvContent = """
            Name,Address,Rating,PerNightRate,Country
            Grand Hotel,123 Ocean Blvd,4.5,199.99,JM
            Sunset Resort,456 Beach Road,not_a_number,250.00,JM
            Royal Palace,789 Palm Way,4.8,320.00,JM
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var results = new List<Result<HotelImportRowDto>>();
        await foreach (var row in _sut.ParseAsync(stream))
        {
            results.Add(row);
        }

        // Assert: 3 total rows yielded
        results.Should().HaveCount(3);
        results[0].IsSuccess.Should().BeTrue();

        // Row 3 is a failure with descriptive error (Row 1 was header)
        results[1].IsSuccess.Should().BeFalse();
        results[1].Errors[0].Description.Should().Contain("Row 3");

        // Row 4 still succeeds!
        results[2].IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ParseAsync_WhenAddressHasCommasInsideQuotes_ParsesCorrectlyWithoutSplittingColumns()
    {
        // Arrange: Address has commas inside quotes! A simple string.Split(',') would fail here.
        var csvContent = """
            Name,Address,Rating,PerNightRate,Country
            Grand Hotel,"123, Ocean Blvd, Apt 4B",4.5,199.99,JM
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var results = new List<Result<HotelImportRowDto>>();
        await foreach (var row in _sut.ParseAsync(stream))
        {
            results.Add(row);
        }

        // Assert
        results.Should().HaveCount(1);
        results[0].IsSuccess.Should().BeTrue();
        results[0].Value!.Address.Should().Be("123, Ocean Blvd, Apt 4B");
    }
}
