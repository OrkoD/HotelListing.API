using HotelListing.Api.Common.Results;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Models.Filtering;

namespace HotelListing.Api.Application.Contracts;

public interface IHotelsService
{
    Task<Result<PageResult<GetHotelDto>>> GetHotelsAsync(PaginationParameters paginationParameters, HotelFilterParameters filters);

    Task<Result<GetHotelDto>> GetHotelAsync(int id);

    Task<Result<GetHotelDto>> CreateHotelAsync(CreateHotelDto hotelDto);

    Task<Result> UpdateHotelAsync(int id, UpdateHotelDto hotel);

    Task<Result> DeleteHotelAsync(int id);
}
