using HotelListing.Api.Common.Results;
using HotelListing.Api.DTOs.Hotel;

namespace HotelListing.Api.Contracts;

public interface IHotelsService
{
    Task<Result<IEnumerable<GetHotelDto>>> GetHotelsAsync();

    Task<Result<GetHotelDto>> GetHotelAsync(int id);

    Task<Result<GetHotelDto>> CreateHotelAsync(CreateHotelDto hotelDto);

    Task<Result> UpdateHotelAsync(int id, UpdateHotelDto hotel);

    Task<Result> DeleteHotelAsync(int id);
}
