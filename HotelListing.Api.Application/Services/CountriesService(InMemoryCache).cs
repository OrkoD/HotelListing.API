using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Domain;
using HotelListing.Api.Application.DTOs.Country;
using Microsoft.EntityFrameworkCore;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Models.Extensions;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Models.Filtering;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Caching.Memory;

namespace HotelListing.Api.Application.Services;

public class CountriesServiceInMemoryCache(HotelListingDbContext db, IMapper mapper, IMemoryCache cache) : ICountriesService
{
    private const string CountryListCacheName = "countries_list_";
    private const string CountrySingleCacheName = "country_";

    public async Task<Result<IEnumerable<GetCountriesDto>>> GetCountriesAsync(CountryFilterParameters filters)
    {
        var searchTerm = filters?.Search?.Trim().ToLowerInvariant() ?? string.Empty;
        var cacheKey = $"{CountryListCacheName}{searchTerm}";

        if (!cache.TryGetValue(cacheKey, out IEnumerable<GetCountriesDto>? countries))
        {
            var query = db.Countries.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filters?.Search))
            {
                var term = filters.Search.Trim();
                query = query.Where(c => c.Name.Contains(term) || c.ShortName.Contains(term));
            }

            // TODO: commented out for now
            // query = filters.SortBy?.ToLower() switch
            // {
            //     "name" => filters.SortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            //     "shortname" => filters.SortDescending ? query.OrderByDescending(c => c.ShortName) : query.OrderBy(c => c.ShortName),
            //     _ => query.OrderBy(c => c.Name)
            // };

            countries = await query
                .ProjectTo<GetCountriesDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(TimeSpan.FromDays(1));

            cache.Set(cacheKey, countries, cacheOptions);
        }

        countries ??= [];

        return Result<IEnumerable<GetCountriesDto>>.Success(countries);
    }

    public async Task<Result<GetCountryHotelsDto>> GetCountryHotelsAsync(
        int countryId,
        PaginationParameters paginationParameters,
        HotelFilterParameters filters
    )
    {
        var exists = await CountryExistsAsync(countryId);

        if (!exists)
            return Result<GetCountryHotelsDto>.NotFound($"Country '{countryId}' was not found.");

        var countryName = await db.Countries
            .AsNoTracking()
            .Where(c => c.CountryId == countryId)
            .Select(c => c.Name)
            .SingleAsync();

        var hotelsQuery = db.Hotels
            .AsNoTracking()
            .Where(h => h.CountryId == countryId);

        if (filters.MinRating.HasValue)
            hotelsQuery = hotelsQuery.Where(h => h.Rating >= filters.MinRating);

        if (filters.MaxRating.HasValue)
            hotelsQuery = hotelsQuery.Where(h => h.Rating <= filters.MaxRating);

        if (filters.MinPrice.HasValue)
            hotelsQuery = hotelsQuery.Where(h => h.PerNightRate >= filters.MinPrice);

        if (filters.MaxPrice.HasValue)
            hotelsQuery = hotelsQuery.Where(h => h.PerNightRate <= filters.MaxPrice);

        if (!string.IsNullOrWhiteSpace(filters.Location))
            hotelsQuery = hotelsQuery.Where(h => h.Address.Contains(filters.Location));

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            hotelsQuery = hotelsQuery.Where(h => h.Name.Contains(term) || h.Address.Contains(term));
        }

        hotelsQuery = filters.SortBy?.ToLower() switch
        {
            "name" => filters.SortDescending ? hotelsQuery.OrderByDescending(h => h.Name) : hotelsQuery.OrderBy(h => h.Name),
            "rating" => filters.SortDescending ? hotelsQuery.OrderByDescending(h => h.Rating) : hotelsQuery.OrderBy(h => h.Rating),
            "price" => filters.SortDescending ? hotelsQuery.OrderByDescending(h => h.PerNightRate) : hotelsQuery.OrderBy(h => h.PerNightRate),
            "address" => filters.SortDescending ? hotelsQuery.OrderByDescending(h => h.Address) : hotelsQuery.OrderBy(h => h.Address),
            _ => hotelsQuery.OrderBy(h => h.Name)
        };

        var pagedHotels = await hotelsQuery
            .ProjectTo<GetHotelSlimDto>(mapper.ConfigurationProvider)
            .ToPageResultAsync(paginationParameters);

        var result = new GetCountryHotelsDto
        (
            countryId,
            countryName,
            pagedHotels
        );

        return Result<GetCountryHotelsDto>.Success(result);
    }

    public async Task<Result<GetCountryDto>> GetCountryAsync(int id)
    {
        // Check the cache
        var cacheKey = $"{CountrySingleCacheName}{id}";

        if (!cache.TryGetValue(cacheKey, out GetCountryDto? country))
        {
            country = await db.Countries
                .AsNoTracking()
                .Where(c => c.CountryId == id)
                .ProjectTo<GetCountryDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (country is not null)
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                cache.Set(cacheKey, country, cacheOptions);
            }
        }

        return country is null
            ? Result<GetCountryDto>.NotFound()
            : Result<GetCountryDto>.Success(country);
    }

    public async Task<Result<GetCountryDto>> CreateCountryAsync(CreateCountryDto countryDto)
    {
        try
        {
            var exists = await CountryExistsAsync(countryDto.Name);

            if (exists)
                return Result<GetCountryDto>
                    .Failure(new Error(ErrorCodes.Conflict, $"Country with name '{countryDto.Name}' already exists"));

            var country = mapper.Map<Country>(countryDto);

            await db.Countries.AddAsync(country);
            await db.SaveChangesAsync();

            var dto = mapper.Map<GetCountryDto>(country);
            InvalidateCountryCache(country.CountryId);

            return Result<GetCountryDto>.Success(dto);
        }
        catch (Exception)
        {
            return Result<GetCountryDto>.Failure();
        }
    }

    public async Task<Result> UpdateCountryAsync(int id, UpdateCountryDto country)
    {
        if (id != country.Id)
            return Result.BadRequest(new Error(ErrorCodes.Validation, "Id route value doesn't match payload Id."));

        var duplicateName = await CountryExistsAsync(country.Name, id);
        if (duplicateName)
            return Result.Failure(new Error(ErrorCodes.Conflict, $"Country with name '{country.Name}' already exists."));

        var updated = await db.Countries
            .Where(c => c.CountryId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Name, country.Name)
                .SetProperty(c => c.ShortName, country.ShortName));

        if (updated > 0)
            InvalidateCountryCache(id);

        return updated > 0
            ? Result.Success()
            : Result.NotFound($"Country with id '{id}' was not found.");
    }

    public async Task<Result> PatchCountryAsync(int id, JsonPatchDocument<UpdateCountryDto> patchDoc)
    {
        if (patchDoc is null)
            return Result.BadRequest(new Error(ErrorCodes.Validation, "Patch document is required."));

        var country = await db.Countries.FindAsync(id);

        if (country is null)
            return Result.NotFound($"Country '{id}' was not found.");

        var countryDto = mapper.Map<UpdateCountryDto>(country);
        patchDoc.ApplyTo(countryDto);

        if (countryDto.Id != id)
            return Result.BadRequest(new Error(ErrorCodes.Validation, "Cannot modify the Id field."));

        var normalizedName = countryDto.Name.ToLower().Trim();
        var duplicateExists = await db.Countries
            .AnyAsync(c => c.Name.ToLower().Trim() == normalizedName && c.CountryId != id);

        if (duplicateExists)
            return Result.Failure(new Error(ErrorCodes.Conflict, $"Country with name '{countryDto.Name}' already exists."));

        mapper.Map(countryDto, country);
        await db.SaveChangesAsync();

        InvalidateCountryCache(id);

        return Result.Success();
    }

    public async Task<Result> DeleteCountryAsync(int id)
    {
        var deleted = await db.Countries
            .Where(c => c.CountryId == id)
            .ExecuteDeleteAsync();

        if (deleted > 0)
            InvalidateCountryCache(id);

        return deleted > 0
            ? Result.Success()
            : Result.NotFound($"Country with id '{id}' was not found.");
    }

    public async Task<bool> CountryExistsAsync(int id) =>
        await db.Countries.AnyAsync(c => c.CountryId == id);

    public async Task<bool> CountryExistsAsync(string name, int? excludeId = null) =>
        await db.Countries.AnyAsync(c =>
            c.Name.ToLower().Trim() == name.ToLower().Trim() &&
            (!excludeId.HasValue || c.CountryId != excludeId.Value));

    private void InvalidateCountryCache(int id)
    {
        cache.Remove($"{CountrySingleCacheName}{id}");
        cache.Remove($"{CountryListCacheName}");
    }
}
