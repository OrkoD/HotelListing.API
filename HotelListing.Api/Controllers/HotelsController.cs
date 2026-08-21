using Microsoft.AspNetCore.Mvc;
using HotelListing.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelsController(HotelListingDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Hotel>>> GetAll() => await context.Hotels.ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Hotel>> GetById(int id)
    {
        var hotel = await context.Hotels.FindAsync(id);

        return hotel is null
            ? NotFound()
            : hotel;
    }

    [HttpPost]
    public async Task<ActionResult<Hotel>> Add(Hotel hotel)
    {
        await context.Hotels.AddAsync(hotel);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = hotel.Id }, hotel);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Hotel hotel)
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

    // [HttpPut("{id:int}")]
    // public async Task<IActionResult> Update(int id, Hotel hotel)
    // {
    //     if (id != hotel.Id)
    //     {
    //         return BadRequest("The route ID and hotel ID must match.");
    //     }

    //     var existingHotel = await context.Hotels.FindAsync(id);

    //     if (existingHotel is null)
    //     {
    //         return NotFound();
    //     }

    //     context.Entry(existingHotel).CurrentValues.SetValues(hotel);
    //     await context.SaveChangesAsync();

    //     return NoContent();
    // }

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
