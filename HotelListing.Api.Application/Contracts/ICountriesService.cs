using HotelListing.Api.Common.Results;
using HotelListing.Api.Application.DTOs.Country;
using HotelListing.Api.Common.Models.Paging;

namespace HotelListing.Api.Application.Contracts;

public interface ICountriesService
{
    Task<Result<PageResult<GetCountriesDto>>> GetCountriesAsync(PaginationParameters paginationParameters);

    Task<Result<GetCountryDto>> GetCountryAsync(int id);

    Task<Result<GetCountryDto>> CreateCountryAsync(CreateCountryDto countryDto);

    Task<Result> UpdateCountryAsync(int id, UpdateCountryDto country);

    Task<Result> DeleteCountryAsync(int id);

    Task<bool> CountryExistsAsync(int id);

    Task<bool> CountryExistsAsync(string name, int? excludeId = null);
}
