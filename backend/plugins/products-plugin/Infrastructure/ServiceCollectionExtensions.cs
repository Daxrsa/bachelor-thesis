using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductsPlugin.Application.Products;
using ProductsPlugin.Infrastructure.Messaging;
using ProductsPlugin.Infrastructure.Persistence;

namespace ProductsPlugin.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProductsInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ProductsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<ProductsDatabaseInitializer>();
        return services;
    }

    public static IServiceCollection AddProductsMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<IProductEventPublisher, RabbitMqProductEventPublisher>();
        return services;
    }
}