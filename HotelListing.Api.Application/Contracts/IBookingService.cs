using HotelListing.Api.Common.Results;
using HotelListing.Api.Application.DTOs.Booking;
using HotelListing.Api.Common.Models.Paging;

namespace HotelListing.Api.Application.Contracts;

public interface IBookingService
{
    Task<Result<PageResult<GetBookingDto>>> GetBookingsForHotelAsync(int hotelId, PaginationParameters paginationParameters);

    Task<Result<PageResult<GetBookingDto>>> GetUserBookingsForHotelAsync(int hotelId, PaginationParameters paginationParameters);

    Task<Result<GetBookingDto>> CreateBookingAsync(CreateBookingDto dto);

    Task<Result<GetBookingDto>> UpdateBookingAsync(int hotelId, int bookingId, UpdateBookingDto dto);

    Task<Result> CancelBookingAsync(int hotelId, int bookingId);

    Task<Result> AdminCancelBookingAsync(int hotelId, int bookingId);

    Task<Result> AdminConfirmBookingAsync(int hotelId, int bookingId);
}
