using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Domain;
using HotelListing.Api.Common.Constants;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HotelListing.Api.Application.Services;

public class HotelImportService(
    HotelListingDbContext db,
    IHotelDataParserFactory parserFactory,
    IFileStorageService fileStorageService
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

        var storedFileUri = await fileStorageService.UploadAsync(fileStream, originalFileName, contentType, cancellationToken);

        // 2. Initialize Audit Job entity in SQL DB
        var job = new ImportJob
        {
            OriginalFileName = originalFileName,
            StoredFileUri = storedFileUri,
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

        // 4. PHASE 1: Stream and validate rows from storage
        var validRows = new List<(HotelImportRowDto Row, int CountryId)>();
        var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();
        var totalRows = 0;

        await using var readStream = await fileStorageService.OpenReadStreamAsync(storedFileUri, cancellationToken);

        await foreach (var rowResult in parser.ParseAsync(readStream, cancellationToken))
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

            // Guard against duplicate hotel rows in the SAME file
            var hotelKey = BuildHotelKey(row.Name, countryId);
            if (!seenInBatch.Add(hotelKey))
            {
                errors.Add($"Row {row.RowNumber}: Duplicate hotel '{row.Name}' found within the same file.");
                continue;
            }

            validRows.Add((row, countryId));
        }

        // 5. PHASE 2: Batch-scoped DB query and Upsert
        if (validRows.Count > 0)
        {
            await UpsertHotelsAsync(validRows, cancellationToken);
        }

        // 6. Finalize the Audit Job record
        job.TotalRecords = totalRows;
        job.SuccessfulRecords = validRows.Count;
        job.FailedRecords = errors.Count;
        job.CompletedAtUtc = DateTime.UtcNow;

        job.Status = (errors.Count, validRows.Count) switch
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

    private async Task UpsertHotelsAsync(
        List<(HotelImportRowDto Row, int CountryId)> validRows,
        CancellationToken cancellationToken)
    {
        var hotelNames = validRows.Select(r => r.Row.Name).Distinct().ToList();
        var existingHotels = await db.Hotels
            .Where(h => hotelNames.Contains(h.Name))
            .ToListAsync(cancellationToken);
        var existingHotelLookup = existingHotels.ToDictionary(
            h => BuildHotelKey(h.Name, h.CountryId),
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var (row, countryId) in validRows)
        {
            var key = BuildHotelKey(row.Name, countryId);
            if (existingHotelLookup.TryGetValue(key, out var existingHotel))
            {
                existingHotel.Address = row.Address;
                existingHotel.Rating = row.Rating;
                existingHotel.PerNightRate = row.PerNightRate;
            }
            else
            {
                db.Hotels.Add(new Hotel
                {
                    Name = row.Name,
                    Address = row.Address,
                    Rating = row.Rating,
                    PerNightRate = row.PerNightRate,
                    CountryId = countryId
                });
            }
        }
    }

    private static string BuildHotelKey(string name, int countryId) =>
        $"{name.Trim().ToLowerInvariant()}|{countryId}";
}
