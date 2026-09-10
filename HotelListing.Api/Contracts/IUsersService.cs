using HotelListing.Api.Common.Results;
using HotelListing.Api.Data;
using HotelListing.Api.DTOs.Auth;

namespace HotelListing.Api.Contracts;

public interface IUsersService
{
    Task<Result<string>> LoginAsync(LoginUserDto loginUserDto);
    Task<Result<RegisteredUserDto>> RegisterAsync(RegisterUserDto registerUserDto);
    string UserId { get; }
}
