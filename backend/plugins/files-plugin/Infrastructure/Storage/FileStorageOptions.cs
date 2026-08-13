namespace FilesPlugin.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public string RootPath { get; init; } = Path.Combine("storage", "images");
    public string RequestPath { get; init; } = "/static/images";
}
