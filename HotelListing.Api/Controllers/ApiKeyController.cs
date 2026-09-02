using HotelListing.Api.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = AuthenticationDefaults.ApiKeyScheme)]
public class ApiKeyController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new
        {
            message = "Successfully authenticated via API Key!",
            scheme = AuthenticationDefaults.ApiKeyScheme,
            timestamp = DateTimeOffset.UtcNow
        });

    [HttpGet("secret")]
    public IActionResult GetSecret() =>
        Ok(new
        {
            secretData = "Top-secret data accessible only with a valid API key.",
            clientId = User.Identity?.Name ?? "ApiKeyClient"
        });
}
