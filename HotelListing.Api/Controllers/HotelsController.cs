using Microsoft.AspNetCore.Mvc;
using HotelListing.Api.DTOs.Hotel;
using HotelListing.Api.Contracts;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelsController(IHotelsService hotelsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetHotelDto>>> GetHotels() =>
        Ok(await hotelsService.GetHotelsAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetHotelDto>> GetHotel(int id)
    {
        var hotel = await hotelsService.GetHotelAsync(id);

        return hotel is null
            ? NotFound()
            : hotel;
    }

    [HttpPost]
    public async Task<ActionResult<GetHotelDto>> CreateHotel(CreateHotelDto hotelDto)
    {
        var resultDto = await hotelsService.CreateHotelAsync(hotelDto);

        return CreatedAtAction(nameof(GetHotel), new { id = resultDto.Id }, resultDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateHotel(int id, UpdateHotelDto hotel)
    {
        if (id != hotel.Id)
            return BadRequest("The route ID and hotel ID must match.");

        var updated = await hotelsService.UpdateHotelAsync(id, hotel);

        return updated
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteHotel(int id)
    {
        var deleted = await hotelsService.DeleteHotelAsync(id);

        return deleted
            ? NoContent()
            : NotFound();
    }
}
