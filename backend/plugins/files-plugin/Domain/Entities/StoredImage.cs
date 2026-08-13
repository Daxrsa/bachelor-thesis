namespace FilesPlugin.Domain.Entities;

public sealed record StoredImage(
    string FileName,
    string ContentType,
    long Size,
    DateTimeOffset UploadedAtUtc);
