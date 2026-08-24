using HotelListing.Api.DTOs.Country;
using HotelListing.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CountriesController(ICountriesService countriesService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetCountriesDto>>> GetCountries() =>
         Ok(await countriesService.GetCountriesAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetCountryDto>> GetCountry(int id)
    {
        var country = await countriesService.GetCountryAsync(id);

        return country is null
            ? NotFound()
            : country;
    }

    [HttpPost]
    public async Task<ActionResult<GetCountryDto>> CreateCountry(CreateCountryDto countryDto)
    {
        var resultDto = await countriesService.CreateCountryAsync(countryDto);

        return CreatedAtAction(nameof(GetCountry), new { id = resultDto.Id }, resultDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCountry(int id, UpdateCountryDto country)
    {
        if (id != country.Id)
            return BadRequest("The route ID and country ID must match.");

        return await countriesService.UpdateCountryAsync(id, country)
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCountry(int id) =>
        await countriesService.DeleteCountryAsync(id)
            ? NoContent()
            : NotFound();
}
