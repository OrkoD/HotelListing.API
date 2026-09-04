using HotelListing.Api.Contracts;
using HotelListing.Api.DTOs.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/hotels/{hotelId:int}/bookings")]
public class HotelBookingsController(IBookingService bookingService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetBookingDto>>> GetBookings([FromRoute] int hotelId) =>
        ToActionResult(await bookingService.GetBookingsForHotelAsync(hotelId));

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<GetBookingDto>> CreateBooking([FromBody] CreateBookingDto bookingDto) =>
        ToActionResult(await bookingService.CreateBookingAsync(bookingDto));

    [HttpPut("{bookingId:int}")]
    public async Task<ActionResult<GetBookingDto>> UpdateBooking(
        [FromRoute] int hotelId,
        [FromRoute] int bookingId,
        [FromBody] UpdateBookingDto dto) =>
        ToActionResult(await bookingService.UpdateBookingAsync(hotelId, bookingId, dto));

    [HttpPut("{bookingId:int}/cancel")]
    public async Task<IActionResult> CancelBooking([FromRoute] int hotelId, [FromRoute] int bookingId) =>
        ToActionResult(await bookingService.CancelBookingAsync(hotelId,bookingId));
}
