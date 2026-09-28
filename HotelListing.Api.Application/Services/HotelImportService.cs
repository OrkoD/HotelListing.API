using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Domain;
using HotelListing.Api.Common.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text.Json;

namespace HotelListing.Api.Application.Services;

public class HotelImportService(
    HotelListingDbContext db,
    IHotelDataParserFactory parserFactory
) : IHotelImportService
{
    public async Task<Result<ImportJobSummaryDto>> ImportHotelsAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve parser
        var parser = parserFactory.GetParser(originalFileName);

        if (parser is null)
            return Result<ImportJobSummaryDto>.BadRequest(
                new Error(ErrorCodes.Validation, $"Unsupported file format for '{originalFileName}'. Supported formats: .csv, .json, .pdf")
            );

        // 2. Initialize Audit Job entity in SQL DB
        var job = new ImportJob
        {
            OriginalFileName = originalFileName,
            StoredFileUri = "local-temp",
            ContentType = contentType,
            FileExtension = Path.GetExtension(originalFileName).ToLowerInvariant(),
            FileSizeBytes = fileSizeBytes,
            Status = ImportStatus.Processing,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.ImportJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);

        // 3. Pre-load countries into memory for O(1) lookups by ShortName (e.g. "JM") and Name (e.g. "Jamaica")
        var countries = await db.Countries.AsNoTracking().ToListAsync(cancellationToken);
        var countryLookup = countries.SelectMany(c => new[]
        {
            (Key: c.Name, Value: c.CountryId),
            (Key: c.ShortName, Value: c.CountryId)
        }).ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase);

        // 4. Stream and process rows
        var hotelsToInsert = new List<Hotel>();
        var errors = new List<string>();
        var totalRows = 0;

        await foreach (var rowResult in parser.ParseAsync(fileStream, cancellationToken))
        {
            totalRows++;

            // Parsing failure (e.g. malformed numbers, missing columns)
            if (!rowResult.IsSuccess)
            {
                errors.Add(rowResult.Errors[0].Description);
                continue;
            }

            var row = rowResult.Value!;

            // Validate that the referenced country exists
            if (!countryLookup.TryGetValue(row.CountryIdentifier, out var countryId))
            {
                errors.Add($"Country '{row.CountryIdentifier}' not found.");
                continue;
            }

            hotelsToInsert.Add(new Hotel
            {
                Name = row.Name,
                Address = row.Address,
                Rating = row.Rating,
                PerNightRate = row.PerNightRate,
                CountryId = countryId
            });
        }

        // 5. Bulk insert all valid hotels
        if (hotelsToInsert.Count > 0)
        {
            db.Hotels.AddRange(hotelsToInsert);
        }

        // 6. Finalize the Audit Job record
        job.TotalRecords = totalRows;
        job.SuccessfulRecords = hotelsToInsert.Count;
        job.FailedRecords = errors.Count;
        job.CompletedAtUtc = DateTime.UtcNow;

        job.Status = (errors.Count, hotelsToInsert.Count) switch
        {
            (0, > 0) => ImportStatus.Completed,
            ( > 0, > 0) => ImportStatus.PartiallyCompleted,
            _ => ImportStatus.Failed
        };

        if (errors.Count > 0)
        {
            job.ErrorsDetailsJson = JsonSerializer.Serialize(errors);
        }

        await db.SaveChangesAsync(cancellationToken);

        // 7. Return summary response
        var summary = new ImportJobSummaryDto(
            job.Id,
            job.OriginalFileName,
            job.Status,
            job.TotalRecords,
            job.SuccessfulRecords,
            job.FailedRecords,
            errors
        );

        return Result<ImportJobSummaryDto>.Success(summary);
    }
}
