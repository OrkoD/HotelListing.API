using HotelListing.Api.Common.Results;
using HotelListing.Api.Domain;
using HotelListing.Api.Application.DTOs.Auth;

namespace HotelListing.Api.Application.Contracts;

public interface IUsersService
{
    Task<Result<string>> LoginAsync(LoginUserDto loginUserDto);
    Task<Result<RegisteredUserDto>> RegisterAsync(RegisterUserDto registerUserDto);
    string UserId { get; }
}
