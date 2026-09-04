using System.ComponentModel.DataAnnotations;

namespace HotelListing.Api.DTOs.Booking;

public class CreateBookingDto
{
    [Required]
    public int HotelId { get; set; }
    [Required]
    public DateOnly CheckIn { get; set; }

    [Required]
    public DateOnly CheckOut { get; set; }

    [Required]
    [Range(1, 20, ErrorMessage = "Guests must be between 1 and 20.")]
    public int Guests { get; set; }
}

// The record is used by mentor
// public record CreateBookingDto(
//     int HotelId,
//     DateOnly CheckIn,
//     DateOnly CheckOut,
//     int Guests
// );
