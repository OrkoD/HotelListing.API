using System.ComponentModel.DataAnnotations;

namespace HotelListing.Api.DTOs.Hotel;

public class CreateHotelDto
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(150)]
    public required string Address { get; set; }

    [Range(1, 5)]
    public double Rating { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid country")]
    public int CountryId { get; set; }
}
