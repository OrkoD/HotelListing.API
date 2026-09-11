using Microsoft.AspNetCore.Mvc;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Models.Paging;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HotelsController(IHotelsService hotelsService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResult<GetHotelDto>>> GetHotels(
        [FromQuery] PaginationParameters paginationParameters
    ) =>
        ToActionResult(await hotelsService.GetHotelsAsync(paginationParameters));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetHotelDto>> GetHotel(int id) =>
        ToActionResult(await hotelsService.GetHotelAsync(id));

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<ActionResult<GetHotelDto>> CreateHotel(CreateHotelDto hotelDto) =>
        ToCreatedAtActionResult(await hotelsService.CreateHotelAsync(hotelDto), nameof(GetHotel), hotel => new { id = hotel.Id });

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> UpdateHotel(int id, UpdateHotelDto hotel) =>
        ToActionResult(await hotelsService.UpdateHotelAsync(id, hotel));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> DeleteHotel(int id) =>
        ToActionResult(await hotelsService.DeleteHotelAsync(id));
}
