using HotelListing.Api.DTOs.Country;

namespace HotelListing.Api.Contracts;

public interface ICountriesService
{
    Task<IEnumerable<GetCountriesDto>> GetCountriesAsync();

    Task<GetCountryDto?> GetCountryAsync(int id);

    Task<GetCountryDto> CreateCountryAsync(CreateCountryDto countryDto);

    Task<bool> UpdateCountryAsync(int id, UpdateCountryDto country);

    Task<bool> DeleteCountryAsync(int id);
}
