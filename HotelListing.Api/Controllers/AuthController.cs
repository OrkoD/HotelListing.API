using HotelListing.Api.Constants;
using HotelListing.Api.Controllers;
using HotelListing.Api.Data;
using HotelListing.Api.DTOs.Auth;
using HotelListing.Api.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelListing.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class AuthController(UserManager<ApplicationUser> userManager) : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserDto registerUserDto)
    {
        var user = new ApplicationUser
        {
            Email = registerUserDto.Email,
            FirstName = registerUserDto.FirstName,
            LastName = registerUserDto.LastName,
            UserName = registerUserDto.Email,
        };

        var result = await userManager.CreateAsync(user, registerUserDto.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new Error(ErrorCodes.BadRequest, e.Description)).ToArray();
            return MapErrorsToResponse(errors);
        }

        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserDto loginUserDto)
    {
        var user = await userManager.FindByEmailAsync(loginUserDto.Email);

        if (user is null)
            return Unauthorized(new { message = "Invalid Credentials" });

        var isPasswordValid = await userManager.CheckPasswordAsync(user, loginUserDto.Password);

        if (!isPasswordValid)
            return Unauthorized(new { message = "Invalid Credentials" });

        return Ok();
    }
}