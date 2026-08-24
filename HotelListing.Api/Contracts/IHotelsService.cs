using HotelListing.Api.DTOs.Hotel;

namespace HotelListing.Api.Contracts;

public interface IHotelsService
{
    Task<IEnumerable<GetHotelDto>> GetHotelsAsync();

    Task<GetHotelDto?> GetHotelAsync(int id);

    Task<GetHotelDto> CreateHotelAsync(CreateHotelDto hotelDto);

    Task<bool> UpdateHotelAsync(int id, UpdateHotelDto hotel);

    Task<bool> DeleteHotelAsync(int id);
}
