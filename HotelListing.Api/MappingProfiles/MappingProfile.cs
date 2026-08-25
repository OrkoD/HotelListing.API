using AutoMapper;
using HotelListing.Api.Data;
using HotelListing.Api.DTOs.Country;
using HotelListing.Api.DTOs.Hotel;

namespace HotelListing.Api.MappingProfiles;

public class HotelMappingProfile : Profile
{
    public HotelMappingProfile()
    {
        CreateMap<Hotel, GetHotelDto>()
            .ForCtorParam(
                nameof(GetHotelDto.Country),
                cfg => cfg.MapFrom(s => s.Country != null ? s.Country.Name : string.Empty)
            );
        CreateMap<CreateHotelDto, Hotel>();
        CreateMap<Hotel, GetHotelSlimDto>();
    }
}

public class CountryMappingProfile : Profile
{
    public CountryMappingProfile()
    {
        CreateMap<Country, GetCountryDto>()
            .ForCtorParam(nameof(GetCountryDto.Id), cfg => cfg.MapFrom(s => s.CountryId));
        CreateMap<Country, GetCountriesDto>()
            .ForCtorParam(nameof(GetCountriesDto.Id), cfg => cfg.MapFrom(s => s.CountryId));
        CreateMap<CreateCountryDto, Country>();
    }
}