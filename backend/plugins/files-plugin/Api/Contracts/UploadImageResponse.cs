namespace FilesPlugin.Api.Contracts;

public sealed record UploadImageResponse(
    string FileName,
    string ContentType,
    long Size,
    string PublicUrl,
    DateTimeOffset UploadedAtUtc);
