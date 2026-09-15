using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Domain;
using HotelListing.Api.Application.DTOs.Hotel;
using Microsoft.EntityFrameworkCore;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Models.Extensions;
using HotelListing.Api.Common.Models.Filtering;

namespace HotelListing.Api.Application.Services;

public class HotelsService(
    HotelListingDbContext db,
    ICountriesService countriesService,
    IMapper mapper) : IHotelsService
{
    public async Task<Result<PageResult<GetHotelDto>>> GetHotelsAsync(
        PaginationParameters paginationParameters,
        HotelFilterParameters filters
    )
    {
        var query = db.Hotels.AsNoTracking();

        if (filters.CountryId.HasValue)
            query = query.Where(h => h.CountryId == filters.CountryId);

        if (filters.MinRating.HasValue)
            query = query.Where(h => h.Rating >= filters.MinRating);

        if (filters.MaxRating.HasValue)
            query = query.Where(h => h.Rating <= filters.MaxRating);

        if (filters.MinPrice.HasValue)
            query = query.Where(h => h.PerNightRate >= filters.MinPrice);

        if (filters.MaxPrice.HasValue)
            query = query.Where(h => h.PerNightRate <= filters.MaxPrice);

        if (!string.IsNullOrWhiteSpace(filters.Location))
            query = query.Where(h => h.Address.Contains(filters.Location));

        if (!string.IsNullOrWhiteSpace(filters.Search))
            query = query.Where(h => h.Name.Contains(filters.Search) || h.Address.Contains(filters.Search));

        query = filters.SortBy?.ToLower() switch
        {
            "name" => filters.SortDescending ? query.OrderByDescending(h => h.Name) : query.OrderBy(h => h.Name),
            "rating" => filters.SortDescending ? query.OrderByDescending(h => h.Rating) : query.OrderBy(h => h.Rating),
            "price" => filters.SortDescending ? query.OrderByDescending(h => h.PerNightRate) : query.OrderBy(h => h.PerNightRate),
            "address" => filters.SortDescending ? query.OrderByDescending(h => h.Address) : query.OrderBy(h => h.Address),
            _ => query.OrderBy(h => h.Id)
        };

        var hotels = await query
            .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
            .ToPageResultAsync(paginationParameters);

        return Result<PageResult<GetHotelDto>>.Success(hotels);
    }

    public async Task<Result<GetHotelDto>> GetHotelAsync(int id)
    {
        var hotel = await db.Hotels
            .AsNoTracking()
            .Where(h => h.Id == id)
            .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
            .SingleOrDefaultAsync();

        return hotel is null
            ? Result<GetHotelDto>.NotFound($"Hotel '{id}' was not found.")
            : Result<GetHotelDto>.Success(hotel);
    }

    public async Task<Result<GetHotelDto>> CreateHotelAsync(CreateHotelDto hotelDto)
    {
        var countryExists = await countriesService.CountryExistsAsync(hotelDto.CountryId);

        if (!countryExists)
            return Result<GetHotelDto>.Failure(new Error(ErrorCodes.NotFound, $"Country '{hotelDto.CountryId}' was not found."));

        var duplicated = await HotelExistsAsync(hotelDto.Name, hotelDto.CountryId);
        if (duplicated)
            return Result<GetHotelDto>.Failure(new Error(ErrorCodes.Conflict, $"Hotel '{hotelDto.Name}' already exists in the selected country."));

        var hotel = mapper.Map<Hotel>(hotelDto);
        await db.Hotels.AddAsync(hotel);
        await db.SaveChangesAsync();

        var dto = await db.Hotels
            .AsNoTracking()
            .Where(h => h.Id == hotel.Id)
            .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
            .FirstAsync();

        return Result<GetHotelDto>.Success(dto);
    }

    public async Task<Result> UpdateHotelAsync(int id, UpdateHotelDto hotel)
    {
        if (id != hotel.Id)
            return Result.BadRequest(new Error(ErrorCodes.Validation, "Id route value doesn't match payload Id."));

        var countryExists = await countriesService.CountryExistsAsync(hotel.CountryId);

        if (!countryExists)
            return Result.Failure(new Error(ErrorCodes.NotFound, $"Country '{hotel.CountryId}' was not found."));

        var updated = await db.Hotels
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
        var deleted = await db.Hotels
            .Where(h => h.Id == id)
            .ExecuteDeleteAsync() > 0;

        return deleted
            ? Result.Success()
            : Result.NotFound($"Hotel with id '{id}' was not found.");
    }

    public async Task<bool> HotelExistsAsync(string name, int countryId)
    {
        return await db.Hotels.AnyAsync(h => h.Name == name && h.CountryId == countryId);
    }
}
