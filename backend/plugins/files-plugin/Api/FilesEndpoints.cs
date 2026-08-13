using FilesPlugin.Api.Contracts;
using FilesPlugin.Application.Files;
using System.IO;

namespace FilesPlugin.Api;

public static class FilesEndpoints
{
    public static IEndpointRouteBuilder MapFilesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok", plugin = "files-plugin" }));

        app.MapGet("/files/greeting", (HttpContext context) =>
        {
            var email = context.Request.Headers["X-User-Email"].ToString();
            return Results.Ok(new
            {
                message = string.IsNullOrWhiteSpace(email) ? "Hello from files-plugin, guest!" : $"Hello from files-plugin, {email}!",
                from = "files-plugin"
            });
        });

        app.MapPost("/files/images", async (HttpContext context, IFileService service, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("FilesUpload");

            try
            {
                var form = await context.Request.ReadFormAsync(cancellationToken);
                var file = form.Files.GetFile("file");
                if (file is null)
                {
                    var formFieldNames = form.Keys.ToArray();
                    logger.LogWarning(
                        "Image upload rejected: no file in form field 'file'. ContentType={ContentType}, FormFields=[{FormFields}], FileCount={FileCount}",
                        context.Request.ContentType,
                        string.Join(",", formFieldNames),
                        form.Files.Count);

                    return Results.BadRequest(new
                    {
                        error = "No file was uploaded under form field 'file'.",
                        expectedField = "file",
                        contentType = context.Request.ContentType,
                        formFields = formFieldNames,
                        fileCount = form.Files.Count,
                        traceId = context.TraceIdentifier
                    });
                }

                if (file.Length == 0)
                    return Results.BadRequest(new
                    {
                        error = "File is empty.",
                        fileName = file.FileName,
                        size = file.Length,
                        traceId = context.TraceIdentifier
                    });

                await using var stream = file.OpenReadStream();
                var storedImage = await service.UploadAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    cancellationToken);

                var response = new UploadImageResponse(
                    storedImage.FileName,
                    storedImage.ContentType,
                    storedImage.Size,
                    $"{context.Request.Scheme}://{context.Request.Host}/static/images/{storedImage.FileName}",
                    storedImage.UploadedAtUtc);

                return Results.Created($"/files/images/{storedImage.FileName}", response);
            }
            catch (InvalidDataException ex)
            {
                logger.LogWarning(ex, "Image upload rejected while reading multipart form payload.");
                return Results.BadRequest(new
                {
                    error = "Invalid multipart form payload.",
                    detail = ex.Message,
                    expectedContentType = "multipart/form-data",
                    traceId = context.TraceIdentifier
                });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Image upload rejected by validation.");
                return Results.BadRequest(new
                {
                    error = "Image upload validation failed.",
                    detail = ex.Message,
                    traceId = context.TraceIdentifier
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during image upload.");
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Image upload failed unexpectedly.",
                    detail: ex.Message,
                    extensions: new Dictionary<string, object?>
                    {
                        ["traceId"] = context.TraceIdentifier
                    });
            }
        })
        .DisableAntiforgery();

        app.MapGet("/files/images", async (HttpContext context, IFileService service, CancellationToken cancellationToken) =>
        {
            var files = await service.ListAsync(cancellationToken);
            var response = files.Select(file => new UploadImageResponse(
                file.FileName,
                file.ContentType,
                file.Size,
                $"{context.Request.Scheme}://{context.Request.Host}/static/images/{file.FileName}",
                file.UploadedAtUtc));
            return Results.Ok(response);
        });

        app.MapGet("/files/images/{fileName}", async (HttpContext context, string fileName, IFileService service, CancellationToken cancellationToken) =>
        {
            var file = await service.GetByFileNameAsync(fileName, cancellationToken);
            if (file is null)
                return Results.NotFound(new { error = "File not found." });

            return Results.Ok(new UploadImageResponse(
                file.FileName,
                file.ContentType,
                file.Size,
                $"{context.Request.Scheme}://{context.Request.Host}/static/images/{file.FileName}",
                file.UploadedAtUtc));
        });

        app.MapDelete("/files/images/{fileName}", async (string fileName, IFileService service, CancellationToken cancellationToken) =>
        {
            var wasDeleted = await service.DeleteAsync(fileName, cancellationToken);
            return wasDeleted
                ? Results.NoContent()
                : Results.NotFound(new { error = "File not found." });
        });

        return app;
    }
}
