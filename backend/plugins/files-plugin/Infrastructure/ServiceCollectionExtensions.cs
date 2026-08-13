using FilesPlugin.Application.Files;
using FilesPlugin.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FilesPlugin.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFilesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<FileStorageOptions>>().Value);
        services.AddSingleton<IImageStorageRepository, LocalImageStorageRepository>();
        return services;
    }
}
