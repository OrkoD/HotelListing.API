using HotelListing.Api.DTOs.Country;
using HotelListing.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CountriesController(ICountriesService countriesService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetCountriesDto>>> GetCountries() =>
        ToActionResult(await countriesService.GetCountriesAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetCountryDto>> GetCountry(int id) =>
        ToActionResult(await countriesService.GetCountryAsync(id));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GetCountryDto>> CreateCountry(CreateCountryDto countryDto) =>
        ToCreatedAtActionResult(await countriesService.CreateCountryAsync(countryDto), nameof(GetCountry), country => new { id = country.Id });

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCountry(int id, UpdateCountryDto country) =>
        ToActionResult(await countriesService.UpdateCountryAsync(id, country));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCountry(int id) =>
        ToActionResult(await countriesService.DeleteCountryAsync(id));
}
