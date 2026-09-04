using HotelListing.Api.Data;
using HotelListing.Api.Data.Enums;
using HotelListing.Api.DTOs.Hotel;

namespace HotelListing.Api.DTOs.Booking;

public record GetBookingDto(
    int Id,
    int HotelId,
    string HotelName,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Guests,
    decimal TotalPrice,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
