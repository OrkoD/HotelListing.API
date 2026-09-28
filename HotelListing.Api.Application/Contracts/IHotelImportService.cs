using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Results;

namespace HotelListing.Api.Application.Contracts;

public interface IHotelImportService
{
    Task<Result<ImportJobSummaryDto>> ImportHotelsAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken = default
    );
}
