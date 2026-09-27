using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Results;

namespace HotelListing.Api.Application.Contracts;

/// <summary>
/// Strategy contract for parsing hotel data feeds from various file formats.
/// </summary>
public interface IHotelDataParser
{
    /// <summary>The file format this strategy is responsible for.</summary>
    SupportedImportFormat Format { get; }

    /// <summary>
    /// Streams parsed rows asynchronously from a file stream.
    /// Each row is wrapped in a Result to support partial successes.
    /// </summary>
    IAsyncEnumerable<Result<HotelImportRowDto>> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken = default
    );
}
