using FilesPlugin.Application.Files;
using FilesPlugin.Domain.Entities;

namespace FilesPlugin.Infrastructure.Storage;

public sealed class LocalImageStorageRepository(IHostEnvironment hostEnvironment, FileStorageOptions options) : IImageStorageRepository
{
    private string StorageRootPath => ResolvePath(hostEnvironment.ContentRootPath, options.RootPath);

    public async Task<StoredImage> SaveAsync(Stream stream, string originalFileName, string contentType, long size, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(StorageRootPath);

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var safeName = MakeSafeBaseName(Path.GetFileNameWithoutExtension(originalFileName));
        var fileName = $"{safeName}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(StorageRootPath, fileName);

        await using var destination = File.Create(fullPath);
        await stream.CopyToAsync(destination, cancellationToken);

        return new StoredImage(
            fileName,
            contentType,
            size,
            DateTimeOffset.UtcNow);
    }

    public Task<StoredImage?> GetByFileNameAsync(string fileName, CancellationToken cancellationToken)
    {
        var safeFileName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(StorageRootPath, safeFileName);
        if (!File.Exists(fullPath))
            return Task.FromResult<StoredImage?>(null);

        var info = new FileInfo(fullPath);
        var contentType = ContentTypeFromExtension(info.Extension);

        return Task.FromResult<StoredImage?>(new StoredImage(
            safeFileName,
            contentType,
            info.Length,
            info.CreationTimeUtc));
    }

    public Task<IReadOnlyList<StoredImage>> ListAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(StorageRootPath))
            return Task.FromResult<IReadOnlyList<StoredImage>>(Array.Empty<StoredImage>());

        var files = Directory.GetFiles(StorageRootPath)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .Select(file => new StoredImage(
                file.Name,
                ContentTypeFromExtension(file.Extension),
                file.Length,
                file.CreationTimeUtc))
            .ToArray();

        return Task.FromResult<IReadOnlyList<StoredImage>>(files);
    }

    public Task<bool> DeleteAsync(string fileName, CancellationToken cancellationToken)
    {
        var safeFileName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(StorageRootPath, safeFileName);

        if (!File.Exists(fullPath))
            return Task.FromResult(false);

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    private static string ResolvePath(string contentRootPath, string configuredPath) =>
        Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));

    private static string MakeSafeBaseName(string input)
    {
        var chars = input
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();

        var normalized = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "image" : normalized;
    }

    private static string ContentTypeFromExtension(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };
}
