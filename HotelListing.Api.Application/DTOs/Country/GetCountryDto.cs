using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Models.Paging;

namespace HotelListing.Api.Application.DTOs.Country;

public record GetCountryDto(
    int Id,
    string Name,
    string ShortName,
    List<GetHotelSlimDto>? Hotels
);

public record GetCountryHotelsDto(
    int Id,
    string Name,
    PageResult<GetHotelSlimDto> Hotels
);

public record GetCountriesDto(
    int Id,
    string Name,
    string ShortName
);
