using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HotelListing.Api.Data;

namespace HotelListing.Api;

[Route("api/[controller]")]
[ApiController]
public class HotelsController : ControllerBase
{
    private static readonly List<Hotel> Hotels = [
        new Hotel { Id = 1, Name = "Grand Plaza", Address = "123 Main St", Rating = 4.5 },
        new Hotel { Id = 2, Name = "Ocean View", Address = "456 Beach Rd", Rating = 4.8 },
    ];

    [HttpGet]
    public ActionResult<IEnumerable<Hotel>> GetAll() => Hotels;

    [HttpGet("{id:int}")]
    public ActionResult<Hotel> GetById(int id)
    {
        var hotel = Hotels.Find(h => h.Id == id);

        return hotel is null
            ? NotFound()
            : hotel;
    }

    [HttpPost]
    public ActionResult<Hotel> Add(Hotel hotel)
    {
        if (Hotels.Exists(h => h.Id == hotel.Id))
        {
            return Conflict($"Hotel with ID {hotel.Id} already exists.");
        }

        hotel.Id = hotel.Id != 0
            ? hotel.Id
            : Hotels.Count == 0 ? 1 : Hotels.Max(h => h.Id) + 1;

        Hotels.Add(hotel);

        return CreatedAtAction(nameof(GetById), new { id = hotel.Id }, hotel);
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, Hotel hotel)
    {
        if (id != hotel.Id)
        {
            return BadRequest("The route ID and hotel ID must match.");
        }

        var index = Hotels.FindIndex(h => h.Id == id);

        if (index == -1)
        {
            return NotFound();
        }

        Hotels[index] = hotel;

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var hotel = Hotels.Find(h => h.Id == id);

        if (hotel is null)
        {
            return NotFound();
        }

        Hotels.Remove(hotel);

        return NoContent();
    }
}
