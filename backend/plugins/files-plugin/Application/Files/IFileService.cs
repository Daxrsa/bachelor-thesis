using FilesPlugin.Domain.Entities;

namespace FilesPlugin.Application.Files;

public interface IFileService
{
    Task<StoredImage> UploadAsync(Stream stream, string originalFileName, string contentType, long size, CancellationToken cancellationToken);
    Task<StoredImage?> GetByFileNameAsync(string fileName, CancellationToken cancellationToken);
    Task<IReadOnlyList<StoredImage>> ListAsync(CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string fileName, CancellationToken cancellationToken);
}

public interface IImageStorageRepository
{
    Task<StoredImage> SaveAsync(Stream stream, string originalFileName, string contentType, long size, CancellationToken cancellationToken);
    Task<StoredImage?> GetByFileNameAsync(string fileName, CancellationToken cancellationToken);
    Task<IReadOnlyList<StoredImage>> ListAsync(CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string fileName, CancellationToken cancellationToken);
}
