using HotelListing.Api.Common.Results;
using HotelListing.Api.Application.DTOs.Booking;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Models.Filtering;

namespace HotelListing.Api.Application.Contracts;

public interface IBookingService
{
    Task<Result<CursorPageResult<GetBookingDto>>> GetBookingsForHotelCursorAsync(
        int hotelId,
        CursorPaginationParameters parameters,
        BookingFilterParameters filters,
        CancellationToken cancellationToken = default
    );

    Task<Result<PageResult<GetBookingDto>>> GetBookingsForHotelAsync(
        int hotelId,
        PaginationParameters paginationParameters,
        BookingFilterParameters filters
    );

    Task<Result<PageResult<GetBookingDto>>> GetUserBookingsForHotelAsync(
        int hotelId,
        PaginationParameters paginationParameters,
        BookingFilterParameters filters
    );

    Task<Result<GetBookingDto>> CreateBookingAsync(CreateBookingDto dto);

    Task<Result<GetBookingDto>> UpdateBookingAsync(int hotelId, int bookingId, UpdateBookingDto dto);

    Task<Result> CancelBookingAsync(int hotelId, int bookingId);

    Task<Result> AdminCancelBookingAsync(int hotelId, int bookingId);

    Task<Result> AdminConfirmBookingAsync(int hotelId, int bookingId);
}
