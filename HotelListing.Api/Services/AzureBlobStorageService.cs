using HotelListing.Api.Application.Contracts;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace HotelListing.Api.Services;

public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration.GetConnectionString("AzureBlobStorage") ?? "UseDevelopmentStorage=true";
        _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "hotel-imports";
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        // 1. Get container reference and ensure it exists
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        // 2. Build unique, date-partitioned blob path (e.g. 2026/09/a1b2c3..._ukrainian_hotels.csv)
        var blobName = GenerateBlobPath(fileName);
        var blobClient = containerClient.GetBlobClient(blobName);

        // 3. Set content type so Azure knows the file MIME type
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        // 4. Ensure stream starts at byte 0 if seekable
        if (stream.CanSeek)
            stream.Position = 0;

        _logger.LogInformation("Uploading {FileName} to Azure Blob Storage ({Container}/{BlobName})",
            fileName, _containerName, blobName);

        // 5. Upload stream to blob storage
        await blobClient.UploadAsync(stream, uploadOptions, cancellationToken);

        // Returns full URI, e.g.: "http://127.0.0.1:10000/devstoreaccount1/hotel-imports/2026/09/..."
        return blobClient.Uri.ToString();
    }

    public async Task<Stream> OpenReadStreamAsync(
        string storedFileUri,
        CancellationToken cancellationToken = default
    )
    {
        // 1. Parse the container and blob path from the URI
        var blobUriBuilder = new BlobUriBuilder(new Uri(storedFileUri));

        // 2. Get an authenticated client from _blobServiceClient (which holds your credentials!)
        var containerClient = _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
        var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

        // 3. Authenticated streaming download
        var downloadResponse = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return downloadResponse.Value.Content;
    }

    public async Task<bool> DeleteAsync(string storedFileUri, CancellationToken cancellationToken = default)
    {
        var blobUriBuilder = new BlobUriBuilder(new Uri(storedFileUri));
        var containerClient = _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
        var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

        var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        return response.Value;
    }

    private static string GenerateBlobPath(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var safeFileName = Path.GetFileNameWithoutExtension(fileName);
        var datePrefix = DateTime.UtcNow.ToString("yyyy/MM");

        return $"{datePrefix}/{Guid.NewGuid():N}_{safeFileName}{extension}";
    }
}
