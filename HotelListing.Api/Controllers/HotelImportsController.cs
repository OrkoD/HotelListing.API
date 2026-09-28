using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelListing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelImportsController(IHotelImportService importService) : ApiControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB limit


    /// <summary>
    /// Uploads and ingests a batch of hotels from a CSV, JSON, or PDF file.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImportJobSummaryDto>> ImportHotels(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default
    )
    {
        if (file is null || file.Length == 0)
            return BadRequest("A non-empty file is required.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest($"File size exceeds the 10 MB limit.");

        await using var stream = file.OpenReadStream();

        var result = await importService.ImportHotelsAsync(stream, file.FileName, file.ContentType, file.Length, cancellationToken);

        return ToActionResult(result);
    }
}
