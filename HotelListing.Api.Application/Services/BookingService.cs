using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Domain;
using HotelListing.Api.Application.DTOs.Booking;
using Microsoft.EntityFrameworkCore;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Models.Extensions;
using HotelListing.Api.Common.Enums;
using HotelListing.Api.Common.Models.Filtering;

namespace HotelListing.Api.Application.Services;

public class BookingService(HotelListingDbContext db, IUsersService usersService, IMapper mapper) : IBookingService
{
    public async Task<Result<CursorPageResult<GetBookingDto>>> GetBookingsForHotelCursorAsync(
        int hotelId,
        CursorPaginationParameters parameters,
        BookingFilterParameters filters,
        CancellationToken cancellationToken = default
    )
    {
        if (!await HotelExistsAsync(hotelId, cancellationToken))
            return Result<CursorPageResult<GetBookingDto>>.NotFound($"Hotel {hotelId} was not found.");

        var query = ApplyFilters(hotelId, filters);

        var bookings = await query
            .ToCursorResultAsync(
                parameters,
                q => q.ProjectTo<GetBookingDto>(mapper.ConfigurationProvider),
                cancellationToken
            );

        return Result<CursorPageResult<GetBookingDto>>.Success(bookings);
    }

    public async Task<Result<PageResult<GetBookingDto>>> GetBookingsForHotelAsync(
        int hotelId,
        PaginationParameters paginationParameters,
        BookingFilterParameters filters
    )
    {
        if (!await HotelExistsAsync(hotelId))
            return Result<PageResult<GetBookingDto>>.NotFound($"Hotel {hotelId} was not found.");

        var query = ApplyFilters(hotelId, filters);

        var bookings = await query
            .ProjectTo<GetBookingDto>(mapper.ConfigurationProvider)
            .ToPageResultAsync(paginationParameters);

        return Result<PageResult<GetBookingDto>>.Success(bookings);
    }

    public async Task<Result<PageResult<GetBookingDto>>> GetUserBookingsForHotelAsync(
        int hotelId,
        PaginationParameters paginationParameters,
        BookingFilterParameters filters
    )
    {
        var userId = usersService.UserId;

        if (!await HotelExistsAsync(hotelId))
            return Result<PageResult<GetBookingDto>>.NotFound($"Hotel {hotelId} was not found.");

        var query = ApplyFilters(hotelId, filters);

        var bookings = await query
            .Where(b => b.UserId == userId)
            .ProjectTo<GetBookingDto>(mapper.ConfigurationProvider)
            .ToPageResultAsync(paginationParameters);

        return Result<PageResult<GetBookingDto>>.Success(bookings);
    }

    public async Task<Result<GetBookingDto>> CreateBookingAsync(CreateBookingDto dto)
    {
        var userId = usersService.UserId;

        if (string.IsNullOrEmpty(userId))
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Validation, "User is required."));

        var overlaps = await IsOverlap(dto.HotelId, userId, dto.CheckIn, dto.CheckOut);

        if (overlaps)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Conflict, $"The selected dates overlap with an existing booking."));

        var hotel = await db.Hotels
            .FirstOrDefaultAsync(h => h.Id == dto.HotelId);

        if (hotel is null)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.NotFound, $"Hotel '{dto.HotelId}' was not found."));

        var nights = dto.CheckOut.DayNumber - dto.CheckIn.DayNumber;
        var totalPrice = hotel.PerNightRate * nights;

        var booking = mapper.Map<Booking>(dto);
        booking.UserId = userId;
        booking.TotalPrice = totalPrice;
        booking.Hotel = hotel;

        await db.Bookings.AddAsync(booking);
        await db.SaveChangesAsync();

        var created = mapper.Map<GetBookingDto>(booking);

        return Result<GetBookingDto>.Success(created);
    }

    public async Task<Result<GetBookingDto>> UpdateBookingAsync(int hotelId, int bookingId, UpdateBookingDto dto)
    {
        var userId = usersService.UserId;

        if (string.IsNullOrEmpty(userId))
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Validation, "User is required."));

        var overlaps = await IsOverlap(hotelId, userId, dto.CheckIn, dto.CheckOut, bookingId);

        if (overlaps)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Conflict, $"The selected dates overlap with an existing booking."));

        var booking = await db.Bookings
            .Include(b => b.Hotel)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.HotelId == hotelId && b.UserId == userId);

        if (booking is null)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.NotFound, $"Booking '{bookingId}' was not found."));

        if (booking.Status == BookingStatus.Cancelled)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Conflict, $"Canceled bookings cannot be modified."));

        mapper.Map(dto, booking);

        var nights = dto.CheckOut.DayNumber - dto.CheckIn.DayNumber;
        booking.TotalPrice = booking.Hotel!.PerNightRate * nights;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var updated = mapper.Map<GetBookingDto>(booking);

        return Result<GetBookingDto>.Success(updated);
    }

    public async Task<Result> CancelBookingAsync(int hotelId, int bookingId)
    {
        var userId = usersService.UserId;

        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.HotelId == hotelId && b.UserId == userId);

        if (booking is null)
            return Result.Failure(new Error(ErrorCodes.NotFound, $"Booking '{bookingId}' was not found."));

        if (booking.Status == BookingStatus.Cancelled)
            return Result.Failure(new Error(ErrorCodes.Conflict, $"This booking has already been canceled."));

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> AdminCancelBookingAsync(int hotelId, int bookingId)
    {
        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.HotelId == hotelId);

        if (booking is null)
            return Result.Failure(new Error(ErrorCodes.NotFound, $"Booking '{bookingId}' was not found."));

        if (booking.Status == BookingStatus.Cancelled)
            return Result.Failure(new Error(ErrorCodes.Conflict, $"This booking has already been canceled."));

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> AdminConfirmBookingAsync(int hotelId, int bookingId)
    {
        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.HotelId == hotelId);

        if (booking is null)
            return Result.Failure(new Error(ErrorCodes.NotFound, $"Booking '{bookingId}' was not found."));

        if (booking.Status == BookingStatus.Confirmed)
            return Result.Failure(new Error(ErrorCodes.Conflict, $"This booking has already been confirmed."));

        booking.Status = BookingStatus.Confirmed;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Result.Success();
    }

    private async Task<bool> IsOverlap(int hotelId, string userId, DateOnly checkIn, DateOnly checkOut, int? excludeBookingId = null)
    {
        return await db.Bookings
            .AnyAsync(b =>
                b.HotelId == hotelId
                && (excludeBookingId == null || b.Id != excludeBookingId)
                && b.UserId == userId
                && b.Status != BookingStatus.Cancelled
                && checkIn < b.CheckOut
                && checkOut > b.CheckIn
            );
    }

    private IQueryable<Booking> ApplyFilters(int hotelId, BookingFilterParameters filters)
    {
        var query = db.Bookings.Where(b => b.HotelId == hotelId);

        if (filters.Status.HasValue)
            query = query.Where(b => b.Status == filters.Status);

        if (filters.CheckInFrom.HasValue)
            query = query.Where(b => b.CheckIn >= filters.CheckInFrom);

        if (filters.CheckInTo.HasValue)
            query = query.Where(b => b.CheckIn <= filters.CheckInTo);

        if (filters.CheckOutFrom.HasValue)
            query = query.Where(b => b.CheckOut >= filters.CheckOutFrom);

        if (filters.CheckOutTo.HasValue)
            query = query.Where(b => b.CheckOut <= filters.CheckOutTo);

        if (filters.MinPrice.HasValue)
            query = query.Where(b => b.TotalPrice >= filters.MinPrice);

        if (filters.MaxPrice.HasValue)
            query = query.Where(b => b.TotalPrice <= filters.MaxPrice);

        if (filters.MinGuests.HasValue)
            query = query.Where(b => b.Guests >= filters.MinGuests);

        if (filters.MaxGuests.HasValue)
            query = query.Where(b => b.Guests <= filters.MaxGuests);

        if (filters.CreatedAfter.HasValue)
            query = query.Where(b => b.CreatedAtUtc >= filters.CreatedAfter);

        if (filters.CreatedBefore.HasValue)
            query = query.Where(b => b.CreatedAtUtc <= filters.CreatedBefore);

        query = filters.SortBy?.ToLower() switch
        {
            "checkin" => filters.SortDescending ? query.OrderByDescending(b => b.CheckIn) : query.OrderBy(b => b.CheckIn),
            "checkout" => filters.SortDescending ? query.OrderByDescending(b => b.CheckOut) : query.OrderBy(b => b.CheckOut),
            "price" => filters.SortDescending ? query.OrderByDescending(b => b.TotalPrice) : query.OrderBy(b => b.TotalPrice),
            "created" => filters.SortDescending ? query.OrderByDescending(b => b.CreatedAtUtc) : query.OrderBy(b => b.CreatedAtUtc),
            _ => query.OrderBy(b => b.CheckIn)
        };

        return query;
    }

    private Task<bool> HotelExistsAsync(int hotelId, CancellationToken cancellationToken = default) =>
        db.Hotels.AnyAsync(h => h.Id == hotelId, cancellationToken);
}
