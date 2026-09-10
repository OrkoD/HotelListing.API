using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Contracts;
using HotelListing.Api.Domain;
using HotelListing.Api.Domain.Enums;
using HotelListing.Api.DTOs.Booking;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Services;

public class BookingService(HotelListingDbContext db, IUsersService usersService, IMapper mapper) : IBookingService
{
    public async Task<Result<IEnumerable<GetBookingDto>>> GetBookingsForHotelAsync(int hotelId)
    {
        var hotelExists = await db.Hotels.AnyAsync(h => h.Id == hotelId);

        if (!hotelExists)
            return Result<IEnumerable<GetBookingDto>>
                .Failure(new Error(ErrorCodes.NotFound, $"Hotel {hotelId} was not found."));

        var bookings = await db.Bookings
            .AsNoTracking()
            .Where(b => b.HotelId == hotelId)
            .OrderBy(b => b.CheckIn)
            .ProjectTo<GetBookingDto>(mapper.ConfigurationProvider)
            .ToListAsync();

        return Result<IEnumerable<GetBookingDto>>.Success(bookings);
    }

    public async Task<Result<IEnumerable<GetBookingDto>>> GetUserBookingsForHotelAsync(int hotelId)
    {
        var userId = usersService.UserId;
        var hotelExists = await db.Hotels.AnyAsync(h => h.Id == hotelId);

        if (!hotelExists)
            return Result<IEnumerable<GetBookingDto>>
                .Failure(new Error(ErrorCodes.NotFound, $"Hotel {hotelId} was not found."));

        var bookings = await db.Bookings
            .AsNoTracking()
            .Where(b => b.HotelId == hotelId && b.UserId == userId)
            .OrderBy(b => b.CheckIn)
            .ProjectTo<GetBookingDto>(mapper.ConfigurationProvider)
            .ToListAsync();

        return Result<IEnumerable<GetBookingDto>>.Success(bookings);
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
}
