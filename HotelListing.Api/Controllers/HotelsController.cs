using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HotelListing.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
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
        {
            return BadRequest("The route ID and hotel ID must match.");
        }

        var existingHotel = await context.Hotels.FindAsync(id);

        if (existingHotel is null)
        {
            return NotFound();
        }

        context.Entry(existingHotel).CurrentValues.SetValues(hotel);
        await context.SaveChangesAsync();

        return NoContent();
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
