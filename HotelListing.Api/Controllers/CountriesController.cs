using HotelListing.Api.Application.DTOs.Country;
using HotelListing.Api.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Models.Filtering;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;

namespace HotelListing.Api.Controllers;

/// <summary>
/// Manages country resources, including querying, pagination, filtering, and administrative lifecycle operations.
/// </summary>
/// <param name="countriesService">The domain service handling country business logic and persistence.</param>
[ApiController]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1.0)]
[EnableRateLimiting(RateLimitingConstants.FixedPolicy)]
public class CountriesController(ICountriesService countriesService) : ApiControllerBase
{
    /// <summary>
    /// Retrieves a list of countries with optional filtering and sorting.
    /// </summary>
    /// <param name="filters">Query criteria to filter by name, short code, and sort order.</param>
    /// <returns>A collection of countries matching the specified criteria.</returns>
    /// <response code="200">Returns the matching list of countries.</response>
    /// <response code="400">If the filter parameters are invalid.</response>
    /// <response code="429">If the client exceeds the rate limit.</response>
    [HttpGet]
    [OutputCache(PolicyName = CacheConstants.AuthenticatedUserCachingPolicy)]
    public async Task<ActionResult<IEnumerable<GetCountriesDto>>> GetCountries([FromQuery] CountryFilterParameters filters) =>
        ToActionResult(await countriesService.GetCountriesAsync(filters));

    /// <summary>
    /// Retrieves a specific country along with its associated hotels, supporting pagination and rating filters.
    /// </summary>
    /// <param name="countryId">The unique identifier of the country.</param>
    /// <param name="paginationParameters">Pagination settings (page number and page size).</param>
    /// <param name="filters">Filter parameters for the nested hotel list (e.g., minimum rating, search term).</param>
    /// <returns>The country details including a paginated list of its hotels.</returns>
    /// <response code="200">Returns the country and its associated hotels.</response>
    /// <response code="400">If pagination or filter parameters are invalid.</response>
    /// <response code="404">If the country with the specified ID was not found.</response>
    [HttpGet("{countryId:int}/hotels")]
    public async Task<ActionResult<GetCountryHotelsDto>> GetCountryHotels(
        [FromRoute] int countryId,
        [FromQuery] PaginationParameters paginationParameters,
        [FromQuery] HotelFilterParameters filters
    ) =>
        ToActionResult(await countriesService.GetCountryHotelsAsync(countryId, paginationParameters, filters));

    /// <summary>
    /// Retrieves the details of a single country by its unique identifier.
    /// </summary>
    /// <param name="id">The unique integer identifier of the country.</param>
    /// <returns>The country details.</returns>
    /// <response code="200">Returns the requested country.</response>
    /// <response code="404">If a country with the specified ID was not found.</response>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetCountryDto>> GetCountry(int id) =>
        ToActionResult(await countriesService.GetCountryAsync(id));

    /// <summary>
    /// Creates a new country record.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/countries
    ///     {
    ///        "name": "United Kingdom",
    ///        "shortName": "UK"
    ///     }
    ///
    /// Requires administrative privileges (<c>Admin</c> role).
    /// </remarks>
    /// <param name="countryDto">The data required to create the new country.</param>
    /// <returns>The newly created country with its assigned identifier.</returns>
    /// <response code="201">Returns the newly created country and sets the Location header.</response>
    /// <response code="400">If the payload fails validation rules.</response>
    /// <response code="401">If the request is unauthenticated.</response>
    /// <response code="403">If the caller does not have the Administrator role.</response>
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<ActionResult<GetCountryDto>> CreateCountry(CreateCountryDto countryDto) =>
        ToCreatedAtActionResult(await countriesService.CreateCountryAsync(countryDto), nameof(GetCountry), country => new { id = country.Id });

    /// <summary>
    /// Fully updates an existing country record by replacing all its properties.
    /// </summary>
    /// <remarks>
    /// Requires administrative privileges (<c>Admin</c> role).
    /// </remarks>
    /// <param name="id">The unique identifier of the country to update.</param>
    /// <param name="country">The updated country payload.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">If the country was successfully updated.</response>
    /// <response code="400">If the ID in the route does not match the ID in the body, or validation fails.</response>
    /// <response code="401">If the request is unauthenticated.</response>
    /// <response code="403">If the caller does not have the Administrator role.</response>
    /// <response code="404">If the country does not exist.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCountry(int id, UpdateCountryDto country) =>
        ToActionResult(await countriesService.UpdateCountryAsync(id, country));

    /// <summary>
    /// Partially updates an existing country record using JSON Patch (RFC 6902).
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     PATCH /api/v1/countries/1
    ///     [
    ///       { "op": "replace", "path": "/name", "value": "New Country Name" }
    ///     ]
    ///
    /// Requires administrative privileges (<c>Admin</c> role).
    /// </remarks>
    /// <param name="id">The unique identifier of the country to patch.</param>
    /// <param name="patchDoc">The JSON Patch document containing the operations to apply.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">If the patch was successfully applied.</response>
    /// <response code="400">If the patch document is invalid or the resulting model fails validation.</response>
    /// <response code="401">If the request is unauthenticated.</response>
    /// <response code="403">If the caller does not have the Administrator role.</response>
    /// <response code="404">If the country does not exist.</response>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> PatchCountry(int id, [FromBody] JsonPatchDocument<UpdateCountryDto> patchDoc) =>
        ToActionResult(await countriesService.PatchCountryAsync(id, patchDoc));

    /// <summary>
    /// Deletes a country record by its unique identifier.
    /// </summary>
    /// <remarks>
    /// Requires administrative privileges (<c>Admin</c> role).
    /// </remarks>
    /// <param name="id">The unique identifier of the country to delete.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">If the country was successfully deleted.</response>
    /// <response code="401">If the request is unauthenticated.</response>
    /// <response code="403">If the caller does not have the Administrator role.</response>
    /// <response code="404">If the country does not exist.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> DeleteCountry(int id) =>
        ToActionResult(await countriesService.DeleteCountryAsync(id));
}
