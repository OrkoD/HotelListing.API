using HotelListing.Api.Contracts;
using HotelListing.Api.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelListing.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class AuthController(IUsersService usersService) : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<RegisteredUserDto>> Register(RegisterUserDto registerUserDto) =>
        ToActionResult(await usersService.RegisterAsync(registerUserDto));

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(LoginUserDto loginUserDto) =>
        ToActionResult(await usersService.LoginAsync(loginUserDto));
}