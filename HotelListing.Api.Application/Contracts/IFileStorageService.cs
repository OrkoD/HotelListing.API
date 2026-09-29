namespace HotelListing.Api.Application.Contracts;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default
    );

    Task<Stream> OpenReadStreamAsync(
        string storedFileUri,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(
        string storedFileUri,
        CancellationToken cancellationToken = default
    );
}
