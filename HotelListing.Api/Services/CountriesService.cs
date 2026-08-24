using HotelListing.Api.Contracts;
using HotelListing.Api.Data;
using HotelListing.Api.DTOs.Country;
using HotelListing.Api.DTOs.Hotel;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Services;

public class CountriesService(HotelListingDbContext context) : ICountriesService
{
    public async Task<IEnumerable<GetCountriesDto>> GetCountriesAsync() =>
        await context.Countries
            .Select(c => new GetCountriesDto(c.CountryId, c.Name, c.ShortName))
            .ToListAsync();

    public async Task<GetCountryDto?> GetCountryAsync(int id) =>
        await context.Countries
            .Where(c => c.CountryId == id)
            .Select(c => new GetCountryDto(
                c.CountryId,
                c.Name,
                c.ShortName,
                c.Hotels.Select(h => new GetHotelSlimDto(
                    h.Id,
                    h.Name,
                    h.Address,
                    h.Rating
                )).ToList()
            ))
            .SingleOrDefaultAsync();

    public async Task<GetCountryDto> CreateCountryAsync(CreateCountryDto countryDto)
    {
        var country = new Country
        {
            Name = countryDto.Name,
            ShortName = countryDto.ShortName
        };
        await context.Countries.AddAsync(country);
        await context.SaveChangesAsync();

        return new GetCountryDto(
            country.CountryId,
            country.Name,
            country.ShortName,
            []
        );
    }

    public async Task<bool> UpdateCountryAsync(int id, UpdateCountryDto country)
    {
        var updated = await context.Countries
            .Where(c => c.CountryId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Name, country.Name)
                .SetProperty(c => c.ShortName, country.ShortName));

        return updated > 0;
    }

    public async Task<bool> DeleteCountryAsync(int id)
    {
        var deleted = await context.Countries
            .Where(c => c.CountryId == id)
            .ExecuteDeleteAsync();

        return deleted > 0;
    }
}
