using System.ComponentModel.DataAnnotations;

namespace HotelListing.Api.DTOs.Country;

public class UpdateCountryDto : CreateCountryDto
{
    [Required]
    public required int Id { get; set; }
}
