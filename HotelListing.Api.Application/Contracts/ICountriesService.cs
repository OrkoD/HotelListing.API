using HotelListing.Api.Common.Results;
using HotelListing.Api.Application.DTOs.Country;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Models.Filtering;
using Microsoft.AspNetCore.JsonPatch;

namespace HotelListing.Api.Application.Contracts;

public interface ICountriesService
{
    Task<Result<IEnumerable<GetCountriesDto>>> GetCountriesAsync(CountryFilterParameters filters);

    Task<Result<GetCountryHotelsDto>> GetCountryHotelsAsync(
        int countryId,
        PaginationParameters paginationParameters,
        HotelFilterParameters filters
    );

    Task<Result<GetCountryDto>> GetCountryAsync(int id);

    Task<Result<GetCountryDto>> CreateCountryAsync(CreateCountryDto countryDto);

    Task<Result> UpdateCountryAsync(int id, UpdateCountryDto country);

    Task<Result> PatchCountryAsync(int id, JsonPatchDocument<UpdateCountryDto> patchDoc);

    Task<Result> DeleteCountryAsync(int id);

    Task<bool> CountryExistsAsync(int id);

    Task<bool> CountryExistsAsync(string name, int? excludeId = null);
}
