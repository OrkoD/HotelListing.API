using HotelListing.Api.AuthorizationFilters;
using HotelListing.Api.Contracts;
using HotelListing.Api.DTOs.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/hotels/{hotelId:int}/bookings")]
[Authorize]
public class HotelBookingsController(IBookingService bookingService) : ApiControllerBase
{
    [HttpGet("admin")]
    [HotelOrSystemAdmin]
    public async Task<ActionResult<IEnumerable<GetBookingDto>>> GetBookingsAdmin([FromRoute] int hotelId) =>
        ToActionResult(await bookingService.GetBookingsForHotelAsync(hotelId));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetBookingDto>>> GetBookings([FromRoute] int hotelId) =>
        ToActionResult(await bookingService.GetUserBookingsForHotelAsync(hotelId));

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
        ToActionResult(await bookingService.CancelBookingAsync(hotelId, bookingId));

    [HttpPut("{bookingId:int}/admin/cancel")]
    [HotelOrSystemAdmin]
    public async Task<IActionResult> AdminCancelBooking([FromRoute] int hotelId, [FromRoute] int bookingId) =>
        ToActionResult(await bookingService.AdminCancelBookingAsync(hotelId, bookingId));

    [HttpPut("{bookingId:int}/admin/confirm")]
    [HotelOrSystemAdmin]
    public async Task<IActionResult> AdminConfirmBooking([FromRoute] int hotelId, [FromRoute] int bookingId) =>
        ToActionResult(await bookingService.AdminConfirmBookingAsync(hotelId, bookingId));
}
