using HotelListing.Api.DTOs.Country;
using HotelListing.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using HotelListing.Api.Results;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CountriesController(ICountriesService countriesService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetCountriesDto>>> GetCountries() =>
        ToActionResult(await countriesService.GetCountriesAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetCountryDto>> GetCountry(int id) =>
        ToActionResult(await countriesService.GetCountryAsync(id));

    [HttpPost]
    public async Task<ActionResult<GetCountryDto>> CreateCountry(CreateCountryDto countryDto)
    {
        var result = await countriesService.CreateCountryAsync(countryDto);

        if (!result.IsSuccess)
            return MapErrorsToResponse(result.Errors);

        return CreatedAtAction(nameof(GetCountry), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCountry(int id, UpdateCountryDto country) =>
        ToActionResult(await countriesService.UpdateCountryAsync(id, country));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCountry(int id) =>
        ToActionResult(await countriesService.DeleteCountryAsync(id));

    // Helpers
    private ActionResult<T> ToActionResult<T>(Result<T> result) =>
        result.IsSuccess
            ? Ok(result.Value)
            : MapErrorsToResponse(result.Errors);

    private ActionResult ToActionResult(Result result) =>
        result.IsSuccess
            ? NoContent()
            : MapErrorsToResponse(result.Errors);

    private ActionResult MapErrorsToResponse(Error[] errors)
    {
        if (errors is null || errors.Length == 0)
            return Problem();

        var error = errors.FirstOrDefault();

        return error.Code switch
        {
            "NotFound" => NotFound(error.Description),        // 404
            "BadRequest" => BadRequest(error.Description),    // 400
            "Validation" => BadRequest(error.Description),    // 400
            "Conflict" => Conflict(error.Description),        // 409
            _ => Problem(detail: string.Join("; ", errors.Select(e => e.Description)), title: error.Code)
        };
    }
}
