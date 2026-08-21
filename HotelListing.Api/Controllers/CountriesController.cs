using HotelListing.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CountriesController(HotelListingDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Country>>> GetAll() => await context.Countries.ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Country>> GetById(int id)
    {
        var country = await context.Countries.FindAsync(id);

        return country is null
            ? NotFound()
            : country;
    }

    [HttpPost]
    public async Task<ActionResult<Country>> Add(Country country)
    {
        await context.Countries.AddAsync(country);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = country.CountryId }, country);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Country country)
    {
        if (id != country.CountryId)
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