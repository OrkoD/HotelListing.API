namespace HotelListing.Api.Domain;

/// <summary>
/// Tracks the metadata, processing state, and results of a bulk file upload.
/// </summary>
public class ImportJob
{
    public int Id { get; set; }

    /// <summary>Original client file name, e.g., "summer_hotels_2026.csv".</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Storage URI or local path where the raw file is stored.</summary>
    public string StoredFileUri { get; set; } = string.Empty;

    /// <summary>MIME type detected, e.g., "text/csv", "application/json", "application/pdf".</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Normalized file extension: ".csv", ".json", ".pdf".</summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>File size in bytes (used for auditing and storage quotas).</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Current lifecycle status.</summary>
    public ImportStatus Status { get; set; }

    /// <summary>Total rows/records found in the file.</summary>
    public int TotalRecords { get; set; }

    /// <summary>Count of successfully persisted records.</summary>
    public int SuccessfulRecords { get; set; }

    /// <summary>Count of rejected/invalid records.</summary>
    public int FailedRecords { get; set; }

    /// <summary>
    /// JSON-serialized list of row-level errors, e.g. [{"Row": 14, "Error": "Price must be > 0"}].
    /// </summary>
    public string? ErrorsDetailsJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }
}
