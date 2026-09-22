using FluentAssertions;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Country;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Models.Filtering;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace HotelListing.Api.Tests.Controllers;

public class CountriesControllerTests
{
    // SUT = System Under Test
    private readonly CountriesController _sut;
    private readonly ICountriesService _countriesService = Substitute.For<ICountriesService>();

    public CountriesControllerTests()
    {
        _sut = new CountriesController(_countriesService);
    }

    [Fact]
    public async Task GetCountry_WhenCountryExists_ReturnsOkWithCountry()
    {
        // Arrange
        var countryId = 1;
        var countryDto = new GetCountryDto(countryId, "Ukraine", "UA", null);

        // Tell our mock: When GetCountryAsync(1) is called, return Success
        _countriesService.GetCountryAsync(countryId)
            .Returns(Result<GetCountryDto>.Success(countryDto));

        // Act
        var response = await _sut.GetCountry(countryId);

        // Assert
        var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCountry = okResult.Value.Should().BeOfType<GetCountryDto>().Subject;

        returnedCountry.Should().BeEquivalentTo(countryDto);

        // Verify the service was called exactly once with this ID
        await _countriesService.Received(1).GetCountryAsync(countryId);
    }

    [Fact]
    public async Task GetCountry_WhenCountryDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var countryId = 000;

        _countriesService.GetCountryAsync(countryId)
            .Returns(Result<GetCountryDto>.NotFound($"Country '{countryId}' was not found."));

        // Act
        var response = await _sut.GetCountry(countryId);

        // Asset
        response.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetCountries_WhenCountriesExist_ReturnsOkWithCountries()
    {
        // Arrange
        var countries = new List<GetCountriesDto>
        {
            new GetCountriesDto(1, "Ukraine", "UA"),
            new GetCountriesDto(2, "Poland", "PL")
        };
        var filters = new CountryFilterParameters();

        _countriesService.GetCountriesAsync(Arg.Any<CountryFilterParameters>())
            .Returns(Result<IEnumerable<GetCountriesDto>>.Success(countries));

        // Act
        var response = await _sut.GetCountries(filters);

        // Assert
        var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

        okResult.Value.Should().BeEquivalentTo(countries);
        await _countriesService.Received(1).GetCountriesAsync(filters);
    }

    [Fact]
    public async Task CreateCountry_WhenValid_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateCountryDto { Name = "Ukraine", ShortName = "uA" };
        var createdDto = new GetCountryDto(1, "Ukraine", "UA", null);

        _countriesService.CreateCountryAsync(createDto)
            .Returns(Result<GetCountryDto>.Success(createdDto));

        // Act
        var response = await _sut.CreateCountry(createDto);

        // Assert
        var createdResult = response.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(CountriesController.GetCountry));
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(createdDto.Id);
        createdResult.Value.Should().BeEquivalentTo(createdDto);
    }

    [Fact]
    public async Task CreateCountry_WhenValidationFails_ReturnsBadRequest()
    {
        // 1. Arrange
        var createDto = new CreateCountryDto { Name = "", ShortName = "" };
        var error = new Error(ErrorCodes.Validation, "Name is required.");

        _countriesService.CreateCountryAsync(createDto)
            .Returns(Result<GetCountryDto>.Failure(error));

        // 2. Act
        var response = await _sut.CreateCountry(createDto);

        // 3. Assert
        var badRequestResult = response.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problemDetails = badRequestResult.Value.Should().BeAssignableTo<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Validation failed");
        problemDetails.Detail.Should().Contain("Name is required.");
    }

    [Fact]
    public async Task DeleteCountry_WhenCountryExists_ReturnsNoContent()
    {
        // Arrange
        var countryId = 1;

        _countriesService.DeleteCountryAsync(countryId).Returns(Result.Success());

        // Act
        var response = await _sut.DeleteCountry(countryId);

        // Assert
        var noContentResult = response.Should().BeOfType<NoContentResult>().Subject;
        noContentResult.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        await _countriesService.Received(1).DeleteCountryAsync(countryId);
    }
}
