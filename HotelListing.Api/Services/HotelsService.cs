using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Constants;
using HotelListing.Api.Contracts;
using HotelListing.Api.Data;
using HotelListing.Api.DTOs.Hotel;
using HotelListing.Api.Results;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Services;

public class HotelsService(HotelListingDbContext context, IMapper mapper) : IHotelsService
{
    public async Task<Result<IEnumerable<GetHotelDto>>> GetHotelsAsync()
    {
        var hotels = await context.Hotels
            .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
            .ToListAsync();

        return Result<IEnumerable<GetHotelDto>>.Success(hotels);
    }

    public async Task<Result<GetHotelDto>> GetHotelAsync(int id)
    {
        var hotel = await context.Hotels
            .Where(h => h.Id == id)
            .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
            .SingleOrDefaultAsync();

        return hotel is null
            ? Result<GetHotelDto>.NotFound()
            : Result<GetHotelDto>.Success(hotel);
    }

    public async Task<Result<GetHotelDto>> CreateHotelAsync(CreateHotelDto hotelDto)
    {
        try
        {
            var hotel = mapper.Map<Hotel>(hotelDto);

            await context.Hotels.AddAsync(hotel);
            await context.SaveChangesAsync();

            var dto = mapper.Map<GetHotelDto>(hotel);

            return Result<GetHotelDto>.Success(dto);
        }
        catch (Exception)
        {
            return Result<GetHotelDto>.Failure();
        }
    }

    public async Task<Result> UpdateHotelAsync(int id, UpdateHotelDto hotel)
    {
        if (id != hotel.Id)
            return Result.BadRequest(new Error(ErrorCodes.Validation, "Id route value doesn't match payload Id."));

        var updated = await context.Hotels
            .Where(h => h.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Name, hotel.Name)
                .SetProperty(h => h.Address, hotel.Address)
                .SetProperty(h => h.Rating, hotel.Rating)
                .SetProperty(h => h.CountryId, hotel.CountryId)) > 0;

        return updated
            ? Result.Success()
            : Result.NotFound($"Hotel with id '{id}' was not found.");
    }

    public async Task<Result> DeleteHotelAsync(int id)
    {
        var deleted = await context.Hotels
            .Where(h => h.Id == id)
            .ExecuteDeleteAsync() > 0;

        return deleted
            ? Result.Success()
            : Result.NotFound($"Hotel with id '{id}' was not found.");
    }
}
