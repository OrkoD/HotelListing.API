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

namespace HotelListing.Api.Application.Services;

public class CountriesService(HotelListingDbContext context, IMapper mapper) : ICountriesService
{
    public async Task<Result<PageResult<GetCountriesDto>>> GetCountriesAsync(PaginationParameters paginationParameters)
    {
        var countries = await context.Countries
            .OrderBy(c => c.CountryId)
            .ProjectTo<GetCountriesDto>(mapper.ConfigurationProvider)
            .ToPageResultAsync(paginationParameters);

        return Result<PageResult<GetCountriesDto>>.Success(countries);
    }

    public async Task<Result<GetCountryDto>> GetCountryAsync(int id)
    {
        var country = await context.Countries
            .Where(c => c.CountryId == id)
            .ProjectTo<GetCountryDto>(mapper.ConfigurationProvider)
            .SingleOrDefaultAsync();

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
                return Result<GetCountryDto>.Failure(new Error(ErrorCodes.Conflict, $"Country with name '{countryDto.Name}' already exists"));

            var country = mapper.Map<Country>(countryDto);

            await context.Countries.AddAsync(country);
            await context.SaveChangesAsync();

            var dto = mapper.Map<GetCountryDto>(country);

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

        var updated = await context.Countries
            .Where(c => c.CountryId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Name, country.Name)
                .SetProperty(c => c.ShortName, country.ShortName));

        return updated > 0
            ? Result.Success()
            : Result.NotFound($"Country with id '{id}' was not found.");
    }

    public async Task<Result> DeleteCountryAsync(int id)
    {
        var deleted = await context.Countries
            .Where(c => c.CountryId == id)
            .ExecuteDeleteAsync();

        return deleted > 0
            ? Result.Success()
            : Result.NotFound($"Country with id '{id}' was not found.");
    }

    public async Task<bool> CountryExistsAsync(int id) =>
        await context.Countries.AnyAsync(c => c.CountryId == id);

    public async Task<bool> CountryExistsAsync(string name, int? excludeId = null) =>
        await context.Countries.AnyAsync(c =>
            c.Name.ToLower().Trim() == name.ToLower().Trim() &&
            (!excludeId.HasValue || c.CountryId != excludeId.Value));
}
