using Microsoft.AspNetCore.Mvc;
using HotelListing.Api.Data;
using Microsoft.EntityFrameworkCore;
using HotelListing.Api.DTOs.Hotel;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelsController(HotelListingDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetHotelsDto>>> GetAll() =>
        await context.Hotels
            .Select(h => new GetHotelsDto(h.Id, h.Name, h.Address, h.Rating, h.CountryId))
            .ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetHotelDto>> GetById(int id)
    {
        var hotel = await context.Hotels
            .Where(h => h.Id == id)
            .Select(h => new GetHotelDto(h.Id, h.Name, h.Address, h.Rating, h.Country!.ShortName))
            .SingleOrDefaultAsync();

        return hotel is null
            ? NotFound()
            : hotel;
    }

    [HttpPost]
    public async Task<ActionResult<GetHotelDto>> Add(CreateHotelDto hotelDto)
    {
        var country = await context.Countries.FindAsync(hotelDto.CountryId);
        if (country is null)
            return BadRequest("Invalid CountryId.");

        var hotel = new Hotel
        {
            Name = hotelDto.Name,
            Address = hotelDto.Address,
            Rating = hotelDto.Rating,
            CountryId = hotelDto.CountryId
        };
        await context.Hotels.AddAsync(hotel);
        await context.SaveChangesAsync();

        var resultDto = new GetHotelDto(
            hotel.Id,
            hotel.Name,
            hotel.Address,
            hotel.Rating,
            country.ShortName
        );

        return CreatedAtAction(nameof(GetById), new { id = resultDto.Id }, resultDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateHotelDto hotel)
    {
        if (id != hotel.Id)
            return BadRequest("The route ID and hotel ID must match.");

        var updated = await context.Hotels
                .Where(h => h.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(h => h.Name, hotel.Name)
                    .SetProperty(h => h.Address, hotel.Address)
                    .SetProperty(h => h.Rating, hotel.Rating)
                    .SetProperty(h => h.CountryId, hotel.CountryId));

        return updated > 0
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await context.Hotels
            .Where(h => h.Id == id)
            .ExecuteDeleteAsync();

        return deleted > 0
            ? NoContent()
            : NotFound();
    }
}
