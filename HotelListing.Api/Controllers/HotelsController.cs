using Microsoft.AspNetCore.Mvc;
using HotelListing.Api.DTOs.Hotel;
using HotelListing.Api.Contracts;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelsController(IHotelsService hotelsService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetHotelDto>>> GetHotels() =>
        ToActionResult(await hotelsService.GetHotelsAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetHotelDto>> GetHotel(int id) =>
        ToActionResult(await hotelsService.GetHotelAsync(id));

    [HttpPost]
    public async Task<ActionResult<GetHotelDto>> CreateHotel(CreateHotelDto hotelDto) =>
        ToCreatedAtActionResult(await hotelsService.CreateHotelAsync(hotelDto), nameof(GetHotel), hotel => new { id = hotel.Id });

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateHotel(int id, UpdateHotelDto hotel) =>
        ToActionResult(await hotelsService.UpdateHotelAsync(id, hotel));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteHotel(int id) =>
        ToActionResult(await hotelsService.DeleteHotelAsync(id));
}
