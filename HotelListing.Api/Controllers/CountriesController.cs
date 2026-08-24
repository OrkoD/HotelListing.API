using HotelListing.Api.Data;
using HotelListing.Api.DTOs.Country;
using HotelListing.Api.DTOs.Hotel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CountriesController(HotelListingDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetCountriesDto>>> GetAll() =>
        await context.Countries
            .Select(c => new GetCountriesDto(c.CountryId, c.Name, c.ShortName))
            .ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetCountryDto>> GetById(int id)
    {
        var country = await context.Countries
            .Where(c => c.CountryId == id)
            .Select(c => new GetCountryDto(
                c.CountryId,
                c.Name,
                c.ShortName,
                c.Hotels.Select(h => new GetHotelSlimDto(
                    h.Id,
                    h.Name,
                    h.Address,
                    h.Rating
                )).ToList()
            ))
            .SingleOrDefaultAsync();

        return country is null
            ? NotFound()
            : country;
    }

    [HttpPost]
    public async Task<ActionResult<GetCountryDto>> Add(CreateCountryDto countryDto)
    {
        var country = new Country
        {
            Name = countryDto.Name,
            ShortName = countryDto.ShortName
        };
        await context.Countries.AddAsync(country);
        await context.SaveChangesAsync();

        var resultDto = new GetCountryDto(
            country.CountryId,
            country.Name,
            country.ShortName,
            []
        );

        return CreatedAtAction(nameof(GetById), new { id = resultDto.Id }, resultDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCountryDto country)
    {
        if (id != country.Id)
            return BadRequest("The route ID and country ID must match.");

        var updated = await context.Countries
                .Where(c => c.CountryId == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Name, country.Name)
                    .SetProperty(c => c.ShortName, country.ShortName));

        return updated > 0
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await context.Countries
            .Where(c => c.CountryId == id)
            .ExecuteDeleteAsync();

        return deleted > 0
            ? NoContent()
            : NotFound();
    }
}