using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.AuthorizationFilters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class HotelOrSystemAdminAttribute : TypeFilterAttribute
{
    public HotelOrSystemAdminAttribute() : base(typeof(HotelOrSystemAdminFilter))
    {
    }
}

public class HotelOrSystemAdminFilter(HotelListingDbContext db) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpUser = context.HttpContext.User;

        if (httpUser?.Identity?.IsAuthenticated == false)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (httpUser!.IsInRole(RoleNames.Admin))
            return;

        var userId = httpUser.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? httpUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Result = new ForbidResult();
            return;
        }

        context.RouteData.Values.TryGetValue("hotelId", out var hotelIdObj);
        int.TryParse(hotelIdObj?.ToString(), out int hotelId);

        if (hotelId == 0)
        {
            context.Result = new ForbidResult();
            return;
        }

        var isHotelAdmin = await db.HotelAdmins
            .AnyAsync(a => a.UserId == userId && a.HotelId == hotelId);

        if (!isHotelAdmin)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}
