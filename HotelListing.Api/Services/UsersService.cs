using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Models;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Contracts;
using HotelListing.Api.Data;
using HotelListing.Api.DTOs.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelListing.Api.Services;

public class UsersService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtSettings> jwtOptions,
    IHttpContextAccessor httpContextAccessor,
    HotelListingDbContext db
) : IUsersService
{
    public async Task<Result<RegisteredUserDto>> RegisterAsync(RegisterUserDto registerUserDto)
    {
        var isHotelAdmin = string.Equals(registerUserDto.Role, RoleNames.HotelAdmin, StringComparison.OrdinalIgnoreCase);
        if (isHotelAdmin)
        {
            var hotelExists = await db.Hotels.AnyAsync(h => h.Id == registerUserDto.AssociatedHotelId);
            if (!hotelExists)
                return Result<RegisteredUserDto>.Failure(
                    new Error(ErrorCodes.NotFound, $"Hotel with Id '{registerUserDto.AssociatedHotelId}' does not exist."));
        }

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
            return Result<RegisteredUserDto>.BadRequest(errors);
        }

        await userManager.AddToRoleAsync(user, registerUserDto.Role);

        if (isHotelAdmin)
        {
            db.HotelAdmins.Add(new HotelAdmin
            {
                UserId = user.Id,
                HotelId = registerUserDto.AssociatedHotelId!.Value
            });
            await db.SaveChangesAsync();
        }

        var registeredUserDto = new RegisteredUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = registerUserDto.Role
        };

        return Result<RegisteredUserDto>.Success(registeredUserDto);
    }

    public async Task<Result<string>> LoginAsync(LoginUserDto loginUserDto)
    {
        var user = await userManager.FindByEmailAsync(loginUserDto.Email);

        if (user is null)
            return Result<string>.Failure(new Error(ErrorCodes.BadRequest, "Invalid credentials."));

        var isPasswordValid = await userManager.CheckPasswordAsync(user, loginUserDto.Password);

        if (!isPasswordValid)
            return Result<string>.Failure(new Error(ErrorCodes.BadRequest, "Invalid credentials."));

        var token = await GenerateToken(user);

        return Result<string>.Success(token);
    }

    public string UserId =>
        httpContextAccessor?.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? string.Empty;

    private async Task<string> GenerateToken(ApplicationUser user)
    {
        // 1. Gather all claims in one single step (no temporary lists)
        var roles = await userManager.GetRolesAsync(user);
        List<Claim> claims = [
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            ..roles.Select(r => new Claim(ClaimTypes.Role, r))
        ];

        // 2. Signing credentials
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 3. Create and serialize JWT token
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Value.Issuer,
            audience: jwtOptions.Value.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtOptions.Value.DurationInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
