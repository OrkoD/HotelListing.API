using HotelListing.Api.Contracts;
using HotelListing.Api.Data;
using HotelListing.Api.DTOs.Hotel;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Services;

public class HotelsService(HotelListingDbContext context) : IHotelsService
{
    public async Task<IEnumerable<GetHotelDto>> GetHotelsAsync() =>
        await context.Hotels
            .Select(h => new GetHotelDto(h.Id, h.Name, h.Address, h.Rating, h.CountryId, h.Country!.ShortName))
            .ToListAsync();

    public async Task<GetHotelDto?> GetHotelAsync(int id) =>
        await context.Hotels
            .Where(h => h.Id == id)
            .Select(h => new GetHotelDto(h.Id, h.Name, h.Address, h.Rating, h.CountryId, h.Country!.ShortName))
            .SingleOrDefaultAsync();

    public async Task<GetHotelDto> CreateHotelAsync(CreateHotelDto hotelDto)
    {
        var country = await context.Countries.FindAsync(hotelDto.CountryId);

        var hotel = new Hotel
        {
            Name = hotelDto.Name,
            Address = hotelDto.Address,
            Rating = hotelDto.Rating,
            CountryId = hotelDto.CountryId
        };
        await context.Hotels.AddAsync(hotel);
        await context.SaveChangesAsync();

        return new GetHotelDto(
            hotel.Id,
            hotel.Name,
            hotel.Address,
            hotel.Rating,
            hotel.CountryId,
            country!.ShortName
        );
    }

    public async Task<bool> UpdateHotelAsync(int id, UpdateHotelDto hotel) =>
        await context.Hotels
            .Where(h => h.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Name, hotel.Name)
                .SetProperty(h => h.Address, hotel.Address)
                .SetProperty(h => h.Rating, hotel.Rating)
                .SetProperty(h => h.CountryId, hotel.CountryId)) > 0;

    public async Task<bool> DeleteHotelAsync(int id) =>
        await context.Hotels
            .Where(h => h.Id == id)
            .ExecuteDeleteAsync() > 0;
}
