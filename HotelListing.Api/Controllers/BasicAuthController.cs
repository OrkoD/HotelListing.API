using HotelListing.Api.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = AuthenticationDefaults.BasicScheme)]
public class BasicAuthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new
        {
            message = "Successfully authenticated via Basic Auth!",
            scheme = AuthenticationDefaults.BasicScheme,
            user = User.Identity?.Name,
            timestamp = DateTimeOffset.UtcNow
        });

    [HttpGet("profile")]
    public IActionResult GetProfile() =>
        Ok(new
        {
            username = User.Identity?.Name,
            isAuthenticated = User.Identity?.IsAuthenticated,
            authType = User.Identity?.AuthenticationType
        });
}
