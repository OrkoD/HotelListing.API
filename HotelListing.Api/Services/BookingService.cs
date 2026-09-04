using System.IdentityModel.Tokens.Jwt;
using HotelListing.Api.Constants;
using HotelListing.Api.Contracts;
using HotelListing.Api.Data;
using HotelListing.Api.Data.Enums;
using HotelListing.Api.DTOs.Booking;
using HotelListing.Api.Results;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Services;

public class BookingService(HotelListingDbContext db, IHttpContextAccessor httpContextAccessor) : IBookingService
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
            .Select(b => new GetBookingDto(
                b.Id,
                b.HotelId,
                b.Hotel!.Name,
                b.CheckIn,
                b.CheckOut,
                b.Guests,
                b.TotalPrice,
                b.Status.ToString(),
                b.CreatedAtUtc,
                b.UpdatedAtUtc
            ))
            .ToListAsync();

        return Result<IEnumerable<GetBookingDto>>.Success(bookings);
    }

    public async Task<Result<GetBookingDto>> CreateBookingAsync(CreateBookingDto dto)
    {
        var userId = httpContextAccessor?.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Validation, "User is required."));

        var nights = dto.CheckOut.DayNumber - dto.CheckIn.DayNumber;

        if (nights <= 0)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Validation, "Check-out must be after check-in."));

        if (dto.Guests <= 0)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Validation, "Guests must be at least one."));

        var overlaps = await db.Bookings
            .AnyAsync(b => b.HotelId == dto.HotelId
                && b.UserId == userId
                && b.Status != BookingStatus.Cancelled
                && dto.CheckIn < b.CheckOut
                && dto.CheckOut > b.CheckIn
            );

        if (overlaps)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Conflict, $"The selected dates overlap with an existing booking."));

        var hotel = await db.Hotels
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == dto.HotelId);

        if (hotel is null)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.NotFound, $"Hotel '{dto.HotelId}' was not found."));

        var totalPrice = hotel.PerNightRate * nights;

        var booking = new Booking
        {
            HotelId = dto.HotelId,
            UserId = userId,
            CheckIn = dto.CheckIn,
            CheckOut = dto.CheckOut,
            Guests = dto.Guests,
            TotalPrice = totalPrice,
            Status = BookingStatus.Pending
        };

        await db.Bookings.AddAsync(booking);
        await db.SaveChangesAsync();

        var created = new GetBookingDto(
            booking.Id,
            hotel.Id,
            hotel.Name,
            dto.CheckIn,
            dto.CheckOut,
            dto.Guests,
            totalPrice,
            BookingStatus.Pending.ToString(),
            booking.CreatedAtUtc,
            booking.UpdatedAtUtc
        );

        return Result<GetBookingDto>.Success(created);
    }

    public async Task<Result<GetBookingDto>> UpdateBookingAsync(int hotelId, int bookingId, UpdateBookingDto dto)
    {
        var userId = httpContextAccessor?.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Validation, "User is required."));

        var nights = dto.CheckOut.DayNumber - dto.CheckIn.DayNumber;

        if (nights <= 0)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Validation, "Check-out must be after check-in."));

        if (dto.Guests <= 0)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Validation, "Guests must be at least one."));

        var overlaps = await db.Bookings
            .AnyAsync(b => b.HotelId == hotelId
                && b.UserId == userId
                && b.Status != BookingStatus.Cancelled
                && dto.CheckIn < b.CheckOut
                && dto.CheckOut > b.CheckIn
            );

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

        var perNightRate = booking.Hotel!.PerNightRate;
        booking.CheckIn = dto.CheckIn;
        booking.CheckOut = dto.CheckOut;
        booking.Guests = dto.Guests;
        booking.TotalPrice = perNightRate * (dto.CheckOut.DayNumber - dto.CheckIn.DayNumber);
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var updated = new GetBookingDto(
            booking.Id,
            booking.HotelId,
            booking.Hotel.Name,
            booking.CheckIn,
            booking.CheckOut,
            booking.Guests,
            booking.TotalPrice,
            booking.Status.ToString(),
            booking.CreatedAtUtc,
            booking.UpdatedAtUtc
        );

        return Result<GetBookingDto>.Success(updated);
    }

    public async Task<Result> CancelBookingAsync(int hotelId, int bookingId)
    {
        var userId = httpContextAccessor?.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

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
}
