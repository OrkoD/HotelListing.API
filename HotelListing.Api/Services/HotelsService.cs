using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Contracts;
using HotelListing.Api.Data;
using HotelListing.Api.DTOs.Hotel;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Services;

public class HotelsService(HotelListingDbContext context, IMapper mapper) : IHotelsService
{
    public async Task<IEnumerable<GetHotelDto>> GetHotelsAsync() =>
        await context.Hotels
            .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
            .ToListAsync();

    public async Task<GetHotelDto?> GetHotelAsync(int id) =>
        await context.Hotels
            .Where(h => h.Id == id)
            .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
            .SingleOrDefaultAsync();

    public async Task<GetHotelDto> CreateHotelAsync(CreateHotelDto hotelDto)
    {
        var hotel = mapper.Map<Hotel>(hotelDto);

        await context.Hotels.AddAsync(hotel);
        await context.SaveChangesAsync();

        return mapper.Map<GetHotelDto>(hotel);
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
