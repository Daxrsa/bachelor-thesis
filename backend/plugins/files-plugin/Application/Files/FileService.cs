using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FilesPlugin.Domain.Entities;

namespace FilesPlugin.Application.Files;

public sealed class FileService(IImageStorageRepository repository, FileValidationOptions options) : IFileService
{
    private static readonly StringComparer ContentTypeComparer = StringComparer.OrdinalIgnoreCase;

    public async Task<StoredImage> UploadAsync(Stream stream, string originalFileName, string contentType, long size, CancellationToken cancellationToken)
    {
        if (size <= 0)
            throw new InvalidOperationException("File is empty.");

        if (size > options.MaxFileSizeBytes)
            throw new InvalidOperationException($"File exceeds max size of {options.MaxFileSizeBytes} bytes.");

        if (!options.AllowedContentTypes.Contains(contentType, ContentTypeComparer))
            throw new InvalidOperationException($"Unsupported content type '{contentType}'.");

        return await repository.SaveAsync(stream, originalFileName, contentType, size, cancellationToken);
    }

    public Task<StoredImage?> GetByFileNameAsync(string fileName, CancellationToken cancellationToken) =>
        repository.GetByFileNameAsync(fileName, cancellationToken);

    public Task<IReadOnlyList<StoredImage>> ListAsync(CancellationToken cancellationToken) =>
        repository.ListAsync(cancellationToken);

    public Task<bool> DeleteAsync(string fileName, CancellationToken cancellationToken) =>
        repository.DeleteAsync(fileName, cancellationToken);
}

public sealed class FileValidationOptions
{
    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;

    public string[] AllowedContentTypes { get; init; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    ];
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFilesApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IFileService, FileService>();
        services.Configure<FileValidationOptions>(configuration.GetSection("FileValidation"));
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileValidationOptions>>().Value);
        return services;
    }
}
