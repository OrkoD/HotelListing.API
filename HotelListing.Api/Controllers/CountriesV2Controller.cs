using Asp.Versioning;
using HotelListing.Api.Common.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/countries")]
[ApiVersion(2.0, Deprecated = true)]
[EnableRateLimiting(RateLimitingConstants.FixedPolicy)]
public class CountriesV2Controller : ApiControllerBase
{
    [HttpGet]
    public IActionResult GetCountries(
        [FromQuery] int? pageNumber = 1,
        [FromQuery] int? pageSize = 10
    )
    {
        return Ok(new
        {
            version = "2.0",
            Message = "Enhanced Countries API",
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }
}
