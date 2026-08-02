using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductsPlugin.Application.Products;
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
}