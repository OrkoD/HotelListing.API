using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelListing.Api.Constants;
using HotelListing.Api.Contracts;
using HotelListing.Api.Data;
using HotelListing.Api.DTOs.Auth;
using HotelListing.Api.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HotelListing.Api.Services;

public class UsersService(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    HotelListingDbContext db
) : IUsersService
{
    public async Task<Result<RegisteredUserDto>> RegisterAsync(RegisterUserDto registerUserDto)
    {
        var isHotelAdmin = string.Equals(registerUserDto.Role, "Hotel Admin", StringComparison.OrdinalIgnoreCase);
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
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 3. Expiration duration (safe fallback with GetValue)
        var durationInMinutes = configuration.GetValue("JwtSettings:DurationInMinutes", 15);

        // 4. Create and serialize JWT token
        var token = new JwtSecurityToken(
            issuer: configuration["JwtSettings:Issuer"],
            audience: configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(durationInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
