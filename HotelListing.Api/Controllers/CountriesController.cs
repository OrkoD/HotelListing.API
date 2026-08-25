using HotelListing.Api.DTOs.Country;
using HotelListing.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using HotelListing.Api.Results;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CountriesController(ICountriesService countriesService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetCountriesDto>>> GetCountries() =>
        ToActionResult(await countriesService.GetCountriesAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetCountryDto>> GetCountry(int id) =>
        ToActionResult(await countriesService.GetCountryAsync(id));

    [HttpPost]
    public async Task<ActionResult<GetCountryDto>> CreateCountry(CreateCountryDto countryDto) =>
        ToCreatedAtActionResult(await countriesService.CreateCountryAsync(countryDto), nameof(GetCountry), country => new { id = country.Id });

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCountry(int id, UpdateCountryDto country) =>
        ToActionResult(await countriesService.UpdateCountryAsync(id, country));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCountry(int id) =>
        ToActionResult(await countriesService.DeleteCountryAsync(id));
}
