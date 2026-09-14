using AutoMapper;
using HotelListing.Api.Domain;
using HotelListing.Api.Application.DTOs.Booking;
using HotelListing.Api.Application.DTOs.Country;
using HotelListing.Api.Application.DTOs.Hotel;

namespace HotelListing.Api.Application.MappingProfiles;

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
        CreateMap<Country, UpdateCountryDto>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.CountryId))
            .ReverseMap()
            .ForMember(d => d.CountryId, opt => opt.MapFrom(s => s.Id));
    }
}

public sealed class BookingMappingProfile : Profile
{
    public BookingMappingProfile()
    {
        // 1. Map to GetBookingDto using ForCtorParam (safe null check)
        CreateMap<Booking, GetBookingDto>()
            .ForCtorParam(
                nameof(GetBookingDto.HotelName),
                cfg => cfg.MapFrom(s => s.Hotel != null ? s.Hotel.Name : string.Empty)
            )
            .ForCtorParam(
                nameof(GetBookingDto.Status),
                cfg => cfg.MapFrom(s => s.Status.ToString())
            );

        // 2. Map CreateBookingDto -> Booking (Ignoring unmapped fields is great practice!)
        CreateMap<CreateBookingDto, Booking>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.TotalPrice, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.CreatedAtUtc, o => o.Ignore())
            .ForMember(d => d.UpdatedAtUtc, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Hotel, o => o.Ignore());

        // 3. Map UpdateBookingDto -> Booking
        CreateMap<UpdateBookingDto, Booking>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.HotelId, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.TotalPrice, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.CreatedAtUtc, o => o.Ignore())
            .ForMember(d => d.UpdatedAtUtc, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Hotel, o => o.Ignore());
    }
}
