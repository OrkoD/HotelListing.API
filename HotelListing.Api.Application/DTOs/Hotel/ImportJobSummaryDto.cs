using HotelListing.Api.Domain;

namespace HotelListing.Api.Application.DTOs.Hotel;

public record ImportJobSummaryDto(
    int JobId,
    string OriginalFileName,
    ImportStatus Status,
    int TotalRecords,
    int SuccessfulRecords,
    int FailedRecords,
    List<string> Errors
);
