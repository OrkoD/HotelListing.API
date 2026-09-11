using HotelListing.Api.Application.DTOs.Country;
using HotelListing.Api.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Application.DTOs.Hotel;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CountriesController(ICountriesService countriesService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetCountriesDto>>> GetCountries() =>
        ToActionResult(await countriesService.GetCountriesAsync());

    [HttpGet("{countryId:int}/hotels")]
    public async Task<ActionResult<PageResult<GetHotelDto>>> GetCountryHotels(
        [FromRoute] int countryId,
        [FromQuery] PaginationParameters paginationParameters
    ) =>
        ToActionResult(await countriesService.GetCountryHotelsAsync(countryId, paginationParameters));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetCountryDto>> GetCountry(int id) =>
        ToActionResult(await countriesService.GetCountryAsync(id));

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<ActionResult<GetCountryDto>> CreateCountry(CreateCountryDto countryDto) =>
        ToCreatedAtActionResult(await countriesService.CreateCountryAsync(countryDto), nameof(GetCountry), country => new { id = country.Id });

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> UpdateCountry(int id, UpdateCountryDto country) =>
        ToActionResult(await countriesService.UpdateCountryAsync(id, country));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> DeleteCountry(int id) =>
        ToActionResult(await countriesService.DeleteCountryAsync(id));
}
