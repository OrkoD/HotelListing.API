using HotelListing.Api.AuthorizationFilters;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Models.Filtering;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/hotels/{hotelId:int}/bookings")]
[Authorize]
public class HotelBookingsController(IBookingService bookingService) : ApiControllerBase
{
    [HttpGet("admin")]
    [HotelOrSystemAdmin]
    public async Task<ActionResult<PageResult<GetBookingDto>>> GetBookingsAdmin(
        [FromRoute] int hotelId,
        [FromQuery] PaginationParameters paginationParameters,
        [FromQuery] BookingFilterParameters filters
    ) =>
        ToActionResult(await bookingService.GetBookingsForHotelAsync(hotelId, paginationParameters, filters));

    [HttpGet]
    public async Task<ActionResult<PageResult<GetBookingDto>>> GetBookings(
        [FromRoute] int hotelId,
        [FromQuery] PaginationParameters paginationParameters,
        [FromQuery] BookingFilterParameters filters
    ) =>
        ToActionResult(await bookingService.GetUserBookingsForHotelAsync(hotelId, paginationParameters, filters));

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
