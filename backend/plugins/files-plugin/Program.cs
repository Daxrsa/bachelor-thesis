using FilesPlugin.Api;
using FilesPlugin.Application.Files;
using FilesPlugin.Infrastructure;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFilesInfrastructure(builder.Configuration);
builder.Services.AddFilesApplication(builder.Configuration);

var app = builder.Build();

var staticRoot = ResolvePath(
    app.Environment.ContentRootPath,
    app.Configuration["FileStorage:RootPath"] ?? Path.Combine("storage", "images"));
Directory.CreateDirectory(staticRoot);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(staticRoot),
    RequestPath = app.Configuration["FileStorage:RequestPath"] ?? "/static/images"
});

app.MapFilesEndpoints();

app.Run("http://0.0.0.0:8080");

static string ResolvePath(string contentRootPath, string configuredPath) =>
    Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
