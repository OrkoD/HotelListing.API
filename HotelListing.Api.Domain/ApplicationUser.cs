using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace HotelListing.Api.Domain;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    public ICollection<HotelAdmin> Admins { get; set; } = [];

    public ICollection<Booking> Bookings { get; set; } = [];
}
