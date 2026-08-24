using System.ComponentModel.DataAnnotations;

namespace HotelListing.Api.DTOs.Hotel;

public class UpdateHotelDto : CreateHotelDto
{
    [Required]
    public required int Id { get; set; }
}
